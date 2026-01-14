namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class WelcomeSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public WelcomeSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(3).Align(Align.Center)
                  | new Avatar(_stats.UserInfo.FullName ?? _stats.UserInfo.Id, _stats.UserInfo.AvatarUrl)
                      .Height(120).Width(120)
                  | Text.H1($"Welcome, {_stats.UserInfo.FullName ?? _stats.UserInfo.Id}!")
                  | Text.H3("Your GitHub Wrapped 2025").Muted())
               | new Card(Layout.Vertical().Gap(4)
                   | (Layout.Horizontal().Align(Align.Center)
                      | Text.Block("Let's take a look at your GitHub journey in 2025."))
                   | (Layout.Grid().Gap(3).Columns(3)
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.TotalCommits.ToString())
                          | Text.Small("Commits").Muted())
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.PullRequestsCreated.ToString())
                          | Text.Small("Pull Requests").Muted())
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.TotalContributionDays.ToString())
                          | Text.Small("Active Days").Muted())));
    }
}
