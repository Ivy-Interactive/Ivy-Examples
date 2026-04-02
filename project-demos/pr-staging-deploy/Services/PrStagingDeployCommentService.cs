namespace PrStagingDeploy.Services;

using System.Text;
using PrStagingDeploy.Models;

/// <summary>
/// Posts a single PR comment with staging links using <c>GitHub:PrCommentToken</c> (PAT).
/// Each post removes every prior marker comment and creates a new one so the thread always shows one fresh bot message.
/// The comment appears as the PAT owner.
/// </summary>
public class PrStagingDeployCommentService
{
    public const string Marker = "<!-- ivy-staging-deploy -->";

    /// <summary>PR comment heading while a fresh deploy is queued (replaces stale "Deleted" immediately).</summary>
    public const string CommentStatusDeployQueued = "Staging deploy in progress";

    /// <summary>PR comment heading after a push triggered redeploy (matches <see cref="BuildCommentBody"/> redeploy prose).</summary>
    public const string CommentStatusRedeployQueued = "Redeploying staging";

    /// <summary>PR comment when services already exist and we only re-run the link watcher.</summary>
    public const string CommentStatusCheckingStaging = "Checking staging deployment";

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
    /// Deletes all prior staging marker comments on this PR, then posts a new comment with the given body.
    /// </summary>
    public async Task TryPostOrUpdateStagingCommentAsync(
        string owner,
        string repo,
        int prNumber,
        string? docsUrl,
        string? samplesUrl,
        string? status = null,
        IReadOnlyList<string>? logLines = null,
        CancellationToken cancellationToken = default)
    {
        var pat = _config["GitHub:PrCommentToken"] ?? "";
        if (string.IsNullOrWhiteSpace(pat))
            return;

        var body = BuildCommentBody(docsUrl, samplesUrl, status, logLines);
        await DeleteAllMarkerCommentsAsync(owner, repo, prNumber, pat, cancellationToken);

        var id = await _github.CreateIssueCommentAsync(owner, repo, prNumber, pat, body, cancellationToken);
        if (id == null)
            _logger.LogWarning("Failed to create staging comment on PR #{Pr}", prNumber);
        else
            _logger.LogInformation("Posted new staging comment on PR #{Pr} (replaced prior marker comments)", prNumber);
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

    /// <summary>
    /// Replaces the staging thread with a progress comment (deletes old marker comments, posts new).
    /// </summary>
    public Task TryNotifyDeployQueuedAsync(
        string owner,
        string repo,
        int prNumber,
        string progressStatus,
        CancellationToken cancellationToken = default)
    {
        return TryPostOrUpdateStagingCommentAsync(
            owner,
            repo,
            prNumber,
            docsUrl: null,
            samplesUrl: null,
            status: progressStatus,
            logLines: null,
            cancellationToken);
    }

    /// <summary>Removes every issue comment that contains <see cref="Marker"/> (retries per id, re-lists between rounds).</summary>
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
