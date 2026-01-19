namespace GitHubWrapped.Services;

using GitHubWrapped.Models;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// Client for working with GitHub API
/// </summary>
public class GitHubApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Get contribution streak from GitHub GraphQL API (same as GitHub Profile)
    /// Returns (longestStreak, currentStreak, totalContributionDays)
    /// </summary>
    public async Task<(int longestStreak, int currentStreak, int totalDays)> GetContributionStreakAsync(
        string accessToken,
        string username,
        GitHubStatsOptions options)
    {
        using var httpClient = CreateClient();
        var startDate = options.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endDate = options.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var query = @"
            query($login: String!, $from: DateTime!, $to: DateTime!) {
              user(login: $login) {
                contributionsCollection(from: $from, to: $to) {
                  contributionCalendar {
                    totalContributions
                    weeks {
                      contributionDays {
                        date
                        contributionCount
                      }
                    }
                  }
                }
              }
            }
        ";

        var graphqlQuery = new
        {
            query,
            variables = new
            {
                login = username,
                from = startDate,
                to = endDate
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(graphqlQuery),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (0, 0, 0);
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("user", out var user) ||
                !user.TryGetProperty("contributionsCollection", out var contributions))
            {
                return (0, 0, 0);
            }

            var calendar = contributions.GetProperty("contributionCalendar");
            var totalContributions = calendar.GetProperty("totalContributions").GetInt32();
            
            // Get all contribution days
            var contributionDays = new List<DateTime>();
            
            foreach (var week in calendar.GetProperty("weeks").EnumerateArray())
            {
                foreach (var day in week.GetProperty("contributionDays").EnumerateArray())
                {
                    var count = day.GetProperty("contributionCount").GetInt32();
                    if (count > 0)
                    {
                        var date = DateTime.Parse(day.GetProperty("date").GetString() ?? "");
                        contributionDays.Add(date);
                    }
                }
            }
            
            var (longestStreak, currentStreak) = CalculateStreakFromDays(contributionDays);
            
            return (longestStreak, currentStreak, contributionDays.Count);
        }
        catch (Exception)
        {
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Get GitHub username for the authenticated user
    /// </summary>
    public async Task<string?> GetUsernameAsync(string accessToken)
    {
        using var httpClient = CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "https://api.github.com/user", accessToken);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("login").GetString();
    }

    /// <summary>
    /// Get repositories for the authenticated user
    /// </summary>
    public async Task<List<GitHubRepository>> GetRepositoriesAsync(string accessToken, GitHubStatsOptions options)
    {
        var repos = new List<GitHubRepository>();
        using var httpClient = CreateClient();

        for (var page = 1; page <= options.MaxPages; page++)
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                $"https://api.github.com/user/repos?type=all&sort=updated&per_page={options.PerPage}&page={page}",
                accessToken
            );

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) break;

            var pageRepos = await ParseRepositoriesAsync(response);
            if (pageRepos.Count == 0) break;

            repos.AddRange(pageRepos);
            if (pageRepos.Count < options.PerPage) break;
        }

        // Fetch language statistics for active repositories
        await EnrichWithLanguagesAsync(httpClient, accessToken, repos, options);

        return repos;
    }

    /// <summary>
    /// Get commits using GitHub GraphQL API (same as GitHub Profile)
    /// This is the official way GitHub calculates contributions
    /// </summary>
    public async Task<List<GitHubCommit>> GetCommitsAsync(
        string accessToken, 
        string username, 
        GitHubStatsOptions options)
    {
        // Try GraphQL first (official GitHub way)
        var commits = await GetCommitsViaGraphQLAsync(accessToken, username, options);
        
        if (commits.Count > 0)
        {
            return commits;
        }

        // Fallback to REST API if GraphQL fails
        return await GetCommitsViaRestAsync(accessToken, username, options);
    }

    /// <summary>
    /// Get commits via GraphQL API (preferred method, same as GitHub Profile)
    /// </summary>
    private async Task<List<GitHubCommit>> GetCommitsViaGraphQLAsync(
        string accessToken,
        string username,
        GitHubStatsOptions options)
    {
        using var httpClient = CreateClient();
        var startDate = options.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endDate = options.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var query = @"
            query($login: String!, $from: DateTime!, $to: DateTime!) {
              user(login: $login) {
                contributionsCollection(from: $from, to: $to) {
                  commitContributionsByRepository {
                    repository {
                      nameWithOwner
                      isFork
                    }
                    contributions(first: 100) {
                      nodes {
                        commitCount
                        occurredAt
                      }
                      totalCount
                    }
                  }
                  totalCommitContributions
                }
              }
            }
        ";

        var graphqlQuery = new
        {
            query,
            variables = new
            {
                login = username,
                from = startDate,
                to = endDate
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(graphqlQuery),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<GitHubCommit>();
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("user", out var user) ||
                !user.TryGetProperty("contributionsCollection", out var contributions))
            {
                return new List<GitHubCommit>();
            }

            var totalCommits = contributions.GetProperty("totalCommitContributions").GetInt32();

            // For detailed commit info, we still need to fetch from each repo
            var reposByType = contributions.GetProperty("commitContributionsByRepository").EnumerateArray()
                .Select(r => new
                {
                    Name = r.GetProperty("repository").GetProperty("nameWithOwner").GetString() ?? "",
                    IsFork = r.GetProperty("repository").GetProperty("isFork").GetBoolean(),
                    Count = r.GetProperty("contributions").GetProperty("totalCount").GetInt32()
                })
                .Where(r => r.Count > 0)
                .ToList();

            var forkCount = reposByType.Count(r => r.IsFork);
            var ownedCount = reposByType.Count(r => !r.IsFork);
            var forkCommits = reposByType.Where(r => r.IsFork).Sum(r => r.Count);
            var ownedCommits = reposByType.Where(r => !r.IsFork).Sum(r => r.Count);

            // Filter out forks if needed
            var reposToFetch = options.IncludeForks 
                ? reposByType 
                : reposByType.Where(r => !r.IsFork).ToList();

            // Now fetch detailed commits from each repo
            var commits = await FetchCommitsFromRepositoriesAsync(httpClient, accessToken, username, reposToFetch.Select(r => r.Name).ToList(), options);
            
            return commits;
        }
        catch (Exception)
        {
            return new List<GitHubCommit>();
        }
    }

    /// <summary>
    /// Fallback: Get commits via REST API Search (has 1000 limit)
    /// </summary>
    private async Task<List<GitHubCommit>> GetCommitsViaRestAsync(
        string accessToken,
        string username,
        GitHubStatsOptions options)
    {
        var commits = new List<GitHubCommit>();
        using var httpClient = CreateClient();

        var startDate = options.StartDate.ToString("yyyy-MM-dd");
        var endDate = options.EndDate.ToString("yyyy-MM-dd");

        for (var page = 1; page <= options.MaxPages; page++)
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                $"https://api.github.com/search/commits?q=author:{username}+committer-date:{startDate}..{endDate}&sort=committer-date&order=desc&per_page={options.PerPage}&page={page}",
                accessToken
            );

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.cloak-preview+json"));

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) break;

            var pageCommits = await ParseCommitsAsync(response, options);
            if (pageCommits.Count == 0) break;

            commits.AddRange(pageCommits);
            if (pageCommits.Count < options.PerPage) break;
        }

        return commits;
    }

    /// <summary>
    /// Fetch detailed commits from specific repositories
    /// </summary>
    private async Task<List<GitHubCommit>> FetchCommitsFromRepositoriesAsync(
        HttpClient httpClient,
        string accessToken,
        string username,
        List<string> repoNames,
        GitHubStatsOptions options)
    {
        var allCommits = new List<GitHubCommit>();
        var semaphore = new SemaphoreSlim(5); // Max 5 concurrent requests

        var tasks = repoNames.Select(async repoFullName =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await FetchRepoCommitsAsync(httpClient, accessToken, repoFullName, username, options);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        foreach (var repoCommits in results)
        {
            allCommits.AddRange(repoCommits);
        }

        // Deduplicate by SHA (important for cross-repo commits)
        allCommits = allCommits
            .GroupBy(c => c.Sha)
            .Select(g => g.First())
            .OrderByDescending(c => c.Date)
            .ToList();

        return allCommits;
    }

    /// <summary>
    /// Fetch all commits from a specific repository (no 1000 limit)
    /// </summary>
    private async Task<List<GitHubCommit>> FetchRepoCommitsAsync(
        HttpClient httpClient,
        string accessToken,
        string repoFullName,
        string username,
        GitHubStatsOptions options)
    {
        var commits = new List<GitHubCommit>();
        var startDate = options.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endDate = options.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

        try
        {
            for (var page = 1; page <= 100; page++) // Safety limit per repo
            {
                using var request = CreateRequest(
                    HttpMethod.Get,
                    $"https://api.github.com/repos/{repoFullName}/commits?author={username}&since={startDate}&until={endDate}&per_page=100&page={page}",
                    accessToken
                );

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) break;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var pageCommits = doc.RootElement.EnumerateArray()
                    .Select(e => new GitHubCommit(
                        Sha: e.GetProperty("sha").GetString() ?? "",
                        Date: e.GetProperty("commit").GetProperty("author").GetProperty("date").GetDateTime(),
                        Message: e.GetProperty("commit").GetProperty("message").GetString() ?? "",
                        RepoName: repoFullName
                    ))
                    .Where(c => c.Date >= options.StartDate && c.Date <= options.EndDate)
                    .ToList();

                if (pageCommits.Count == 0) break;

                commits.AddRange(pageCommits);

                if (pageCommits.Count < 100) break;
            }
        }
        catch (Exception)
        {
        }

        return commits;
    }

    /// <summary>
    /// Get pull requests using GitHub GraphQL API (same as GitHub Profile)
    /// This is the official way GitHub calculates PR contributions
    /// </summary>
    public async Task<List<GitHubPullRequest>> GetPullRequestsAsync(
        string accessToken, 
        string username, 
        GitHubStatsOptions options)
    {
        // Try GraphQL first (official GitHub way)
        var pullRequests = await GetPullRequestsViaGraphQLAsync(accessToken, username, options);
        
        if (pullRequests.Count > 0)
        {
            return pullRequests;
        }

        // Fallback to REST API if GraphQL fails
        return await GetPullRequestsViaRestAsync(accessToken, username, options);
    }

    /// <summary>
    /// Get pull requests via GraphQL API (preferred method, same as GitHub Profile)
    /// </summary>
    private async Task<List<GitHubPullRequest>> GetPullRequestsViaGraphQLAsync(
        string accessToken,
        string username,
        GitHubStatsOptions options)
    {
        using var httpClient = CreateClient();
        var startDate = options.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endDate = options.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var query = @"
            query($login: String!, $from: DateTime!, $to: DateTime!) {
              user(login: $login) {
                contributionsCollection(from: $from, to: $to) {
                  pullRequestContributionsByRepository {
                    repository {
                      nameWithOwner
                      isFork
                    }
                    contributions(first: 100) {
                      nodes {
                        pullRequest {
                          number
                          title
                          state
                          createdAt
                          mergedAt
                          url
                        }
                      }
                      totalCount
                    }
                  }
                  totalPullRequestContributions
                }
              }
            }
        ";

        var graphqlQuery = new
        {
            query,
            variables = new
            {
                login = username,
                from = startDate,
                to = endDate
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(graphqlQuery),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<GitHubPullRequest>();
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("user", out var user) ||
                !user.TryGetProperty("contributionsCollection", out var contributions))
            {
                return new List<GitHubPullRequest>();
            }

            var totalPRs = contributions.GetProperty("totalPullRequestContributions").GetInt32();

            var pullRequests = new List<GitHubPullRequest>();
            var prsByRepo = contributions.GetProperty("pullRequestContributionsByRepository").EnumerateArray();

            var filteredByFork = 0;
            var filteredByDate = 0;

            foreach (var repoContrib in prsByRepo)
            {
                var repo = repoContrib.GetProperty("repository");
                var isFork = repo.GetProperty("isFork").GetBoolean();
                var repoName = repo.GetProperty("nameWithOwner").GetString() ?? "";

                // Filter out forks if needed
                if (!options.IncludeForks && isFork)
                {
                    var forkPRCount = repoContrib.GetProperty("contributions").GetProperty("totalCount").GetInt32();
                    filteredByFork += forkPRCount;
                    continue;
                }

                var prNodes = repoContrib.GetProperty("contributions").GetProperty("nodes").EnumerateArray();

                foreach (var node in prNodes)
                {
                    var pr = node.GetProperty("pullRequest");
                    
                    var createdAt = pr.GetProperty("createdAt").GetDateTime();
                    
                    // Double-check date range
                    if (createdAt < options.StartDate || createdAt > options.EndDate)
                    {
                        filteredByDate++;
                        continue;
                    }

                    var mergedAt = pr.TryGetProperty("mergedAt", out var ma) && ma.ValueKind != JsonValueKind.Null
                        ? ma.GetDateTime()
                        : (DateTime?)null;

                    pullRequests.Add(new GitHubPullRequest(
                        Number: pr.GetProperty("number").GetInt32(),
                        Title: pr.GetProperty("title").GetString() ?? "",
                        State: pr.GetProperty("state").GetString()?.ToLower() ?? "",
                        CreatedAt: createdAt,
                        MergedAt: mergedAt,
                        HtmlUrl: pr.GetProperty("url").GetString() ?? ""
                    ));
                }
            }

            return pullRequests;
        }
        catch (Exception)
        {
            return new List<GitHubPullRequest>();
        }
    }

    /// <summary>
    /// Fallback: Get pull requests via REST API Search (has 1000 limit)
    /// </summary>
    private async Task<List<GitHubPullRequest>> GetPullRequestsViaRestAsync(
        string accessToken,
        string username,
        GitHubStatsOptions options)
    {
        var pullRequests = new List<GitHubPullRequest>();
        using var httpClient = CreateClient();

        var startDate = options.StartDate.ToString("yyyy-MM-dd");
        var endDate = options.EndDate.ToString("yyyy-MM-dd");

        for (var page = 1; page <= options.MaxPages; page++)
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                $"https://api.github.com/search/issues?q=author:{username}+type:pr+created:{startDate}..{endDate}&per_page={options.PerPage}&page={page}&sort=created&order=desc",
                accessToken
            );

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) break;

            var pagePRs = await ParsePullRequestsAsync(response);
            if (pagePRs.Count == 0) break;

            pullRequests.AddRange(pagePRs);
            if (pagePRs.Count < options.PerPage) break;
        }

        return pullRequests;
    }

    #region Private Methods

    /// <summary>
    /// Calculate longest and current streak from contribution days (GitHub Profile method)
    /// </summary>
    private (int longestStreak, int currentStreak) CalculateStreakFromDays(List<DateTime> contributionDays)
    {
        if (contributionDays.Count == 0) return (0, 0);

        // Sort days
        contributionDays = contributionDays.OrderBy(d => d).ToList();

        var longestStreak = 1;
        var currentStreak = 1;
        var tempStreak = 1;

        // Calculate longest streak
        for (int i = 1; i < contributionDays.Count; i++)
        {
            var daysDiff = (contributionDays[i] - contributionDays[i - 1]).Days;
            
            if (daysDiff == 1)
            {
                tempStreak++;
                longestStreak = Math.Max(longestStreak, tempStreak);
            }
            else
            {
                tempStreak = 1;
            }
        }

        // Calculate current streak (from today backwards)
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        
        // Check if there's activity today or yesterday
        if (!contributionDays.Any(d => d.Date == today || d.Date == yesterday))
        {
            currentStreak = 0;
        }
        else
        {
            // Find the most recent contribution date
            var lastContribution = contributionDays.Max();
            
            // If last contribution is today or yesterday, calculate streak
            if ((today - lastContribution.Date).Days <= 1)
            {
                currentStreak = 1;
                var checkDate = lastContribution.Date.AddDays(-1);
                
                while (contributionDays.Any(d => d.Date == checkDate))
                {
                    currentStreak++;
                    checkDate = checkDate.AddDays(-1);
                }
            }
            else
            {
                currentStreak = 0;
            }
        }

        return (longestStreak, currentStreak);
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("GitHubAuth");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<List<GitHubRepository>> ParseRepositoriesAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray()
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
                    ? p.GetDateTime() : null,
                Languages: new Dictionary<string, long>()
            ))
            .ToList();
    }

    private async Task EnrichWithLanguagesAsync(
        HttpClient httpClient, 
        string accessToken, 
        List<GitHubRepository> repos, 
        GitHubStatsOptions options)
    {
        var reposToFetch = repos
            .Where(r => r.PushedAt.HasValue && r.PushedAt >= options.StartDate)
            .Take(options.MaxRepositories)
            .ToList();

        var tasks = reposToFetch.Select(async repo =>
        {
            try
            {
                using var request = CreateRequest(
                    HttpMethod.Get,
                    $"https://api.github.com/repos/{repo.FullName}/languages",
                    accessToken
                );

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var languages = doc.RootElement.EnumerateObject()
                        .ToDictionary(prop => prop.Name, prop => prop.Value.GetInt64());

                    var index = repos.FindIndex(r => r.FullName == repo.FullName);
                    if (index >= 0)
                    {
                        repos[index] = repo with { Languages = languages };
                    }
                }
            }
            catch
            {
                // Ignore errors for individual repositories
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<List<GitHubCommit>> ParseCommitsAsync(HttpResponseMessage response, GitHubStatsOptions options)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(e => new GitHubCommit(
                Sha: e.GetProperty("sha").GetString() ?? "",
                Date: e.GetProperty("commit").GetProperty("committer").GetProperty("date").GetDateTime(),
                Message: e.GetProperty("commit").GetProperty("message").GetString() ?? "",
                RepoName: e.GetProperty("repository").GetProperty("full_name").GetString() ?? ""
            ))
            .Where(c => c.Date >= options.StartDate && c.Date <= options.EndDate)
            .ToList();
    }

    private async Task<List<GitHubPullRequest>> ParsePullRequestsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items").EnumerateArray()
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
            ))
            .ToList();
    }

    #endregion
}

