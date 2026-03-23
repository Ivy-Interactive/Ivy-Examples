namespace PrStagingDeploy.Services;

using System.Text;
using PrStagingDeploy.Models;

/// <summary>
/// Posts or updates a single PR comment with staging links using <c>GitHub:PrCommentToken</c> (PAT).
/// The comment appears as the PAT owner.
/// </summary>
public class PrStagingDeployCommentService
{
    public const string Marker = "<!-- ivy-staging-deploy -->";

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

    public async Task TryPostOrUpdateStagingCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string? docsUrl,
        string? samplesUrl,
        CancellationToken cancellationToken = default)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        var body = BuildCommentBody(docsUrl, samplesUrl);
        var comments = await _github.ListIssueCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
        var existingId = FindCommentIdByMarker(comments, Marker);

        if (existingId.HasValue)
        {
            var ok = await _github.UpdateIssueCommentAsync(owner, repo, existingId.Value, pat, body, cancellationToken);
            if (!ok)
                _logger.LogWarning("Failed to update staging comment {CommentId} on PR #{Pr}", existingId.Value, prNumber);
            else
                _logger.LogInformation("Updated staging links comment on PR #{Pr}", prNumber);
        }
        else
        {
            var id = await _github.CreateIssueCommentAsync(owner, repo, prNumber, pat, body, cancellationToken);
            if (id == null)
                _logger.LogWarning("Failed to create staging comment on PR #{Pr}", prNumber);
            else
                _logger.LogInformation("Posted staging links comment on PR #{Pr}", prNumber);
        }
    }

    private static string BuildCommentBody(string? docsUrl, string? samplesUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Marker);
        sb.AppendLine();
        sb.AppendLine("### Staging preview");
        sb.AppendLine();
        sb.AppendLine(FormatLinkLine("Docs", docsUrl));
        sb.AppendLine(FormatLinkLine("Samples", samplesUrl));
        return sb.ToString();
    }

    private static string FormatLinkLine(string label, string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            return $"- **{label}:** [{label}]({url})";
        return $"- **{label}:** _pending_";
    }

    private static long? FindCommentIdByMarker(
        IReadOnlyList<GitHubIssueComment> comments,
        string marker)
    {
        foreach (var c in comments)
        {
            if (c.Body.Contains(marker, StringComparison.Ordinal))
                return c.Id;
        }

        return null;
    }
}
