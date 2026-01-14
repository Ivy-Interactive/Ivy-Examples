namespace GitHubWrapped.Services;

using GitHubWrapped.Models;

public class GitHubStatsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DateTime _year2025Start = new(2025, 1, 1);
    private readonly DateTime _year2025End = new(2025, 12, 31, 23, 59, 59);

    public GitHubStatsService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GitHubStats?> FetchStatsAsync(IAuthService authService)
    {
        var token = authService.GetAuthSession().AuthToken?.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var userInfo = await authService.GetUserInfoAsync();
        if (userInfo == null)
        {
            return null;
        }

        // Get the GitHub username (login) from the API
        var username = await FetchGitHubUsernameAsync(token);
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var repositories = await FetchRepositoriesAsync(token);
        var commits = await FetchCommitsAsync(token, repositories, username);
        var pullRequests = await FetchPullRequestsAsync(token, username);
        
        var commitsByMonth = CalculateCommitsByMonth(commits);
        var languageBreakdown = CalculateLanguageBreakdown(repositories, commits);
        var topRepos = CalculateTopRepos(repositories, commits);
        var (longestStreak, totalDays) = CalculateContributionStreak(commits);
        var (prsCreated, prsMerged) = CalculatePullRequestStats(pullRequests);
        var starsReceived = repositories.Sum(r => r.StargazersCount);

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
            StarsGiven: 0, // Would need separate API call to starred repos
            StarsReceived: starsReceived
        );
    }

    private async Task<string?> FetchGitHubUsernameAsync(string accessToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("GitHubAuth");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("login").GetString();
    }

    private async Task<List<GitHubRepository>> FetchRepositoriesAsync(string accessToken)
    {
        var repos = new List<GitHubRepository>();
        using var httpClient = _httpClientFactory.CreateClient("GitHubAuth");

        for (var page = 1; page <= 10; page++) // Limit to 1000 repos max
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/user/repos?type=all&sort=updated&per_page=100&page={page}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var pageRepos = doc.RootElement.EnumerateArray()
                .Select(e => new GitHubRepository(
                    Name: e.GetProperty("name").GetString() ?? "",
                    FullName: e.GetProperty("full_name").GetString() ?? "",
                    HtmlUrl: e.GetProperty("html_url").GetString() ?? "",
                    Language: e.TryGetProperty("language", out var l) && l.ValueKind != JsonValueKind.Null
                        ? l.GetString() : null,
                    StargazersCount: e.GetProperty("stargazers_count").GetInt32(),
                    ForksCount: e.GetProperty("forks_count").GetInt32(),
                    CreatedAt: e.GetProperty("created_at").GetDateTime(),
                    UpdatedAt: e.GetProperty("updated_at").GetDateTime(),
                    PushedAt: e.TryGetProperty("pushed_at", out var p) && p.ValueKind != JsonValueKind.Null
                        ? p.GetDateTime() : null
                )).ToList();

            if (pageRepos.Count == 0) break;
            repos.AddRange(pageRepos);
            
            if (pageRepos.Count < 100) break; // Last page
        }

        return repos;
    }

    private async Task<List<GitHubCommit>> FetchCommitsAsync(string accessToken, 
        List<GitHubRepository> repositories, string username)
    {
        var allCommits = new List<GitHubCommit>();
        using var httpClient = _httpClientFactory.CreateClient("GitHubAuth");

        // Use GitHub Search API to get all commits by the user across all repositories
        try
        {
            for (var page = 1; page <= 10; page++) // Up to 1000 commits
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/search/commits?q=author:{username}+committer-date:2025-01-01..2025-12-31&sort=committer-date&order=desc&per_page=100&page={page}");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                // Required for commit search API
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.cloak-preview+json"));

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var items = doc.RootElement.GetProperty("items").EnumerateArray()
                    .Select(e => new GitHubCommit(
                        Sha: e.GetProperty("sha").GetString() ?? "",
                        Date: e.GetProperty("commit").GetProperty("committer").GetProperty("date").GetDateTime(),
                        Message: e.GetProperty("commit").GetProperty("message").GetString() ?? "",
                        RepoName: e.GetProperty("repository").GetProperty("full_name").GetString() ?? ""
                    ))
                    .Where(c => c.Date >= _year2025Start && c.Date <= _year2025End)
                    .ToList();

                if (items.Count == 0) break;
                allCommits.AddRange(items);
                
                if (items.Count < 100) break;
            }
        }
        catch (Exception ex)
        {
            // Fallback to old method if search API fails
            Console.WriteLine($"Search API failed: {ex.Message}, falling back to repo-by-repo fetch");
            return await FetchCommitsLegacy(httpClient, accessToken, repositories, username);
        }

        return allCommits;
    }

    private async Task<List<GitHubCommit>> FetchCommitsLegacy(HttpClient httpClient, 
        string accessToken, List<GitHubRepository> repositories, string username)
    {
        var allCommits = new List<GitHubCommit>();

        // Only fetch commits from repos that were active in 2025
        var activeRepos = repositories
            .Where(r => r.PushedAt.HasValue && r.PushedAt.Value >= _year2025Start)
            .Take(100) // Increased from 50 to 100
            .ToList();

        foreach (var repo in activeRepos)
        {
            try
            {
                var commits = await FetchRepoCommitsAsync(httpClient, accessToken, repo.FullName, username);
                allCommits.AddRange(commits);
            }
            catch
            {
                // Continue with other repos if one fails
                continue;
            }
        }

        return allCommits.OrderByDescending(c => c.Date).ToList();
    }

    private async Task<List<GitHubCommit>> FetchRepoCommitsAsync(HttpClient httpClient, 
        string accessToken, string repoFullName, string username)
    {
        var commits = new List<GitHubCommit>();
        
        for (var page = 1; page <= 3; page++) // Limit pages per repo
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{repoFullName}/commits?author={username}&since={_year2025Start:yyyy-MM-ddTHH:mm:ssZ}&until={_year2025End:yyyy-MM-ddTHH:mm:ssZ}&per_page=100&page={page}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var pageCommits = doc.RootElement.EnumerateArray()
                .Select(e => new GitHubCommit(
                    Sha: e.GetProperty("sha").GetString() ?? "",
                    Date: e.GetProperty("commit").GetProperty("author").GetProperty("date").GetDateTime(),
                    Message: e.GetProperty("commit").GetProperty("message").GetString() ?? "",
                    RepoName: repoFullName
                ))
                .Where(c => c.Date >= _year2025Start && c.Date <= _year2025End)
                .ToList();

            if (pageCommits.Count == 0) break;
            commits.AddRange(pageCommits);
            
            if (pageCommits.Count < 100) break;
        }

        return commits;
    }

    private async Task<List<GitHubPullRequest>> FetchPullRequestsAsync(string accessToken, string username)
    {
        var pullRequests = new List<GitHubPullRequest>();
        using var httpClient = _httpClientFactory.CreateClient("GitHubAuth");

        try
        {
            for (var page = 1; page <= 10; page++) // Up to 1000 PRs
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/search/issues?q=author:{username}+type:pr+created:2025-01-01..2025-12-31&per_page=100&page={page}&sort=created&order=desc");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var items = doc.RootElement.GetProperty("items").EnumerateArray()
                    .Select(e => new GitHubPullRequest(
                        Number: e.GetProperty("number").GetInt32(),
                        Title: e.GetProperty("title").GetString() ?? "",
                        State: e.GetProperty("state").GetString() ?? "",
                        CreatedAt: e.GetProperty("created_at").GetDateTime(),
                        MergedAt: e.TryGetProperty("pull_request", out var pr) 
                            && pr.TryGetProperty("merged_at", out var ma) 
                            && ma.ValueKind != JsonValueKind.Null
                            ? ma.GetDateTime() : null,
                        HtmlUrl: e.GetProperty("html_url").GetString() ?? ""
                    )).ToList();

                if (items.Count == 0) break;
                pullRequests.AddRange(items);
                
                if (items.Count < 100) break;
            }
        }
        catch
        {
            // Return empty list if search fails
        }

        return pullRequests;
    }

    private Dictionary<string, int> CalculateCommitsByMonth(List<GitHubCommit> commits)
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

    private Dictionary<string, int> CalculateLanguageBreakdown(List<GitHubRepository> repos, 
        List<GitHubCommit> commits)
    {
        var languageCommits = new Dictionary<string, int>();

        foreach (var commit in commits)
        {
            var repo = repos.FirstOrDefault(r => r.FullName == commit.RepoName);
            if (repo?.Language != null)
            {
                if (!languageCommits.ContainsKey(repo.Language))
                {
                    languageCommits[repo.Language] = 0;
                }
                languageCommits[repo.Language]++;
            }
        }

        return languageCommits
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private List<RepoStats> CalculateTopRepos(List<GitHubRepository> repos, 
        List<GitHubCommit> commits)
    {
        // Group commits by repository
        var repoCommitCounts = commits
            .GroupBy(c => c.RepoName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Create a lookup for repos we have info about
        var repoLookup = repos.ToDictionary(r => r.FullName);

        // Get top 5 repos by commit count, including repos we don't own
        return repoCommitCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp =>
            {
                var repoName = kvp.Key;
                var commitCount = kvp.Value;

                // If we have repo info, use it
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

                // Otherwise, create basic stats from commit data
                var repoShortName = repoName.Split('/').LastOrDefault() ?? repoName;
                return new RepoStats(
                    Name: repoShortName,
                    HtmlUrl: $"https://github.com/{repoName}",
                    CommitCount: commitCount,
                    Language: null,
                    Stars: 0,
                    Forks: 0
                );
            })
            .ToList();
    }

    private (int longestStreak, int totalDays) CalculateContributionStreak(List<GitHubCommit> commits)
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
        var longestStreak = 0;
        var currentStreak = 1;

        for (int i = 1; i < contributionDays.Count; i++)
        {
            if ((contributionDays[i] - contributionDays[i - 1]).Days == 1)
            {
                currentStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, currentStreak);
                currentStreak = 1;
            }
        }

        longestStreak = Math.Max(longestStreak, currentStreak);
        return (longestStreak, totalDays);
    }

    private (int created, int merged) CalculatePullRequestStats(List<GitHubPullRequest> pullRequests)
    {
        var created = pullRequests.Count;
        var merged = pullRequests.Count(pr => pr.MergedAt.HasValue);
        return (created, merged);
    }
}

