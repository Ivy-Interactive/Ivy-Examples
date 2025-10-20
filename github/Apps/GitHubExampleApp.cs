using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHubExample;

public record GitHubUserStats(
	int TotalStars,
	int TotalCommitsLastYear,
	int TotalPullRequests,
	int TotalIssues,
	int ContributedReposLastYear
);

public static class GitHubConstants
{
	public const int MaxReposToProcess = 50;
}

[App(icon: Icons.User, title: "GitHub")]
public class GitHubExampleApp : ViewBase
{
	public override object? Build()
	{
		var username = this.UseState<string>();
		var loading = this.UseState(false);
		var user = this.UseState<GhUser?>();
		var stats = this.UseState<GitHubUserStats?>();
		var error = this.UseState<string?>();

		async Task handleGetStats()
		{
			error.Set((string?)null);
			user.Set((GhUser?)null);
			stats.Set((GitHubUserStats?)null);
			if (string.IsNullOrWhiteSpace(username.Value))
			{
				error.Set("Please enter a GitHub username.");
				return;
			}

			loading.Set(true);
			try
			{
				using var client = CreateConfiguredHttpClient();
				var ghUser = await client.GetFromJsonAsync<GhUser>($"https://api.github.com/users/{username.Value.Trim()}", JsonOptions);
				if (ghUser is null) throw new Exception("User not found.");
				user.Set(ghUser);
				var computed = await ComputeStatsAsync(client, ghUser);
				stats.Set(computed);
			}
			catch (Exception ex)
			{
				error.Set(ex.Message);
			}
			finally
			{
				loading.Set(false);
			}
		}

		object content = new Card(Text.Block("Stats will appear here...")).Width(Size.Units(120).Max(560));
		if (!string.IsNullOrEmpty(error.Value))
		{
			content = new Card(Text.Block($"Error: {error.Value}"));
		}
		else if (user.Value != null && stats.Value != null)
		{
			var u = user.Value;
			var s = stats.Value!;

			var statsData = new[]
			{
				new { Metric = "Total Stars Earned", Value = s.TotalStars.ToString() },
				new { Metric = "Total Commits (last year)", Value = s.TotalCommitsLastYear.ToString() },
				new { Metric = "Total PRs", Value = s.TotalPullRequests.ToString() },
				new { Metric = "Total Issues", Value = s.TotalIssues.ToString() },
				new { Metric = "Contributed to (last year)", Value = s.ContributedReposLastYear.ToString() },
				new { Metric = "Public Repos", Value = u.PublicRepos.ToString() },
				new { Metric = "Followers", Value = u.Followers.ToString() },
				new { Metric = "Following", Value = u.Following.ToString() }
			};

			var table = statsData.ToTable()
				.Width(Size.Full());

			content = new Card(
				Layout.Vertical().Gap(2)
					| Text.H3($"{u.Name ?? u.Login}'s GitHub Stats")
					| table
			).Width(Size.Units(120).Max(560));
		}

		return (Layout.Center()
			| new Card(Layout.Vertical().Gap(2)
				| Text.H1("GitHub Stats")
				| Text.Muted("Type a username and click Get Stats. Integrates Ivy with the GitHub REST API.")
				| username.ToInput(placeholder: "GitHub username (e.g. torvalds)")
				| (Layout.Horizontal().Gap(2)
					| new Button("Get Stats", onClick: () => { _ = handleGetStats(); })
						.Icon(Icons.ChartBar)
						.Loading(loading.Value)
						.Disabled(loading.Value)
					| new Button("Clear", onClick: () => { username.Set(""); })
						.Secondary()
						.Icon(Icons.Trash)
					)).Width(Size.Fraction(0.25f))
			| new Card(Layout.Vertical().Gap(2)
				| content).Width(Size.Fraction(0.4f)));
	}

	private static async Task<GitHubUserStats> ComputeStatsAsync(HttpClient client, GhUser user)
	{
		var owner = user.Login;

		var repos = new List<GhRepo>();
		var page = 1;
		while (true)
		{
			var pageItems = await client.GetFromJsonAsync<List<GhRepo>>($"https://api.github.com/users/{owner}/repos?per_page=100&page={page}", JsonOptions) ?? new List<GhRepo>();
			if (pageItems.Count == 0) break;
			repos.AddRange(pageItems);
			page++;
		}

		var nonForkRepos = repos.Where(r => !r.Fork).ToList();
		var totalStars = nonForkRepos.Sum(r => r.StargazersCount);

		var prSearch = await client.GetFromJsonAsync<SearchIssuesResponse>($"https://api.github.com/search/issues?q=type:pr+author:{owner}&per_page=1", JsonOptions);
		var totalPRs = prSearch?.TotalCount ?? 0;

		var issueSearch = await client.GetFromJsonAsync<SearchIssuesResponse>($"https://api.github.com/search/issues?q=type:issue+author:{owner}&per_page=1", JsonOptions);
		var totalIssues = issueSearch?.TotalCount ?? 0;

		var since = DateTimeOffset.UtcNow.AddYears(-1);
		var until = DateTimeOffset.UtcNow;
		var totalCommits = 0;
		var contributedRepos = 0;

		foreach (var repo in repos.Take(GitHubConstants.MaxReposToProcess))
		{
			try
			{
				var commitsForRepo = await GetCommitCountForRepo(client, owner, repo.Name, owner, since, until);
				if (commitsForRepo > 0) contributedRepos++;
				totalCommits += commitsForRepo;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error processing repo '{repo.Name}': {ex.Message}");
				if (ex.Message.Contains("409") || ex.Message.Contains("Conflict"))
					break;
			}
		}

		return new GitHubUserStats(totalStars, totalCommits, totalPRs, totalIssues, contributedRepos);
	}

	private static async Task<int> GetCommitCountForRepo(HttpClient client, string owner, string repoName, string authorLogin, DateTimeOffset since, DateTimeOffset until)
	{
		var count = 0;
		var page = 1;
		while (true)
		{
			var encodedRepoName = Uri.EscapeDataString(repoName);
			var url = $"https://api.github.com/repos/{owner}/{encodedRepoName}/commits?author={authorLogin}&since={Uri.EscapeDataString(since.ToString("o"))}&until={Uri.EscapeDataString(until.ToString("o"))}&per_page=100&page={page}";
			var commits = await client.GetFromJsonAsync<List<GhCommit>>(url, JsonOptions) ?? new List<GhCommit>();
			if (commits.Count == 0) break;
			count += commits.Count;
			page++;
		}
		return count;
	}

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNameCaseInsensitive = true
	};

	private static HttpClient CreateConfiguredHttpClient()
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Ivy-GitHub-Demo", "1.0"));
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		if (!string.IsNullOrWhiteSpace(token))
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		}
		return client;
	}

	private sealed class GhUser
	{
		[JsonPropertyName("login")] public string Login { get; set; }
		[JsonPropertyName("name")] public string? Name { get; set; }
		[JsonPropertyName("public_repos")] public int PublicRepos { get; set; }
		[JsonPropertyName("followers")] public int Followers { get; set; }
		[JsonPropertyName("following")] public int Following { get; set; }
	}

	private sealed class GhRepo
	{
		[JsonPropertyName("name")] public string Name { get; set; }
		[JsonPropertyName("fork")] public bool Fork { get; set; }
		[JsonPropertyName("stargazers_count")] public int StargazersCount { get; set; }
	}

	private sealed class GhCommit { }

	private sealed class SearchIssuesResponse
	{
		[JsonPropertyName("total_count")] public int TotalCount { get; set; }
	}
}