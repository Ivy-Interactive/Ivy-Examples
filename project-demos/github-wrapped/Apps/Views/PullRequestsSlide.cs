namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class PullRequestsSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public PullRequestsSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var mergeRate = _stats.PullRequestsCreated > 0 
            ? (int)Math.Round(_stats.PullRequestsMerged * 100.0 / _stats.PullRequestsCreated)
            : 0;

        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(2).Align(Align.Center)
                  | Icons.Github.ToIcon()
                  | Text.H1(_stats.PullRequestsCreated.ToString())
                  | Text.H3("Pull Requests Created").Muted())
               | new Card(Layout.Grid().Gap(4).Columns(2)
                   | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                       | Text.H2(_stats.PullRequestsMerged.ToString())
                       | Text.Small("Merged").Muted())
                   | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                       | Text.H2($"{mergeRate}%")
                       | Text.Small("Merge Rate").Muted()));
    }
}
