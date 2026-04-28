namespace PrStagingDeploy.Models;

/// <summary>Sliplane service event (deploy status, etc.).</summary>
public record SliplaneServiceEvent(string Type, string? Message, DateTime CreatedAt, string? TriggeredBy = null, string? Reason = null);

/// <summary>Issue/PR comment (REST).</summary>
public record GitHubIssueComment(long Id, long UserId, string Body);

/// <summary>GitHub pull request info.</summary>
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

/// <summary>Staging deployment for a branch (docs + samples services).</summary>
public record StagingDeployment(
    string RepoKey,
    string BranchName,
    string BranchSafe,
    string? DocsServiceId,
    string? DocsUrl,
    string? DocsStatus,
    string? SamplesServiceId,
    string? SamplesUrl,
    string? SamplesStatus,
    DateTime DeployedAt,
    DateTime ExpiresAt,
    string Status
);
