namespace PrStagingDeploy.Services;

using System.Security.Cryptography;
using System.Text;
using PrStagingDeploy.Models;

/// <summary>
/// Handles GitHub webhooks: pull_request (opened/reopened/synchronize → deploy; closed → delete staging), issue_comment (/deploy).
/// </summary>
public class GitHubWebhookHandler
{
    private readonly StagingDeployService _deployService;
    private readonly GitHubApiClient _github;
    private readonly PrStagingDeployCommentService _prComments;
    private readonly PrStagingDeployCommentUpdateQueue _commentUpdateQueue;
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubWebhookHandler> _logger;

    public GitHubWebhookHandler(
        StagingDeployService deployService,
        GitHubApiClient github,
        PrStagingDeployCommentService prComments,
        PrStagingDeployCommentUpdateQueue commentUpdateQueue,
        IConfiguration config,
        ILogger<GitHubWebhookHandler> logger)
    {
        _deployService = deployService;
        _github = github;
        _prComments = prComments;
        _commentUpdateQueue = commentUpdateQueue;
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

        // For fork PRs the head repo differs from the base repo.
        // Sliplane must clone from the fork URL, otherwise the branch won't be found.
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

        _logger.LogInformation("Processing PR webhook: action={Action} PR#{Pr} branch={Branch}", action, prNumber, branch);

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

                // Check if staging services already exist for this PR.
                var existingDepOnOpen = await _deployService.GetDeploymentByPrNumberAsync(apiToken, prNumber);
                if (existingDepOnOpen != null)
                {
                    _logger.LogInformation(
                        "Staging services already exist for PR #{Pr} branch={Branch}, skipping deploy",
                        prNumber, branch);

                    // Re-enqueue so the background worker posts the final result when ready.
                    if (!string.IsNullOrEmpty(existingDepOnOpen.DocsServiceId) || !string.IsNullOrEmpty(existingDepOnOpen.SamplesServiceId))
                    {
                        await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                            Owner: owner,
                            Repo: repoName,
                            PrNumber: prNumber,
                            BranchName: branch,
                            DocsServiceId: existingDepOnOpen.DocsServiceId,
                            SamplesServiceId: existingDepOnOpen.SamplesServiceId));
                    }
                    break;
                }

                _logger.LogInformation("PR #{Pr} opened: {Title} branch={Branch}", prNumber, title, branch);
                var deployResult = await _deployService.DeployBranchAsync(apiToken, branch, prNumber, headRepoCloneUrl);
                _logger.LogInformation("Deploy result: {Success} - {Message}", deployResult.Success, deployResult.Message);
                if (deployResult.Success)
                {
                    if (string.IsNullOrEmpty(deployResult.DocsServiceId) && string.IsNullOrEmpty(deployResult.SamplesServiceId))
                    {
                        await _prComments.TryPostOrUpdateStagingCommentAsync(
                            owner, repoName, prNumber,
                            docsUrl: null, samplesUrl: null,
                            status: "Deploy failed",
                            logLines: new[] { TruncLine(deployResult.Message, 240) },
                            forceNewComment: true);
                        break;
                    }

                    await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                        Owner: owner,
                        Repo: repoName,
                        PrNumber: prNumber,
                        BranchName: branch,
                        DocsServiceId: deployResult.DocsServiceId,
                        SamplesServiceId: deployResult.SamplesServiceId));
                }
                else
                {
                    await _prComments.TryPostOrUpdateStagingCommentAsync(
                        owner, repoName, prNumber,
                        docsUrl: null, samplesUrl: null,
                        status: "Deploy failed",
                        logLines: new[] { TruncLine(deployResult.Message, 240) },
                        forceNewComment: true);
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
                var redeployResult = await _deployService.RedeployBranchAsync(apiToken, branch, prNumber);
                _logger.LogInformation("Redeploy result: {Success} - {Message}", redeployResult.Success, redeployResult.Message);

                if (!redeployResult.Success)
                {
                    // Services don't exist yet — fall back to a fresh deploy.
                    _logger.LogInformation("PR #{Pr} redeploy found 0 services, falling back to fresh deploy", prNumber);
                    var fallbackResult = await _deployService.DeployBranchAsync(apiToken, branch, prNumber, headRepoCloneUrl);
                    _logger.LogInformation("Fallback deploy result: {Success} - {Message}", fallbackResult.Success, fallbackResult.Message);

                    if (fallbackResult.Success && (!string.IsNullOrEmpty(fallbackResult.DocsServiceId) || !string.IsNullOrEmpty(fallbackResult.SamplesServiceId)))
                    {
                        await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                            Owner: owner,
                            Repo: repoName,
                            PrNumber: prNumber,
                            BranchName: branch,
                            DocsServiceId: fallbackResult.DocsServiceId,
                            SamplesServiceId: fallbackResult.SamplesServiceId));
                    }
                    else
                    {
                        await _prComments.TryPostOrUpdateStagingCommentAsync(
                            owner, repoName, prNumber,
                            docsUrl: null, samplesUrl: null,
                            status: "Deploy failed",
                            logLines: new[] { TruncLine(fallbackResult.Message, 240) },
                            forceNewComment: true);
                    }
                    break;
                }

                var syncDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, prNumber);
                if (syncDep is null || string.IsNullOrEmpty(syncDep.DocsServiceId) || string.IsNullOrEmpty(syncDep.SamplesServiceId))
                {
                    await _prComments.TryPostOrUpdateStagingCommentAsync(
                        owner, repoName, prNumber,
                        docsUrl: null, samplesUrl: null,
                        status: "Deploy failed",
                        logLines: new[] { "Redeploy: docs/samples services not found in Sliplane." },
                        forceNewComment: true);
                    break;
                }

                await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                    Owner: owner,
                    Repo: repoName,
                    PrNumber: prNumber,
                    BranchName: branch,
                    DocsServiceId: syncDep.DocsServiceId,
                    SamplesServiceId: syncDep.SamplesServiceId));

                break;

            case "closed":
                _logger.LogInformation("PR #{Pr} closed: {Branch} — removing Sliplane staging services", prNumber, branch);
                var deleteResult = await _deployService.DeleteBranchAsync(apiToken, prNumber);
                _logger.LogInformation("Delete result: {Success} - {Message}", deleteResult.Success, deleteResult.Message);
                if (deleteResult.Success)
                    await _prComments.TryPostOrUpdateStagingCommentAsync(
                        owner,
                        repoName,
                        prNumber,
                        docsUrl: null,
                        samplesUrl: null,
                        status: "Deleted",
                        logLines: null);
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
            return; // not a PR comment

        var prNumber = issue.GetProperty("number").GetInt32();
        var owner = root.GetProperty("repository").GetProperty("owner").GetProperty("login").GetString() ?? "";
        var repo = root.GetProperty("repository").GetProperty("name").GetString() ?? "";
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

        // Let users know the bot read the `/deploy` command.
        await _prComments.TryAddRocketReactionAsync(owner, repo, commentId);

        // Check if services already exist for this PR — avoid creating duplicates.
        var existingDep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, prNumber);
        if (existingDep != null)
        {
            _logger.LogInformation("Staging services already exist for PR #{Pr}, re-enqueueing monitor", prNumber);
            if (!string.IsNullOrEmpty(existingDep.DocsServiceId) || !string.IsNullOrEmpty(existingDep.SamplesServiceId))
            {
                await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                    Owner: owner,
                    Repo: repo,
                    PrNumber: prNumber,
                    BranchName: branch,
                    DocsServiceId: existingDep.DocsServiceId,
                    SamplesServiceId: existingDep.SamplesServiceId));
            }
            return;
        }

        _logger.LogInformation("PR #{Pr} /deploy comment: {Branch}", prNumber, branch);
        var result = await _deployService.DeployBranchAsync(apiToken, branch, prNumber);
        _logger.LogInformation("Deploy result: {Success} - {Message}", result.Success, result.Message);
        if (result.Success)
        {
            if (string.IsNullOrEmpty(result.DocsServiceId) && string.IsNullOrEmpty(result.SamplesServiceId))
            {
                await _prComments.TryPostOrUpdateStagingCommentAsync(
                    owner, repo, prNumber,
                    docsUrl: null, samplesUrl: null,
                    status: "Deploy failed",
                    logLines: new[] { TruncLine(result.Message, 240) },
                    forceNewComment: true);
                return;
            }

            await _commentUpdateQueue.EnqueueAsync(new PrStagingDeployCommentUpdateRequest(
                Owner: owner,
                Repo: repo,
                PrNumber: prNumber,
                BranchName: branch,
                DocsServiceId: result.DocsServiceId,
                SamplesServiceId: result.SamplesServiceId));
        }
        else
        {
            await _prComments.TryPostOrUpdateStagingCommentAsync(
                owner,
                repo,
                prNumber,
                docsUrl: null,
                samplesUrl: null,
                status: "Deploy failed",
                logLines: new[] { TruncLine(result.Message, 240) },
                forceNewComment: true);
        }
    }

    private string GetApiToken()
    {
        return _config["Sliplane:ApiToken"] ?? "";
    }

    /// <summary>Matches /deploy or /publish, optionally followed by extra text (e.g. "/deploy this app").</summary>
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

    private static IReadOnlyList<string> BuildRecentLogLines(
        List<SliplaneServiceEvent> docsEvents,
        List<SliplaneServiceEvent> samplesEvents,
        int maxLines)
    {
        var combined = docsEvents.Concat(samplesEvents)
            .OrderByDescending(e => e.CreatedAt)
            .Take(maxLines)
            .ToList();

        if (combined.Count == 0)
            return new[] { "Waiting for Sliplane events..." };

        return combined.Select(e =>
        {
            var time = e.CreatedAt.ToLocalTime().ToString("HH:mm:ss");
            var type = (e.Type ?? "").Trim();
            var msg = !string.IsNullOrWhiteSpace(e.Message) ? e.Message : e.Reason;
            var friendly = type switch
            {
                "service_deploy_success" => "Service deployed successfully",
                "service_deploy_failed" => "Service deploy failed",
                "service_build_failed" => "Build failed",
                "service_build" => "Service build",
                "service_deploy" => "Deploy started",
                _ => type
            };

            var text = string.IsNullOrWhiteSpace(msg) ? friendly : $"{friendly}: {TruncLine(msg, 180)}";
            return $"{time} {text}";
        }).ToList();
    }
}
