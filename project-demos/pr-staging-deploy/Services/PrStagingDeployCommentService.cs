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

    public async Task TryPostOrUpdateStagingCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string? docsUrl,
        string? samplesUrl,
        string? status = null,
        IReadOnlyList<string>? logLines = null,
        bool forceNewComment = false,
        CancellationToken cancellationToken = default)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        var body = BuildCommentBody(docsUrl, samplesUrl, status, logLines);
        var comments = await _github.ListIssueCommentsAsync(owner, repo, prNumber, pat, cancellationToken);
        var existingIds = FindCommentIdsByMarker(comments, Marker);

        if (forceNewComment && existingIds.Any())
        {
            foreach (var oldId in existingIds)
            {
                await _github.DeleteIssueCommentAsync(owner, repo, oldId, pat, cancellationToken);
            }
            existingIds.Clear();
        }

        if (existingIds.Any())
        {
            var existingId = existingIds.Last();
            var ok = await _github.UpdateIssueCommentAsync(owner, repo, existingId, pat, body, cancellationToken);
            if (!ok)
                _logger.LogWarning("Failed to update staging comment {CommentId} on PR #{Pr}", existingId, prNumber);
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

    public async Task TryAddRocketReactionAsync(
        string owner,
        string repo,
        long issueCommentId,
        CancellationToken cancellationToken = default)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        // Creates GitHub reaction (rocket icon) on the `/deploy` comment.
        await _github.AddReactionToIssueCommentAsync(owner, repo, issueCommentId, RocketReaction, pat, cancellationToken);
    }

    private static string BuildCommentBody(
        string? docsUrl,
        string? samplesUrl,
        string? status,
        IReadOnlyList<string>? logLines)
    {
        var statusText = string.IsNullOrWhiteSpace(status)
            ? "Staging preview"
            : status.Trim();

        var isFailed   = statusText.Contains("failed",   StringComparison.OrdinalIgnoreCase);
        var isDeployed = statusText.Contains("deployed", StringComparison.OrdinalIgnoreCase)
                         && !isFailed;
        var isDeleted  = statusText.Contains("deleted",  StringComparison.OrdinalIgnoreCase);

        // Show links only once everything is fully deployed, not during intermediate states.
        var showLinks = isDeployed && !isDeleted;

        var sb = new StringBuilder();
        sb.AppendLine(Marker);
        sb.AppendLine();
        sb.AppendLine("### " + statusText);
        sb.AppendLine();

        if (showLinks)
        {
            var docsPageUrl = BuildDocsIntroPageUrl(docsUrl);
            sb.AppendLine(FormatLinkLine("Docs", docsPageUrl));
            sb.AppendLine(FormatLinkLine("Samples", samplesUrl));
            sb.AppendLine();
        }

        // Show a descriptive prose line for non-final states.
        if (!isDeployed)
        {
            if (isFailed)
            {
                sb.AppendLine("Deployment stopped due to an error. I'm attaching the latest Sliplane events below.");
            }
            else if (statusText.Contains("redeploy", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Updating this PR deployment — I'll keep the comment updated until Sliplane finishes.");
            }
            else if (isDeleted)
            {
                sb.AppendLine("Staging services have been deleted.");
            }
            else
            {
                sb.AppendLine("I'm preparing your docs & samples for this PR. I'll update the comment as Sliplane reports progress.");
            }
        }

        if (logLines is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("### Logs");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var line in logLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                sb.AppendLine(line);
            }
            sb.AppendLine("```");
        }
        return sb.ToString();
    }

    private static string FormatLinkLine(string label, string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            return $"**{label}:** [{url}]({url})";

        return $"**{label}:** _pending_";
    }

    private static string? BuildDocsIntroPageUrl(string? docsUrl)
    {
        if (string.IsNullOrWhiteSpace(docsUrl))
            return null;

        // Sliplane docs service usually returns the managed domain for the UI.
        // We want to show a fully clickable URL, without forcing any extra path.
        return docsUrl.TrimEnd('/');
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

