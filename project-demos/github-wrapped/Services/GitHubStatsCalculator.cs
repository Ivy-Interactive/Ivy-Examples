namespace GitHubWrapped.Services;

using GitHubWrapped.Models;

/// <summary>
/// Calculator for GitHub statistics
/// </summary>
public class GitHubStatsCalculator
{
    private readonly GitHubStatsOptions _options;

    public GitHubStatsCalculator(GitHubStatsOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Calculate number of commits by month
    /// </summary>
    public Dictionary<string, int> CalculateCommitsByMonth(List<GitHubCommit> commits)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var commitsByMonth = months.ToDictionary(m => m, _ => 0);

        foreach (var commit in commits)
        {
            var monthName = months[commit.Date.Month - 1];
            commitsByMonth[monthName]++;
        }

        return commitsByMonth;
    }

    /// <summary>
    /// Calculate breakdown by programming languages
    /// </summary>
    public Dictionary<string, long> CalculateLanguageBreakdown(List<GitHubRepository> repos)
    {
        var languageBytes = new Dictionary<string, long>();

        foreach (var repo in repos)
        {
            if (repo.Languages == null || repo.Languages.Count == 0)
                continue;

            foreach (var (language, bytes) in repo.Languages)
            {
                if (!languageBytes.ContainsKey(language))
                {
                    languageBytes[language] = 0;
                }
                languageBytes[language] += bytes;
            }
        }

        return languageBytes
            .OrderByDescending(kvp => kvp.Value)
            .Take(_options.MaxLanguages)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Calculate top repositories by commit count
    /// </summary>
    public List<RepoStats> CalculateTopRepos(List<GitHubRepository> repos, List<GitHubCommit> commits)
    {
        var repoCommitCounts = commits
            .GroupBy(c => c.RepoName)
            .ToDictionary(g => g.Key, g => g.Count());

        var repoLookup = repos.ToDictionary(r => r.FullName);

        return repoCommitCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(_options.MaxTopRepos)
            .Select(kvp => CreateRepoStats(kvp.Key, kvp.Value, repoLookup))
            .ToList();
    }

    /// <summary>
    /// Calculate longest commit streak and total contribution days
    /// DEPRECATED: Use GitHubApiClient.GetContributionStreakAsync() instead for accurate GitHub Profile calculation
    /// This method only counts commit days, which may differ from GitHub Profile
    /// </summary>
    [Obsolete("Use GitHubApiClient.GetContributionStreakAsync() for accurate GitHub Profile streak calculation")]
    public (int longestStreak, int totalDays) CalculateContributionStreak(List<GitHubCommit> commits)
    {
        if (commits.Count == 0)
        {
            return (0, 0);
        }

        var contributionDays = commits
            .Select(c => c.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var totalDays = contributionDays.Count;
        var longestStreak = CalculateLongestStreak(contributionDays);

        return (longestStreak, totalDays);
    }

    /// <summary>
    /// Calculate pull request statistics
    /// </summary>
    public (int created, int merged) CalculatePullRequestStats(List<GitHubPullRequest> pullRequests)
    {
        var created = pullRequests.Count;
        var merged = pullRequests.Count(pr => pr.MergedAt.HasValue);
        return (created, merged);
    }

    /// <summary>
    /// Calculate total number of stars received
    /// </summary>
    public int CalculateStarsReceived(List<GitHubRepository> repos)
    {
        return repos.Sum(r => r.StargazersCount);
    }

    #region Private Methods

    private RepoStats CreateRepoStats(string repoName, int commitCount, Dictionary<string, GitHubRepository> repoLookup)
    {
        if (repoLookup.TryGetValue(repoName, out var repo))
        {
            return new RepoStats(
                Name: repo.Name,
                HtmlUrl: repo.HtmlUrl,
                CommitCount: commitCount,
                Language: repo.Language,
                Stars: repo.StargazersCount,
                Forks: repo.ForksCount
            );
        }

        // If repository not found (possibly an external repository)
        var repoShortName = repoName.Split('/').LastOrDefault() ?? repoName;
        return new RepoStats(
            Name: repoShortName,
            HtmlUrl: $"https://github.com/{repoName}",
            CommitCount: commitCount,
            Language: null,
            Stars: 0,
            Forks: 0
        );
    }

    private int CalculateLongestStreak(List<DateTime> contributionDays)
    {
        if (contributionDays.Count == 0) return 0;

        var longestStreak = 1;
        var currentStreak = 1;

        for (int i = 1; i < contributionDays.Count; i++)
        {
            if ((contributionDays[i] - contributionDays[i - 1]).Days == 1)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    #endregion
}

