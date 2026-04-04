namespace PrStagingDeploy.Services;

using System.Text;
using PrStagingDeploy.Models;

/// <summary>
/// Posts a single PR comment with <c>GitHub:PrCommentToken</c>. Replaces prior marker comments.
/// </summary>
public class PrStagingDeployCommentService
{
    public const string Marker = "<!-- ivy-staging-deploy -->";

    private const string RocketReaction = "rocket";

    private readonly GitHubApiClient _github;
    private readonly IConfiguration _config;
    private readonly ILogger<PrStagingDeployCommentService> _logger;

    public PrStagingDeployCommentService(
        GitHubApiClient github,
        IConfiguration config,
        ILogger<PrStagingDeployCommentService> logger)
    {
        _github = github;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Replaces every prior staging marker comment, then posts a new one with links.
    /// If <paramref name="error"/> is set, the comment shows the error instead of links —
    /// call this to replace the earlier links comment when a failure is detected.
    /// New commits trigger the same path so the old comment is always removed first.
    /// </summary>
    public Task TryPostStagingAsync(
        string owner,
        string repo,
        int prNumber,
        string? docsUrl,
        string? samplesUrl,
        string? error,
        CancellationToken cancellationToken = default)
    {
        var body = BuildBody(docsUrl, samplesUrl, error, removed: false);
        return PostMarkerCommentAsync(owner, repo, prNumber, body, cancellationToken);
    }

    /// <summary>PR closed / staging torn down.</summary>
    public Task TryPostStagingRemovedAsync(
        string owner,
        string repo,
        int prNumber,
        CancellationToken cancellationToken = default)
    {
        var body = BuildBody(docsUrl: null, samplesUrl: null, error: null, removed: true);
        return PostMarkerCommentAsync(owner, repo, prNumber, body, cancellationToken);
    }

    public async Task TryAddRocketReactionAsync(
        string owner,
        string repo,
        long issueCommentId,
        CancellationToken cancellationToken = default)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        await _github.AddReactionToIssueCommentAsync(owner, repo, issueCommentId, RocketReaction, pat, cancellationToken);
    }

    private async Task PostMarkerCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string body,
        CancellationToken cancellationToken)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        await DeleteAllMarkerCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
        var id = await _github.CreateIssueCommentAsync(owner, repo, prNumber, pat, body, cancellationToken);
        if (id == null)
            _logger.LogWarning("Failed to create staging comment on PR #{Pr}", prNumber);
        else
            _logger.LogInformation("Replaced staging comment on PR #{Pr} (deleted prior marker comments)", prNumber);
    }

    private static string BuildBody(string? docsUrl, string? samplesUrl, string? error, bool removed)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Marker);
        sb.AppendLine();

        if (removed)
        {
            sb.AppendLine("### Staging removed");
            sb.AppendLine();
            sb.AppendLine("Staging environment has been deleted for this PR.");
            return sb.ToString();
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            sb.AppendLine("### Staging deploy failed");
            sb.AppendLine();
            var oneLine = error.Trim().Replace("\r", "").Replace("\n", " ");
            sb.AppendLine($"> {oneLine}");
            return sb.ToString();
        }

        sb.AppendLine("### Staging preview");
        sb.AppendLine();
        sb.AppendLine(FormatLinkLine("📄 Docs", docsUrl));
        sb.AppendLine(FormatLinkLine("🧪 Samples", samplesUrl));
        return sb.ToString();
    }

    private static string FormatLinkLine(string label, string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            return $"**{label}:** [{url}]({url})";
        return $"**{label}:** _not available_";
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
            var ids = FindCommentIdsByMarker(comments, Marker);
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

        var finalCheck = await _github.ListIssueCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
        if (FindCommentIdsByMarker(finalCheck, Marker).Count > 0)
            _logger.LogWarning("Some ivy-staging marker comments may still exist on PR #{Pr} after cleanup", prNumber);
    }

    private static List<long> FindCommentIdsByMarker(
        IReadOnlyList<GitHubIssueComment> comments,
        string marker)
    {
        var list = new List<long>();
        foreach (var c in comments)
        {
            if (c.Body.Contains(marker, StringComparison.Ordinal))
                list.Add(c.Id);
        }
        return list;
    }
}
