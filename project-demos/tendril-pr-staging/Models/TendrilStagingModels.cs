namespace TendrilPrStaging.Models;

public record SliplaneServiceEvent(string Type, string? Message, DateTime CreatedAt, string? TriggeredBy = null, string? Reason = null);

public record GitHubIssueComment(long Id, long UserId, string Body);

public record GitHubPullRequest(
    int Number,
    string Title,
    string HeadRef,
    string HeadSha,
    string HtmlUrl,
    string State,
    string? Author,
    DateTime CreatedAt
);

/// <summary>Tendril staging deployment for one PR — a single Sliplane service.</summary>
public record TendrilStagingDeployment(
    string RepoKey,
    int PrNumber,
    string ServiceId,
    string? ServiceUrl,
    string? ServiceStatus,
    DateTime DeployedAt,
    DateTime ExpiresAt
);
