namespace PrStagingDeploy.Services;

using System.Net.Http.Headers;
using PrStagingDeploy.Models;

/// <summary>GitHub REST API client for listing PRs.</summary>
public class GitHubApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<GitHubPullRequest>> GetPullRequestsAsync(string owner, string repo, string? token, string state = "open")
    {
        var client = CreateClient(token);
        var url = $"https://api.github.com/repos/{owner}/{repo}/pulls?state={state}&per_page=50";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<GitHubPullRequest>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var list = new List<GitHubPullRequest>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var head = el.GetProperty("head");
            list.Add(new GitHubPullRequest(
                Number: el.GetProperty("number").GetInt32(),
                Title: el.GetProperty("title").GetString() ?? "",
                HeadRef: head.GetProperty("ref").GetString() ?? "",
                HeadSha: head.GetProperty("sha").GetString() ?? "",
                HtmlUrl: el.GetProperty("html_url").GetString() ?? "",
                State: el.GetProperty("state").GetString() ?? "open",
                Author: el.TryGetProperty("user", out var u) ? u.GetProperty("login").GetString() : null,
                CreatedAt: DateTime.Parse(el.GetProperty("created_at").GetString() ?? "1970-01-01")
            ));
        }
        return list;
    }

    public async Task<string?> GetPullRequestBranchAsync(string owner, string repo, int prNumber, string? token)
    {
        var client = CreateClient(token);
        var response = await client.GetAsync($"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}");
        if (!response.IsSuccessStatusCode)
            return null;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("head").GetProperty("ref").GetString();
    }

    private HttpClient CreateClient(string? token)
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PrStagingDeploy/1.0");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
