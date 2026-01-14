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
        var userName = _stats.UserInfo.FullName ?? _stats.UserInfo.Id;

        return Layout.Vertical().Gap(4).Align(Align.Center)
                   | (Layout.Vertical().Gap(2).Align(Align.Center)
                      | new Avatar(userName, _stats.UserInfo.AvatarUrl)
                          .Height(80).Width(80)
                      | Text.H1($"Hey, {userName}!").Bold()
                      | Text.Block("Your GitHub Wrapped 2025 is ready").Muted())
                   | (Layout.Vertical().Gap(1).Align(Align.Center)
                      | Text.Block("Let’s rewind what you shipped").Muted())
                   | (Layout.Grid().Gap(3).Columns(3)
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.TotalCommits.ToString()).Bold().Italic()
                          | Text.Small("Commits pushed across your repositories").Muted())
                          .Title("Commits").Icon(Icons.GitCommitVertical)
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.PullRequestsCreated.ToString()).Bold().Italic()
                          | Text.Small("Pull requests opened & reviewed").Muted())
                          .Title("Pull Requests").Icon(Icons.GitPullRequestCreate)
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Text.H2(_stats.TotalContributionDays.ToString()).Bold().Italic()
                          | Text.Small("Days you showed up and shipped code").Muted())
                          .Title("Active Days").Icon(Icons.Activity));
    }
}
