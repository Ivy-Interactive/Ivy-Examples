namespace GitHubExample;

public record GitHubUserStats(
	int TotalStars,
	int TotalCommitsLastYear,
	int TotalPullRequests,
	int TotalIssues,
	int ContributedReposLastYear
);

[App(icon: Icons.Github, title: "GitHub")]
public class GitHubExampleApp : ViewBase
{
	public override object? Build()
	{
		var username = this.UseState<string?>(String.Empty);
		var loading = this.UseState(false);
		var user = this.UseState<GhUser?>();
		var stats = this.UseState<GitHubUserStats?>();
		var error = this.UseState<string?>();
		var client = UseService<IClientProvider>();

		async Task handleGetStats()
		{
			error.Set((string?)null);
			user.Set((GhUser?)null);
			stats.Set((GitHubUserStats?)null);
			if (string.IsNullOrWhiteSpace(username.Value))
			{
				error.Set("Please enter a GitHub username.");
				client.Toast("Please enter a GitHub username.");
				return;
			}

			loading.Set(true);
			try
			{
				// Mock data for testing
				if (username.Value.Trim().ToLower() == "example")
				{
					var mockUser = new GhUser
					{
						Login = "example",
						Name = "Example User",
						AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
						PublicRepos = 15,
						Followers = 42,
						Following = 8
					};
					
					var mockStats = new GitHubUserStats(
						TotalStars: 128,
						TotalCommitsLastYear: 156,
						TotalPullRequests: 23,
						TotalIssues: 7,
						ContributedReposLastYear: 5
					);
					
					user.Set(mockUser);
					stats.Set(mockStats);
					client.Toast($"Successfully loaded mock stats for {mockUser.Name}!");
				}
				else
				{
					using var httpClient = CreateConfiguredHttpClient();
					var ghUser = await httpClient.GetFromJsonAsync<GhUser>($"https://api.github.com/users/{username.Value.Trim()}", JsonOptions);
					if (ghUser is null) throw new Exception("User not found.");
					user.Set(ghUser);
					var computed = await ComputeStatsAsync(httpClient, ghUser);
					stats.Set(computed);
					client.Toast($"Successfully loaded stats for {ghUser.Name ?? ghUser.Login}!");
				}
			}
			catch (Exception ex)
			{
				error.Set(ex.Message);
				client.Toast(ex);
			}
			finally
			{
				loading.Set(false);
			}
		}
		object? content = null;
		if (user.Value != null && stats.Value != null)
		{
			var u = user.Value;
			var s = stats.Value!;

			var userDetails = new
			{
				Name = u.Name ?? u.Login,
				Username = u.Login,
				Avatar = u.AvatarUrl,
				PublicRepos = u.PublicRepos,
				Followers = u.Followers,
				Following = u.Following,
				TotalStars = s.TotalStars,
				TotalCommitsLastYear = s.TotalCommitsLastYear,
				TotalPullRequests = s.TotalPullRequests,
				TotalIssues = s.TotalIssues,
				ContributedReposLastYear = s.ContributedReposLastYear
			};

			var details = userDetails.ToDetails()
				.Remove(x => x.Avatar) // Remove avatar from details since we show it separately
				.Builder(x => x.Username, b => b.CopyToClipboard())
				.Builder(x => x.Name, b => b.CopyToClipboard())
				.Builder(x => x.Avatar, b => b.Link());

			content = new Card(
				Layout.Vertical().Gap(1)
					| Layout.Horizontal().Gap(2)
						| new Avatar(u.Name ?? u.Login, u.AvatarUrl)
							.Height(60)
							.Width(60)
						| Text.H3($"{u.Name ?? u.Login}'s GitHub Stats")
					| details
			).Width(Size.Fraction(0.35f));
		}

		object? rightContent = null;
		if (content != null)
		{
			rightContent = content;
		}

		var leftCard = new Card(Layout.Vertical().Gap(2)
			| Text.H1("GitHub Stats")
			| Text.Muted("Type a username and click Get Stats. Use 'example' for mock data or real GitHub usernames for live data.")
			| username.ToSearchInput(placeholder: "GitHub username (e.g. torvalds, example for mock data)")
			| (Layout.Horizontal().Gap(2)
				| new Button("Get Stats", onClick: () => { _ = handleGetStats(); })
					.Icon(Icons.ChartBar)
					.Loading(loading.Value)
					.Disabled(loading.Value)
				| new Button("Clear", onClick: () =>
				{
					username.Set("");
					user.Set((GhUser?)null);
					stats.Set((GitHubUserStats?)null);
					error.Set((string?)null);
				}).Secondary().Icon(Icons.Trash))
				| new Spacer().Height(Size.Units(5))
				| Text.Small("This demo integrates with the GitHub REST API to fetch user statistics and profile information.")
			    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [GitHub REST API](https://docs.github.com/en/rest)")
				).Width(Size.Fraction(0.35f)).Height(Size.Fit().Min(Size.Fraction(0.5f)));

		return rightContent != null 
			? (Layout.Horizontal().Gap(20).Align(Align.Center) | leftCard | rightContent)
			: Layout.Center() | leftCard;
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
		client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubExample", "1.0"));
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
		[JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
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