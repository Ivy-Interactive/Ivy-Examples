namespace GitHubWrapped.Services;

using GitHubWrapped.Models;

/// <summary>
/// Service for collecting and processing GitHub statistics
/// </summary>
public class GitHubStatsService
{
    private readonly GitHubApiClient _apiClient;
    private readonly GitHubStatsOptions _options = new();

    public GitHubStatsService(GitHubApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Fetch GitHub statistics for the current user
    /// </summary>
    public async Task<GitHubStats?> FetchStatsAsync(IAuthService authService)
    {
        // Get access token
        var token = authService.GetAuthSession().AuthToken?.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        // Get user information
        var userInfo = await authService.GetUserInfoAsync();
        if (userInfo == null)
        {
            return null;
        }

        // Get GitHub username
        var username = await _apiClient.GetUsernameAsync(token);
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        // Fetch data from GitHub API
        var repositories = await _apiClient.GetRepositoriesAsync(token, _options);
        var commits = await _apiClient.GetCommitsAsync(token, username, _options);
        var pullRequests = await _apiClient.GetPullRequestsAsync(token, username, _options);
        
        // Get contribution streak from GraphQL (official GitHub calculation)
        var (longestStreak, currentStreak, totalDays) = await _apiClient.GetContributionStreakAsync(token, username, _options);

        // Calculate statistics
        var calculator = new GitHubStatsCalculator(_options);
        
        var commitsByMonth = calculator.CalculateCommitsByMonth(commits);
        var languageBreakdown = calculator.CalculateLanguageBreakdown(repositories);
        var topRepos = calculator.CalculateTopRepos(repositories, commits);
        var (prsCreated, prsMerged) = calculator.CalculatePullRequestStats(pullRequests);
        var starsReceived = calculator.CalculateStarsReceived(repositories);

        return new GitHubStats(
            UserInfo: userInfo,
            TotalCommits: commits.Count,
            CommitsByMonth: commitsByMonth,
            LanguageBreakdown: languageBreakdown,
            TopRepos: topRepos,
            PullRequestsCreated: prsCreated,
            PullRequestsMerged: prsMerged,
            LongestStreak: longestStreak,
            TotalContributionDays: totalDays,
            StarsGiven: 0, // Requires separate API call
            StarsReceived: starsReceived
        );
    }
}

