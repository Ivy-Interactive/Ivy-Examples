namespace Auth.GitHub.Test.Apps;

using System.Text.Json;
using Microsoft.Extensions.Http;

public record GitHubRepo(
    string Name,
    string HtmlUrl,
    string? Language,
    int StargazersCount,
    int ForksCount,
    DateTime UpdatedAt
);

[App(icon: Icons.Github, title: "GitHub Auth Test")]
public class TestAuthApp : ViewBase
{
    public override object? Build()
    {
        var auth = this.UseService<IAuthService>();
        var httpClientFactory = this.UseService<IHttpClientFactory>();
        
        var userInfo = this.UseState<UserInfo?>();
        var repositories = this.UseState<List<GitHubRepo>?>();
        var loading = this.UseState<bool>(true);

        this.UseEffect(async () =>
        {
            try
            {
                var info = await auth.GetUserInfoAsync();
                userInfo.Set(info);

                if (info != null)
                {
                    var token = auth.GetAuthSession().AuthToken?.AccessToken;
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        repositories.Set(await FetchRepositoriesAsync(httpClientFactory, token));
                    }
                }
            }
            finally
            {
                loading.Set(false);
            }
        });

        if (loading.Value)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Align(Align.Center)
                       | Icons.Github.ToIcon()
                       | Text.H3("GitHub Authentication Test"))
                     .Width(Size.Fraction(0.4f));
        }

        if (userInfo.Value == null)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(3)
                       | Text.H2("Not Authenticated")
                       | Text.Block("Please login via navigation bar to authenticate with GitHub."))
                     .Width(Size.Fraction(0.4f));
        }

        var client = this.UseService<IClientProvider>();

        return Layout.Vertical().Align(Align.TopCenter).Gap(3)
               | new Card(Layout.Vertical().Gap(4)
                   | (Layout.Vertical().Gap(2).Align(Align.Center)
                      | new Avatar(userInfo.Value.FullName ?? userInfo.Value.Id, userInfo.Value.AvatarUrl)
                          .Height(60).Width(60)
                      | Text.H3($"Welcome, {userInfo.Value.FullName ?? userInfo.Value.Id}!"))
                   | (new
                   {
                       UserID = userInfo.Value.Id,
                       Name = userInfo.Value.FullName ?? "N/A",
                       Email = userInfo.Value.Email ?? "N/A"
                   }).ToDetails()
                   | BuildRepositories(repositories.Value, client))
                   .Width(Size.Fraction(0.4f));
    }

    private object BuildRepositories(List<GitHubRepo>? repos, IClientProvider client)
    {
        if (repos == null || repos.Count == 0)
        {
            return new Expandable("Repositories", 
                Text.Block("No repositories found."));
        }

        var repoCards = repos.Select(repo =>
        {
            var stats = Layout.Horizontal().Gap(4).Align(Align.Center)
                | Text.Small($"Stars: {repo.StargazersCount}")
                | Text.Small($"Forks: {repo.ForksCount}");
            
            if (repo.Language != null)
            {
                stats = stats | new Badge(repo.Language, variant: BadgeVariant.Outline);
            }

            var updatedText = repo.UpdatedAt.ToString("MMM dd, yyyy");

            return new Card(Layout.Vertical().Gap(3)
                | (Layout.Horizontal().Gap(2)
                    | (Layout.Vertical().Gap(2)
                       | new Button(repo.Name, variant: ButtonVariant.Link)
                       .Url(repo.HtmlUrl))
                    | (Layout.Vertical().Gap(2).Align(Align.Right)
                       | Text.Small(updatedText).Muted()))
                | stats)
                .HandleClick(_ => client.OpenUrl(repo.HtmlUrl));
        });

        return new Expandable($"Repositories ({repos.Count})",
            Layout.Grid().Gap(2) | repoCards)
            .Open();
    }

    private async Task<List<GitHubRepo>> FetchRepositoriesAsync(
        IHttpClientFactory httpClientFactory,
        string accessToken)
    {
        var repos = new List<GitHubRepo>();
        using var httpClient = httpClientFactory.CreateClient("GitHubAuth");

        for (var page = 1; ; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/user/repos?type=owner&sort=updated&per_page=100&page={page}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"GitHub API error {(int)response.StatusCode}: {error}");
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var pageRepos = doc.RootElement.EnumerateArray()
                .Select(e => new GitHubRepo(
                    HtmlUrl: e.GetProperty("html_url").GetString() ?? "",
                    Name: e.GetProperty("name").GetString() ?? "",
                    Language: e.TryGetProperty("language", out var l) && l.ValueKind != JsonValueKind.Null
                        ? l.GetString() : null,
                    StargazersCount: e.GetProperty("stargazers_count").GetInt32(),
                    ForksCount: e.GetProperty("forks_count").GetInt32(),
                    UpdatedAt: e.GetProperty("updated_at").GetDateTime()
                )).ToList();

            if (pageRepos.Count == 0) break;
            repos.AddRange(pageRepos);
        }

        return repos;
    }
}

