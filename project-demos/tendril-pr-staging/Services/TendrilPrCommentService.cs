namespace TendrilPrStaging.Services;

using System.Text;
using TendrilPrStaging.Models;

/// <summary>
/// Posts a single bot comment on a PR with the Tendril staging URL.
/// Marker: <c>&lt;!-- ivy-tendril-staging --&gt;</c>. Replaces prior marker comments on each update.
/// </summary>
public class TendrilPrCommentService
{
    public const string Marker = "<!-- ivy-tendril-staging -->";
    private const string RocketReaction = "rocket";

    private readonly GitHubApiClient _github;
    private readonly IConfiguration _config;
    private readonly ILogger<TendrilPrCommentService> _logger;

    public TendrilPrCommentService(
        GitHubApiClient github,
        IConfiguration config,
        ILogger<TendrilPrCommentService> logger)
    {
        _github = github;
        _config = config;
        _logger = logger;
    }

    public Task TryPostStagingAsync(
        string owner,
        string repo,
        int prNumber,
        string? serviceUrl,
        string? error,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var body = BuildBody(serviceUrl, error, branch, prNumber, repo, removed: false);
        return PostMarkerCommentAsync(owner, repo, prNumber, body, cancellationToken);
    }

    public Task TryPostStagingRemovedAsync(
        string owner,
        string repo,
        int prNumber,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var body = BuildBody(serviceUrl: null, error: null, branch, prNumber, repo, removed: true);
        return PostMarkerCommentAsync(owner, repo, prNumber, body, cancellationToken);
    }

    public async Task TryAddRocketReactionAsync(
        string owner,
        string repo,
        long issueCommentId,
        CancellationToken cancellationToken = default)
    {
        var pat = ResolveCommentPat();
        if (string.IsNullOrWhiteSpace(pat))
            return;
        await _github.AddReactionToIssueCommentAsync(owner, repo, issueCommentId, RocketReaction, pat, cancellationToken);
    }

    private string? ResolveCommentPat()
    {
        var pr = _config["GitHub:PrCommentToken"];
        if (!string.IsNullOrWhiteSpace(pr)) return pr;
        var gh = _config["GitHub:Token"];
        return string.IsNullOrWhiteSpace(gh) ? null : gh;
    }

    private async Task PostMarkerCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string body,
        CancellationToken cancellationToken)
    {
        var pat = ResolveCommentPat();
        if (string.IsNullOrWhiteSpace(pat))
        {
            _logger.LogWarning("Skipping PR #{Pr} comment: neither GitHub:PrCommentToken nor GitHub:Token is set.", prNumber);
            return;
        }

        await DeleteAllMarkerCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
        var id = await _github.CreateIssueCommentAsync(owner, repo, prNumber, pat, body, cancellationToken);
        if (id == null)
            _logger.LogWarning("Failed to create staging comment on PR #{Pr}", prNumber);
        else
            _logger.LogInformation("Posted staging comment on PR #{Pr}", prNumber);
    }

    private static string BuildBody(string? serviceUrl, string? error, string? branch, int prNumber, string? repo, bool removed)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Marker);
        sb.AppendLine();

        if (removed)
        {
            sb.AppendLine("### 🛑 Tendril Staging Environment Removed");
            sb.AppendLine();
            var branchInfo = !string.IsNullOrWhiteSpace(branch) ? $" (`{branch}`)" : "";
            sb.AppendLine($"The Tendril staging preview for **PR #{prNumber}**{branchInfo} has been deleted.");
            return sb.ToString();
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            sb.AppendLine("### ❌ Tendril Staging Deployment Failed");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(branch))
                sb.AppendLine($"- **Branch:** `{branch}` (PR #{prNumber})");
            sb.AppendLine();
            var oneLine = error.Trim().Replace("\r", "").Replace("\n", " ");
            sb.AppendLine($"> **Error:** {oneLine}");
            return sb.ToString();
        }

        sb.AppendLine("### 🚀 Tendril Staging Preview Ready");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(branch))
            sb.AppendLine($"- **Branch:** `{branch}` (PR #{prNumber})");

        if (!string.IsNullOrWhiteSpace(serviceUrl))
            sb.AppendLine($"- **Environment URL:** [{serviceUrl}]({serviceUrl})");
        else
            sb.AppendLine("- **Environment URL:** _Provisioning service and acquiring domain..._");

        return sb.ToString();
    }

    private async Task DeleteAllMarkerCommentsAsync(
        string owner,
        string repo,
        int prNumber,
        string pat,
        CancellationToken cancellationToken)
    {
        const int maxRounds = 15;
        for (var round = 0; round < maxRounds; round++)
        {
            var comments = await _github.ListIssueCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
            var ids = comments
                .Where(c => c.Body.Contains(Marker, StringComparison.Ordinal))
                .Select(c => c.Id)
                .ToList();

            if (ids.Count == 0)
                return;

            foreach (var id in ids)
            {
                var deleted = false;
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    if (await _github.DeleteIssueCommentAsync(owner, repo, id, pat, cancellationToken))
                    {
                        deleted = true;
                        break;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), cancellationToken);
                }

                if (!deleted)
                    _logger.LogWarning("Could not delete staging marker comment {CommentId} on PR #{Pr}", id, prNumber);
            }
        }
    }
}
