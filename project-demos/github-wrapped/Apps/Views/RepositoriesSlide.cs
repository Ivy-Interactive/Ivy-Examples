namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class RepositoriesSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public RepositoriesSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var topRepo = _stats.TopRepos.FirstOrDefault();
        var client = this.UseService<IClientProvider>();

        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(2).Align(Align.Center)
                  | Icons.Folder.ToIcon()
                  | Text.H1(topRepo?.Name ?? "N/A")
                  | Text.H3("Your Most Active Repository").Muted()
                  | (topRepo != null 
                      ? Text.Small($"{topRepo.CommitCount} commits").Muted() 
                      : null))
               | new Card(Layout.Vertical().Gap(3)
                   | (Layout.Horizontal().Align(Align.Center)
                      | Text.H4("Top 5 Repositories"))
                   | BuildRepoList(client));
    }

    private object BuildRepoList(IClientProvider client)
    {
        if (_stats.TopRepos.Count == 0)
        {
            return Layout.Horizontal().Align(Align.Center)
                   | Text.Block("No repository data available").Muted();
        }

        var repoCards = _stats.TopRepos.Select((repo, index) =>
        {
            return new Card(Layout.Horizontal().Gap(3).Align(Align.Center)
                | Text.Small($"#{index + 1}").Width(30).Muted()
                | (Layout.Vertical().Gap(1)
                   | new Button(repo.Name, variant: ButtonVariant.Link)
                       .HandleClick(_ => client.OpenUrl(repo.HtmlUrl))
                   | (Layout.Horizontal().Gap(2)
                      | Text.Small($"{repo.CommitCount} commits")
                      | (repo.Language != null ? new Badge(repo.Language, variant: BadgeVariant.Outline) : null)))
                  .Width(Size.Fraction(1))
                | (Layout.Horizontal().Gap(2)
                   | (Layout.Horizontal().Gap(1)
                      | Icons.Star.ToIcon()
                      | Text.Small(repo.Stars.ToString()))
                   | (Layout.Horizontal().Gap(1)
                      | Icons.Github.ToIcon()
                      | Text.Small(repo.Forks.ToString()))));
        });

        return Layout.Vertical().Gap(2) | repoCards;
    }
}
