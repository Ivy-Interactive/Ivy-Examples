namespace PrStagingDeploy.Services;

using System.Security.Cryptography;
using System.Text;
using PrStagingDeploy.Models;

/// <summary>
/// Handles GitHub webhooks: pull_request (opened/reopened/synchronize → deploy; closed → delete staging), issue_comment (/deploy).
/// Routes each event to the matching <see cref="StagingRepoConfig"/> by owner+repo.
/// </summary>
public class GitHubWebhookHandler
{
    private readonly StagingDeployService _deployService;
    private readonly GitHubApiClient _github;
    private readonly StagingReposProvider _reposProvider;
    private readonly PrStagingDeployCommentService _prComments;
    private readonly StagingErrorWatcherQueue _errorWatcher;
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubWebhookHandler> _logger;

    public GitHubWebhookHandler(
        StagingDeployService deployService,
        GitHubApiClient github,
        StagingReposProvider reposProvider,
        PrStagingDeployCommentService prComments,
        StagingErrorWatcherQueue errorWatcher,
        IConfiguration config,
        ILogger<GitHubWebhookHandler> logger)
    {
        _deployService = deployService;
        _github = github;
        _reposProvider = reposProvider;
        _prComments = prComments;
        _errorWatcher = errorWatcher;
        _config = config;
        _logger = logger;
    }

    public bool VerifySignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(secret) || !signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrEmpty(secret);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature));
    }

    public async Task HandleAsync(string eventType, string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            switch (eventType)
            {
                case "ping":
                    _logger.LogInformation("Webhook ping received");
                    break;

                case "pull_request":
                    await HandlePullRequestAsync(root);
                    break;

                case "issue_comment":
                    await HandleIssueCommentAsync(root);
                    break;

                default:
                    _logger.LogDebug("Ignored event: {Event}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook handler error for {Event}", eventType);
        }
    }

    private async Task HandlePullRequestAsync(JsonElement root)
    {
        var action = root.GetProperty("action").GetString() ?? "";
        var pr = root.GetProperty("pull_request");
        var branch = pr.GetProperty("head").GetProperty("ref").GetString() ?? "";
        var prNumber = pr.GetProperty("number").GetInt32();
        var title = pr.GetProperty("title").GetString() ?? "";
        var prAuthorLogin = pr.GetProperty("user").GetProperty("login").GetString();
        var repoEl = root.GetProperty("repository");
        var owner = repoEl.GetProperty("owner").GetProperty("login").GetString() ?? "";
        var repoName = repoEl.GetProperty("name").GetString() ?? "";

        var repoConfig = _reposProvider.FindByOwnerRepo(owner, repoName);
        if (repoConfig == null)
        {
            _logger.LogInformation(
                "PR webhook for {Owner}/{Repo} ignored — no matching entry in Repos config.",
                owner, repoName);
            return;
        }

        var headRepoEl = pr.GetProperty("head").GetProperty("repo");
        var headRepoCloneUrl = headRepoEl.TryGetProperty("clone_url", out var cu)
            ? cu.GetString()
            : null;

        var apiToken = GetApiToken();
        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Sliplane API token not configured, skipping webhook deploy for PR #{Pr} branch={Branch}", prNumber, branch);
            return;
        }

        _logger.LogInformation(
            "Processing PR webhook: action={Action} repo={RepoKey} PR#{Pr} branch={Branch}",
            action, repoConfig.Key, prNumber, branch);

        switch (action)
        {
            case "opened":
            case "reopened":
                if (!GitHubDeployPermissions.IsUserAllowed(_config, prAuthorLogin))
                {
                    _logger.LogInformation(
                        "Skipping auto-deploy for PR #{Pr}: PR author {User} not in GitHub:DeployAllowedUsers",
                        prNumber, prAuthorLogin ?? "(unknown)");
                    break;
                }

                var existingDepOnOpen = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
                if (existingDepOnOpen != null)
                {
                    _logger.LogInformation(
                        "Staging services already exist for PR #{Pr} branch={Branch}, skipping deploy",
                        prNumber, branch);
                    if (!string.IsNullOrEmpty(existingDepOnOpen.DocsServiceId) || !string.IsNullOrEmpty(existingDepOnOpen.SamplesServiceId))
                    {
                        await _prComments.TryPostStagingAsync(
                            owner, repoName, prNumber,
                            existingDepOnOpen.DocsUrl, existingDepOnOpen.SamplesUrl, error: null,
                            docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                    }

                    break;
                }

                _logger.LogInformation("PR #{Pr} opened: {Title} branch={Branch}", prNumber, title, branch);
                var deployResult = await _deployService.DeployBranchAsync(
                    apiToken, repoConfig, branch, prNumber, headRepoCloneUrl);
                _logger.LogInformation("Deploy result: {Success} - {Message}", deployResult.Success, deployResult.Message);
                if (deployResult.SkippedBecausePrNotOpen)
                {
                    _logger.LogInformation("Skip staging deploy for PR #{Pr}: already closed or merged", prNumber);
                    break;
                }

                if (deployResult.Success && (!string.IsNullOrEmpty(deployResult.DocsServiceId) || !string.IsNullOrEmpty(deployResult.SamplesServiceId)))
                {
                    await _prComments.TryPostStagingAsync(
                        owner, repoName, prNumber, deployResult.DocsUrl, deployResult.SamplesUrl, error: null,
                        docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                    await EnqueueErrorWatchAsync(repoConfig.Key, owner, repoName, prNumber, deployResult.DocsServiceId, deployResult.SamplesServiceId);
                }
                else
                {
                    await _prComments.TryPostStagingAsync(
                        owner, repoName, prNumber, null, null, TruncLine(deployResult.Message, 500),
                        docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                }

                break;

            case "synchronize":
                if (!GitHubDeployPermissions.IsUserAllowed(_config, prAuthorLogin))
                {
                    _logger.LogInformation(
                        "Skipping auto-redeploy for PR #{Pr}: PR author {User} not in GitHub:DeployAllowedUsers",
                        prNumber, prAuthorLogin ?? "(unknown)");
                    break;
                }

                _logger.LogInformation("PR #{Pr} updated: {Branch}", prNumber, branch);
                var redeployResult = await _deployService.RedeployBranchAsync(apiToken, repoConfig, branch, prNumber);
                _logger.LogInformation("Redeploy result: {Success} - {Message}", redeployResult.Success, redeployResult.Message);

                if (!redeployResult.Success)
                {
                    _logger.LogInformation("PR #{Pr} redeploy found 0 services, falling back to fresh deploy", prNumber);
                    var fallbackResult = await _deployService.DeployBranchAsync(
                        apiToken, repoConfig, branch, prNumber, headRepoCloneUrl);
                    _logger.LogInformation("Fallback deploy result: {Success} - {Message}", fallbackResult.Success, fallbackResult.Message);

                    if (fallbackResult.SkippedBecausePrNotOpen)
                    {
                        _logger.LogInformation("Skip fallback deploy for PR #{Pr}: already closed or merged", prNumber);
                        break;
                    }

                    if (fallbackResult.Success && (!string.IsNullOrEmpty(fallbackResult.DocsServiceId) || !string.IsNullOrEmpty(fallbackResult.SamplesServiceId)))
                    {
                        await _prComments.TryPostStagingAsync(
                            owner, repoName, prNumber, fallbackResult.DocsUrl, fallbackResult.SamplesUrl, error: null,
                            docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                        await EnqueueErrorWatchAsync(repoConfig.Key, owner, repoName, prNumber, fallbackResult.DocsServiceId, fallbackResult.SamplesServiceId);
                    }
                    else
                    {
                        await _prComments.TryPostStagingAsync(
                            owner, repoName, prNumber, null, null, TruncLine(fallbackResult.Message, 500),
                            docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                    }

                    break;
                }

                // Redeploy triggered OK — post current known links, service will rebuild in background.
                var syncDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
                await _prComments.TryPostStagingAsync(
                    owner, repoName, prNumber, syncDep?.DocsUrl, syncDep?.SamplesUrl, error: null,
                    docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                await EnqueueErrorWatchAsync(repoConfig.Key, owner, repoName, prNumber, syncDep?.DocsServiceId, syncDep?.SamplesServiceId);

                break;

            case "closed":
                _logger.LogInformation("PR #{Pr} closed: {Branch} — removing Sliplane staging services", prNumber, branch);
                var deleteResult = await _deployService.DeleteBranchAsync(apiToken, repoConfig, prNumber);
                _logger.LogInformation("Delete result: {Success} - {Message}", deleteResult.Success, deleteResult.Message);
                if (deleteResult.Success)
                    await _prComments.TryPostStagingRemovedAsync(owner, repoName, prNumber,
                        docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
                break;

            default:
                _logger.LogDebug("Ignored PR action: {Action}", action);
                break;
        }
    }

    private async Task HandleIssueCommentAsync(JsonElement root)
    {
        var action = root.GetProperty("action").GetString();
        if (action != "created")
            return;

        var commentEl = root.GetProperty("comment");
        var commentId = commentEl.GetProperty("id").GetInt64();
        var commentBody = commentEl.GetProperty("body").GetString() ?? "";
        var trimmedComment = commentBody.Trim();
        if (!IsDeployCommand(trimmedComment))
            return;

        var issue = root.GetProperty("issue");
        if (!issue.TryGetProperty("pull_request", out _))
            return;

        var prNumber = issue.GetProperty("number").GetInt32();
        var owner = root.GetProperty("repository").GetProperty("owner").GetProperty("login").GetString() ?? "";
        var repo = root.GetProperty("repository").GetProperty("name").GetString() ?? "";

        var repoConfig = _reposProvider.FindByOwnerRepo(owner, repo);
        if (repoConfig == null)
        {
            _logger.LogInformation(
                "issue_comment /deploy for {Owner}/{Repo} ignored — no matching entry in Repos config.",
                owner, repo);
            return;
        }

        var ghToken = _config["GitHub:Token"] ?? "";

        var branch = await _github.GetPullRequestBranchAsync(owner, repo, prNumber, ghToken);
        if (string.IsNullOrEmpty(branch))
        {
            _logger.LogWarning("Could not get branch for PR #{Pr}", prNumber);
            return;
        }

        var apiToken = GetApiToken();
        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Sliplane API token not configured");
            return;
        }

        var commentAuthorLogin = commentEl.GetProperty("user").GetProperty("login").GetString();
        if (!GitHubDeployPermissions.IsUserAllowed(_config, commentAuthorLogin))
        {
            _logger.LogInformation(
                "Ignoring /deploy on PR #{Pr}: comment author {User} not in GitHub:DeployAllowedUsers",
                prNumber, commentAuthorLogin ?? "(unknown)");
            return;
        }

        await _prComments.TryAddRocketReactionAsync(owner, repo, commentId);

        var existingDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
        if (existingDep != null)
        {
            _logger.LogInformation("Staging services already exist for PR #{Pr}, posting current links", prNumber);
            if (!string.IsNullOrEmpty(existingDep.DocsServiceId) || !string.IsNullOrEmpty(existingDep.SamplesServiceId))
            {
                await _prComments.TryPostStagingAsync(
                    owner, repo, prNumber, existingDep.DocsUrl, existingDep.SamplesUrl, error: null,
                    docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
            }

            return;
        }

        _logger.LogInformation("PR #{Pr} /deploy comment: {Branch}", prNumber, branch);
        var result = await _deployService.DeployBranchAsync(apiToken, repoConfig, branch, prNumber);
        _logger.LogInformation("Deploy result: {Success} - {Message}", result.Success, result.Message);
        if (result.SkippedBecausePrNotOpen)
        {
            _logger.LogInformation("Skip /deploy for PR #{Pr}: already closed or merged", prNumber);
            return;
        }

        if (result.Success && (!string.IsNullOrEmpty(result.DocsServiceId) || !string.IsNullOrEmpty(result.SamplesServiceId)))
        {
            await _prComments.TryPostStagingAsync(owner, repo, prNumber, result.DocsUrl, result.SamplesUrl, error: null,
                docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
            await EnqueueErrorWatchAsync(repoConfig.Key, owner, repo, prNumber, result.DocsServiceId, result.SamplesServiceId);
        }
        else
        {
            await _prComments.TryPostStagingAsync(owner, repo, prNumber, null, null, TruncLine(result.Message, 500),
                docsEnabled: repoConfig.HasDocs, samplesEnabled: repoConfig.HasSamples);
        }
    }

    private ValueTask EnqueueErrorWatchAsync(
        string repoKey, string owner, string repo, int prNumber,
        string? docsServiceId, string? samplesServiceId)
    {
        if (string.IsNullOrEmpty(docsServiceId) && string.IsNullOrEmpty(samplesServiceId))
            return ValueTask.CompletedTask;
        return _errorWatcher.EnqueueAsync(
            new StagingErrorWatchRequest(repoKey, owner, repo, prNumber, docsServiceId, samplesServiceId));
    }

    private string GetApiToken()
    {
        return _config["Sliplane:ApiToken"] ?? "";
    }

    private static bool IsDeployCommand(string trimmed)
    {
        foreach (var cmd in new[] { "/deploy", "/publish" })
        {
            if (trimmed.Equals(cmd, StringComparison.OrdinalIgnoreCase))
                return true;
            if (trimmed.StartsWith(cmd + " ", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string TruncLine(string? s, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var line = s.Trim().Replace("\r", "").Replace("\n", " ");
        return line.Length <= maxLen ? line : line[..maxLen] + "...";
    }
}
