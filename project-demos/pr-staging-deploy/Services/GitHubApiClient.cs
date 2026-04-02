namespace PrStagingDeploy.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

    /// <summary>Open PR whose head branch matches <paramref name="headBranch"/> (same repo: <c>owner:branch</c>).</summary>
    public async Task<int?> FindOpenPullRequestNumberByHeadBranchAsync(
        string owner,
        string repo,
        string? token,
        string headBranch,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(headBranch))
            return null;
        var client = CreateClient(token);
        var head = $"{owner}:{headBranch}";
        var url = $"https://api.github.com/repos/{owner}/{repo}/pulls?state=open&head={Uri.EscapeDataString(head)}&per_page=5";
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        if (arr.GetArrayLength() == 0)
            return null;
        return arr[0].GetProperty("number").GetInt32();
    }

    public async Task<IReadOnlyList<GitHubIssueComment>> ListIssueCommentsAsync(
        string owner,
        string repo,
        int issueNumber,
        string? token,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        var results = new List<GitHubIssueComment>();
        var nextUrl = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/comments?per_page=100";
        while (!string.IsNullOrEmpty(nextUrl))
        {
            using var response = await client.GetAsync(nextUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                break;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetProperty("id").GetInt64();
                var userId = el.GetProperty("user").GetProperty("id").GetInt64();
                var body = el.GetProperty("body").GetString() ?? "";
                results.Add(new GitHubIssueComment(id, userId, body));
            }

            nextUrl = ParseNextLink(response.Headers);
        }

        return results;
    }

    private static string? ParseNextLink(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Link", out var values))
            return null;
        foreach (var linkHeader in values)
        {
            foreach (var part in linkHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var semi = part.IndexOf(';', StringComparison.Ordinal);
                if (semi < 0) continue;
                var urlPart = part[..semi].Trim();
                var relPart = part[(semi + 1)..].Trim();
                if (!relPart.Contains("rel=\"next\"", StringComparison.Ordinal) && !relPart.Contains("rel='next'", StringComparison.Ordinal))
                    continue;
                if (urlPart.Length >= 2 && urlPart[0] == '<' && urlPart[^1] == '>')
                    return urlPart[1..^1];
            }
        }

        return null;
    }

    public async Task<long?> CreateIssueCommentAsync(
        string owner,
        string repo,
        int issueNumber,
        string? token,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;
        var client = CreateClient(token);
        var payload = JsonSerializer.Serialize(new { body });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/comments",
            content,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt64();
    }

    public async Task<bool> UpdateIssueCommentAsync(
        string owner,
        string repo,
        long commentId,
        string? token,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        var client = CreateClient(token);
        var payload = JsonSerializer.Serialize(new { body });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PatchAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{commentId}",
            content,
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteIssueCommentAsync(
        string owner,
        string repo,
        long commentId,
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        var client = CreateClient(token);
        var response = await client.DeleteAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{commentId}",
            cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    public async Task<bool> AddReactionToIssueCommentAsync(
        string owner,
        string repo,
        long commentId,
        string reactionContent,
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var client = CreateClient(token);
        var payload = JsonSerializer.Serialize(new { content = reactionContent });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{commentId}/reactions",
            content,
            cancellationToken);
        return response.IsSuccessStatusCode;
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
