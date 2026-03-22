namespace PrStagingDeploy.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Handles GitHub webhooks: pull_request (opened/reopened/synchronize → deploy; closed → delete staging), issue_comment (/deploy).
/// </summary>
public class GitHubWebhookHandler
{
    private readonly StagingDeployService _deployService;
    private readonly GitHubApiClient _github;
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubWebhookHandler> _logger;

    public GitHubWebhookHandler(StagingDeployService deployService, GitHubApiClient github, IConfiguration config, ILogger<GitHubWebhookHandler> logger)
    {
        _deployService = deployService;
        _github = github;
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

                _logger.LogInformation("PR #{Pr} opened: {Title} branch={Branch}", prNumber, title, branch);
                var deployResult = await _deployService.DeployBranchAsync(apiToken, branch);
                _logger.LogInformation("Deploy result: {Success} - {Message}", deployResult.Success, deployResult.Message);
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
                var redeployResult = await _deployService.RedeployBranchAsync(apiToken, branch);
                _logger.LogInformation("Redeploy result: {Success} - {Message}", redeployResult.Success, redeployResult.Message);
                break;

            case "closed":
                _logger.LogInformation("PR #{Pr} closed: {Branch} — removing Sliplane staging services", prNumber, branch);
                var deleteResult = await _deployService.DeleteBranchAsync(apiToken, branch);
                _logger.LogInformation("Delete result: {Success} - {Message}", deleteResult.Success, deleteResult.Message);
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

        var comment = root.GetProperty("comment").GetProperty("body").GetString() ?? "";
        if (!comment.Trim().Equals("/deploy", StringComparison.OrdinalIgnoreCase))
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

        var commentAuthorLogin = root.GetProperty("comment").GetProperty("user").GetProperty("login").GetString();
        if (!GitHubDeployPermissions.IsUserAllowed(_config, commentAuthorLogin))
        {
            _logger.LogInformation(
                "Ignoring /deploy on PR #{Pr}: comment author {User} not in GitHub:DeployAllowedUsers",
                prNumber, commentAuthorLogin ?? "(unknown)");
            return;
        }

        _logger.LogInformation("PR #{Pr} /deploy comment: {Branch}", prNumber, branch);
        var result = await _deployService.DeployBranchAsync(apiToken, branch);
        _logger.LogInformation("Deploy result: {Success} - {Message}", result.Success, result.Message);
    }

    private string GetApiToken()
    {
        return _config["Sliplane:ApiToken"] ?? "";
    }
}
