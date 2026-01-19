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
        var isSheetOpen = this.UseState<bool>(false);
        var searchText = this.UseState<string>("");

        return new Fragment()
               | (Layout.Center()
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
                       | BuildRepositoriesButton(repositories.Value, isSheetOpen))
                       .Width(Size.Fraction(0.3f)))
               | (isSheetOpen.Value ? BuildRepositoriesSheet(repositories.Value, isSheetOpen, searchText, client) : null);
    }

    private object BuildRepositoriesButton(List<GitHubRepo>? repos, IState<bool> isSheetOpen)
    {
        var count = repos?.Count ?? 0;
        var buttonText = count == 0
            ? "Repositories (No repositories found)"
            : $"Repositories ({count})";

        return new Button(buttonText, variant: ButtonVariant.Outline)
            .HandleClick(_ => isSheetOpen.Set(true))
            .Disabled(count == 0)
            .Width(Size.Full());
    }

    private object? BuildRepositoriesSheet(List<GitHubRepo>? repos, IState<bool> isSheetOpen, IState<string> searchText, IClientProvider client)
    {
        var filteredRepos = repos.Where(repo =>
            string.IsNullOrWhiteSpace(searchText.Value) ||
            repo.Name.Contains(searchText.Value, StringComparison.OrdinalIgnoreCase) ||
            (repo.Language != null && repo.Language.Contains(searchText.Value, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        var repoCards = filteredRepos.Select(repo =>
        {
            var updatedText = repo.UpdatedAt.ToString("MMM dd, yyyy");

            var details = new
            {
                Language = repo.Language,
                Stars = repo.StargazersCount == 0 ? (int?)null : repo.StargazersCount,
                Forks = repo.ForksCount == 0 ? (int?)null : repo.ForksCount,
                Updated = updatedText,
            }.ToDetails()
                .RemoveEmpty();
            
            var content = Layout.Vertical().Align(Align.Center)
                | Text.Literal(repo.Name).Italic().Bold()
                | details;
            
            return new Card(content)
                .HandleClick(_ => client.OpenUrl(repo.HtmlUrl));
        });

        var content = Layout.Vertical().Gap(3)
            | searchText.ToTextInput(placeholder: "Search repositories...")
            | (filteredRepos.Count == 0
                ? new Card(Text.Block("No repositories match your search.")).Title("No Results")
                : Layout.Grid().Gap(2) | repoCards);

        return new Sheet(
            async (Event<Sheet> _) =>
            {
                isSheetOpen.Set(false);
                searchText.Set(""); // Reset search when closing
            },
            content,
            title: "Repositories",
            description: $"Found {filteredRepos.Count} of {repos.Count} repositories"
        ).Width(Size.Fraction(0.25f));
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

