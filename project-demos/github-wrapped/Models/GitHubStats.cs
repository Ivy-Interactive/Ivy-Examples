namespace GitHubWrapped.Models;

public record GitHubStats(
    UserInfo UserInfo,
    int TotalCommits,
    Dictionary<string, int> CommitsByMonth,
    Dictionary<string, long> LanguageBreakdown, // Changed to long to store bytes
    List<RepoStats> TopRepos,
    int PullRequestsCreated,
    int PullRequestsMerged,
    int LongestStreak,
    int TotalContributionDays,
    int StarsGiven,
    int StarsReceived
);

public record RepoStats(
    string Name,
    string HtmlUrl,
    int CommitCount,
    string? Language,
    int Stars,
    int Forks
);

public record GitHubCommit(
    string Sha,
    DateTime Date,
    string Message,
    string RepoName
);

public record GitHubPullRequest(
    int Number,
    string Title,
    string State,
    DateTime CreatedAt,
    DateTime? MergedAt,
    string HtmlUrl
);

public record GitHubRepository(
    string Name,
    string FullName,
    string HtmlUrl,
    string? Language,
    int StargazersCount,
    int ForksCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PushedAt,
    Dictionary<string, long> Languages
);

