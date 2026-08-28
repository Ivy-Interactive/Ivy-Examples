namespace TendrilPrStaging.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Handles GitHub webhooks: pull_request (opened/reopened/synchronize → deploy; closed → delete),
/// issue_comment (/deploy command).
/// </summary>
public class GitHubWebhookHandler
{
    private readonly TendrilStagingDeployService _deployService;
    private readonly GitHubApiClient _github;
    private readonly TendrilReposProvider _reposProvider;
    private readonly TendrilPrCommentService _prComments;
    private readonly TendrilErrorWatcherQueue _errorWatcher;
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubWebhookHandler> _logger;

    public GitHubWebhookHandler(
        TendrilStagingDeployService deployService,
        GitHubApiClient github,
        TendrilReposProvider reposProvider,
        TendrilPrCommentService prComments,
        TendrilErrorWatcherQueue errorWatcher,
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

        string? cloneUrl = null;
        if (pr.TryGetProperty("head", out var headEl) && headEl.TryGetProperty("repo", out var headRepoEl) && headRepoEl.ValueKind == JsonValueKind.Object)
        {
            cloneUrl = headRepoEl.TryGetProperty("clone_url", out var cu) ? cu.GetString() : null;
        }

        var repoConfig = _reposProvider.FindByOwnerRepo(owner, repoName);
        if (repoConfig == null)
        {
            _logger.LogInformation("PR webhook for {Owner}/{Repo} ignored — no matching repo in config.", owner, repoName);
            return;
        }

        var apiToken = _config["Sliplane:ApiToken"] ?? "";
        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Sliplane:ApiToken not configured, skipping webhook for PR #{Pr}", prNumber);
            return;
        }

        _logger.LogInformation("Processing PR webhook: action={Action} repo={RepoKey} PR#{Pr} branch={Branch}",
            action, repoConfig.Key, prNumber, branch);

        switch (action)
        {
            case "opened":
            case "reopened":
                if (!GitHubDeployPermissions.IsUserAllowed(_config, prAuthorLogin))
                {
                    _logger.LogInformation("Skipping auto-deploy for PR #{Pr}: author {User} not allowed.", prNumber, prAuthorLogin ?? "(unknown)");
                    break;
                }

                var existing = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
                if (existing != null)
                {
                    _logger.LogInformation("Staging service already exists for PR #{Pr}, skipping deploy.", prNumber);
                    await _prComments.TryPostStagingAsync(owner, repoName, prNumber, existing.ServiceUrl, error: null, branch: branch);
                    break;
                }

                _logger.LogInformation("PR #{Pr} opened: {Title} branch={Branch}", prNumber, title, branch);
                var deployResult = await _deployService.DeployBranchAsync(apiToken, repoConfig, branch, prNumber, cloneUrlOverride: cloneUrl);
                _logger.LogInformation("Deploy result: {Success} - {Message}", deployResult.Success, deployResult.Message);

                if (deployResult.SkippedBecausePrNotOpen)
                    break;

                await _prComments.TryPostStagingAsync(owner, repoName, prNumber,
                    deployResult.ServiceUrl,
                    deployResult.Success ? null : deployResult.Message,
                    branch: branch);

                if (deployResult.Success && deployResult.ServiceId != null)
                    await _errorWatcher.EnqueueAsync(new TendrilErrorWatchRequest(repoConfig.Key, owner, repoName, prNumber, deployResult.ServiceId, Branch: branch));

                break;

            case "synchronize":
                if (!GitHubDeployPermissions.IsUserAllowed(_config, prAuthorLogin))
                {
                    _logger.LogInformation("Skipping auto-redeploy for PR #{Pr}: author {User} not allowed.", prNumber, prAuthorLogin ?? "(unknown)");
                    break;
                }

                _logger.LogInformation("PR #{Pr} updated: branch={Branch}", prNumber, branch);
                var redeployResult = await _deployService.RedeployBranchAsync(apiToken, repoConfig, prNumber);
                _logger.LogInformation("Redeploy result: {Success} - {Message}", redeployResult.Success, redeployResult.Message);

                if (!redeployResult.Success)
                {
                    _logger.LogInformation("PR #{Pr} redeploy found no service, falling back to fresh deploy.", prNumber);
                    var fallback = await _deployService.DeployBranchAsync(apiToken, repoConfig, branch, prNumber, cloneUrlOverride: cloneUrl);
                    _logger.LogInformation("Fallback deploy result: {Success} - {Message}", fallback.Success, fallback.Message);

                    if (fallback.SkippedBecausePrNotOpen)
                        break;

                    await _prComments.TryPostStagingAsync(owner, repoName, prNumber,
                        fallback.ServiceUrl,
                        fallback.Success ? null : fallback.Message,
                        branch: branch);

                    if (fallback.Success && fallback.ServiceId != null)
                        await _errorWatcher.EnqueueAsync(new TendrilErrorWatchRequest(repoConfig.Key, owner, repoName, prNumber, fallback.ServiceId, Branch: branch));

                    break;
                }

                var syncDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
                await _prComments.TryPostStagingAsync(owner, repoName, prNumber, syncDep?.ServiceUrl, error: null, branch: branch);
                if (syncDep?.ServiceId != null)
                    await _errorWatcher.EnqueueAsync(new TendrilErrorWatchRequest(repoConfig.Key, owner, repoName, prNumber, syncDep.ServiceId, Branch: branch));

                break;

            case "closed":
                _logger.LogInformation("PR #{Pr} closed: branch={Branch} — removing Tendril staging service", prNumber, branch);
                var deleteResult = await _deployService.DeleteBranchAsync(apiToken, repoConfig, prNumber);
                _logger.LogInformation("Delete result: {Success} - {Message}", deleteResult.Success, deleteResult.Message);
                if (deleteResult.Success)
                    await _prComments.TryPostStagingRemovedAsync(owner, repoName, prNumber, branch: branch);
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
        if (!IsDeployCommand(commentBody.Trim()))
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
            _logger.LogInformation("issue_comment /deploy for {Owner}/{Repo} ignored — no matching repo in config.", owner, repo);
            return;
        }

        var ghToken = _config["GitHub:Token"] ?? "";
        var (branch, cloneUrl) = await _github.GetPullRequestBranchAndCloneUrlAsync(owner, repo, prNumber, ghToken);
        if (string.IsNullOrEmpty(branch))
        {
            _logger.LogWarning("Could not get branch for PR #{Pr}", prNumber);
            return;
        }

        var apiToken = _config["Sliplane:ApiToken"] ?? "";
        if (string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Sliplane:ApiToken not configured");
            return;
        }

        var commentAuthorLogin = commentEl.GetProperty("user").GetProperty("login").GetString();
        if (!GitHubDeployPermissions.IsUserAllowed(_config, commentAuthorLogin))
        {
            _logger.LogInformation("Ignoring /deploy on PR #{Pr}: author {User} not allowed.", prNumber, commentAuthorLogin ?? "(unknown)");
            return;
        }

        await _prComments.TryAddRocketReactionAsync(owner, repo, commentId);

        var existingDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
        if (existingDep != null)
        {
            _logger.LogInformation("Staging service already exists for PR #{Pr}, posting current link.", prNumber);
            await _prComments.TryPostStagingAsync(owner, repo, prNumber, existingDep.ServiceUrl, error: null, branch: branch);
            return;
        }

        _logger.LogInformation("PR #{Pr} /deploy comment: branch={Branch}", prNumber, branch);
        var result = await _deployService.DeployBranchAsync(apiToken, repoConfig, branch, prNumber, cloneUrlOverride: cloneUrl);
        _logger.LogInformation("Deploy result: {Success} - {Message}", result.Success, result.Message);

        if (result.SkippedBecausePrNotOpen)
            return;

        await _prComments.TryPostStagingAsync(owner, repo, prNumber,
            result.ServiceUrl,
            result.Success ? null : TruncLine(result.Message, 500),
            branch: branch);

        if (result.Success && result.ServiceId != null)
            await _errorWatcher.EnqueueAsync(new TendrilErrorWatchRequest(repoConfig.Key, owner, repo, prNumber, result.ServiceId, Branch: branch));
    }

    private static bool IsDeployCommand(string trimmed)
    {
        foreach (var cmd in new[] { "/deploy", "/publish" })
        {
            if (trimmed.Equals(cmd, StringComparison.OrdinalIgnoreCase)) return true;
            if (trimmed.StartsWith(cmd + " ", StringComparison.OrdinalIgnoreCase)) return true;
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
