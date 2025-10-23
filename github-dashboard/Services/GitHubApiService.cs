using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GitHubDashboard.Services;

public interface IGitHubApiService
{
    Task<GitHubApiResponse<RepositoryInfo>> GetRepositoryInfoAsync(string owner, string repo);
    Task<GitHubApiResponse<List<CommitInfo>>> GetRecentCommitsAsync(string owner, string repo, int count = 30);
    Task<GitHubApiResponse<List<IssueInfo>>> GetRecentIssuesAsync(string owner, string repo, int count = 50);
    Task<GitHubApiResponse<List<ContributorInfo>>> GetContributorsAsync(string owner, string repo);
    Task<GitHubApiResponse<List<LanguageInfo>>> GetLanguagesAsync(string owner, string repo);
    Task<GitHubApiResponse<Dictionary<string, int>>> GetCommitActivityAsync(string owner, string repo, int weeks = 52);
}

public class GitHubApiService : IGitHubApiService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GitHubApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitHubApiService(HttpClient httpClient, IMemoryCache cache, ILogger<GitHubApiService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Set up GitHub API headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubDashboard/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<GitHubApiResponse<RepositoryInfo>> GetRepositoryInfoAsync(string owner, string repo)
    {
        var cacheKey = $"repo_info_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out RepositoryInfo? cachedRepo))
        {
            return new GitHubApiResponse<RepositoryInfo>(cachedRepo!, 5000, DateTime.UtcNow.AddHours(1), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch repository info: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new GitHubApiResponse<RepositoryInfo>(null!, 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var repoInfo = JsonSerializer.Deserialize<RepositoryInfo>(json, _jsonOptions);
            
            // Cache for 1 hour
            _cache.Set(cacheKey, repoInfo, TimeSpan.FromHours(1));
            
            return new GitHubApiResponse<RepositoryInfo>(repoInfo!, 5000, DateTime.UtcNow.AddHours(1), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository info for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<RepositoryInfo>(null!, 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    public async Task<GitHubApiResponse<List<CommitInfo>>> GetRecentCommitsAsync(string owner, string repo, int count = 30)
    {
        var cacheKey = $"commits_{owner}_{repo}_{count}";
        
        if (_cache.TryGetValue(cacheKey, out List<CommitInfo>? cachedCommits))
        {
            return new GitHubApiResponse<List<CommitInfo>>(cachedCommits!, 5000, DateTime.UtcNow.AddMinutes(30), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/commits?per_page={count}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubApiResponse<List<CommitInfo>>(new List<CommitInfo>(), 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var commits = JsonSerializer.Deserialize<List<CommitInfo>>(json, _jsonOptions) ?? new List<CommitInfo>();
            
            // Cache for 30 minutes
            _cache.Set(cacheKey, commits, TimeSpan.FromMinutes(30));
            
            return new GitHubApiResponse<List<CommitInfo>>(commits, 5000, DateTime.UtcNow.AddMinutes(30), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commits for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<List<CommitInfo>>(new List<CommitInfo>(), 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    public async Task<GitHubApiResponse<List<IssueInfo>>> GetRecentIssuesAsync(string owner, string repo, int count = 50)
    {
        var cacheKey = $"issues_{owner}_{repo}_{count}";
        
        if (_cache.TryGetValue(cacheKey, out List<IssueInfo>? cachedIssues))
        {
            return new GitHubApiResponse<List<IssueInfo>>(cachedIssues!, 5000, DateTime.UtcNow.AddMinutes(15), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues?state=all&per_page={count}&sort=updated";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubApiResponse<List<IssueInfo>>(new List<IssueInfo>(), 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var issues = JsonSerializer.Deserialize<List<IssueInfo>>(json, _jsonOptions) ?? new List<IssueInfo>();
            
            // Cache for 15 minutes
            _cache.Set(cacheKey, issues, TimeSpan.FromMinutes(15));
            
            return new GitHubApiResponse<List<IssueInfo>>(issues, 5000, DateTime.UtcNow.AddMinutes(15), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching issues for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<List<IssueInfo>>(new List<IssueInfo>(), 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    public async Task<GitHubApiResponse<List<ContributorInfo>>> GetContributorsAsync(string owner, string repo)
    {
        var cacheKey = $"contributors_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<ContributorInfo>? cachedContributors))
        {
            return new GitHubApiResponse<List<ContributorInfo>>(cachedContributors!, 5000, DateTime.UtcNow.AddHours(2), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/contributors?per_page=100";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubApiResponse<List<ContributorInfo>>(new List<ContributorInfo>(), 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var contributors = JsonSerializer.Deserialize<List<ContributorInfo>>(json, _jsonOptions) ?? new List<ContributorInfo>();
            
            // Cache for 2 hours
            _cache.Set(cacheKey, contributors, TimeSpan.FromHours(2));
            
            return new GitHubApiResponse<List<ContributorInfo>>(contributors, 5000, DateTime.UtcNow.AddHours(2), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching contributors for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<List<ContributorInfo>>(new List<ContributorInfo>(), 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    public async Task<GitHubApiResponse<List<LanguageInfo>>> GetLanguagesAsync(string owner, string repo)
    {
        var cacheKey = $"languages_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<LanguageInfo>? cachedLanguages))
        {
            return new GitHubApiResponse<List<LanguageInfo>>(cachedLanguages!, 5000, DateTime.UtcNow.AddHours(4), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/languages";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubApiResponse<List<LanguageInfo>>(new List<LanguageInfo>(), 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var languageDict = JsonSerializer.Deserialize<Dictionary<string, long>>(json, _jsonOptions) ?? new Dictionary<string, long>();
            
            var totalBytes = languageDict.Values.Sum();
            var languages = languageDict.Select(kvp => new LanguageInfo(
                kvp.Key,
                kvp.Value,
                totalBytes > 0 ? (double)kvp.Value / totalBytes * 100 : 0
            )).OrderByDescending(l => l.Bytes).ToList();
            
            // Cache for 4 hours
            _cache.Set(cacheKey, languages, TimeSpan.FromHours(4));
            
            return new GitHubApiResponse<List<LanguageInfo>>(languages, 5000, DateTime.UtcNow.AddHours(4), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching languages for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<List<LanguageInfo>>(new List<LanguageInfo>(), 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    public async Task<GitHubApiResponse<Dictionary<string, int>>> GetCommitActivityAsync(string owner, string repo, int weeks = 52)
    {
        var cacheKey = $"commit_activity_{owner}_{repo}_{weeks}";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, int>? cachedActivity))
        {
            return new GitHubApiResponse<Dictionary<string, int>>(cachedActivity!, 5000, DateTime.UtcNow.AddHours(6), true, null);
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/stats/commit_activity";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return new GitHubApiResponse<Dictionary<string, int>>(new Dictionary<string, int>(), 0, DateTime.UtcNow, false, $"API Error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var activityData = JsonSerializer.Deserialize<List<CommitActivityWeek>>(json, _jsonOptions) ?? new List<CommitActivityWeek>();
            
            var activity = new Dictionary<string, int>();
            var startDate = DateTime.UtcNow.AddDays(-weeks * 7);
            
            foreach (var week in activityData.TakeLast(weeks))
            {
                var weekStart = DateTimeOffset.FromUnixTimeSeconds(week.Week).DateTime;
                activity[weekStart.ToString("yyyy-MM-dd")] = week.Total;
            }
            
            // Cache for 6 hours
            _cache.Set(cacheKey, activity, TimeSpan.FromHours(6));
            
            return new GitHubApiResponse<Dictionary<string, int>>(activity, 5000, DateTime.UtcNow.AddHours(6), true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commit activity for {Owner}/{Repo}", owner, repo);
            return new GitHubApiResponse<Dictionary<string, int>>(new Dictionary<string, int>(), 0, DateTime.UtcNow, false, ex.Message);
        }
    }

    private record CommitActivityWeek(
        long Week,
        int Total,
        List<int> Days
    );
}
