namespace GitHubDashboard.Models;

public record RepositoryInfo(
    string Name,
    string FullName,
    string Description,
    string HtmlUrl,
    string Language,
    int Stars,
    int Forks,
    int Watchers,
    int OpenIssues,
    int OpenPullRequests,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime PushedAt,
    long Size,
    bool IsPrivate,
    string DefaultBranch
);

public record CommitInfo(
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTime CommitDate,
    int Additions,
    int Deletions,
    int TotalChanges
);

public record IssueInfo(
    int Number,
    string Title,
    string State,
    string Body,
    string UserLogin,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    List<string> Labels,
    bool IsPullRequest
);

public record ContributorInfo(
    string Login,
    string AvatarUrl,
    string HtmlUrl,
    int Contributions,
    string Type
);

public record LanguageInfo(
    string Name,
    long Bytes,
    double Percentage
);

public record RepositoryStats(
    RepositoryInfo Repository,
    List<CommitInfo> RecentCommits,
    List<IssueInfo> RecentIssues,
    List<ContributorInfo> Contributors,
    List<LanguageInfo> Languages,
    Dictionary<string, int> CommitActivity,
    Dictionary<string, int> StarHistory,
    DateTime LastUpdated
);

public record GitHubApiResponse<T>(
    T Data,
    int RateLimitRemaining,
    DateTime RateLimitReset,
    bool Success,
    string? ErrorMessage
);
