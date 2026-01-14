namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class SummarySlide : ViewBase
{
    private readonly GitHubStats _stats;

    public SummarySlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(2).Align(Align.Center)
                  | Icons.TrendingUp.ToIcon()
                  | Text.H1("Your 2025 Highlights")
                  | Text.H3($"Great work, {_stats.UserInfo.FullName ?? _stats.UserInfo.Id}!").Muted())
               | new Card(Layout.Vertical().Gap(4)
                   | (Layout.Grid().Gap(3).Columns(2)
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Icons.Github.ToIcon()
                          | Text.H3(_stats.TotalCommits.ToString())
                          | Text.Small("Total Commits").Muted())
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Icons.Github.ToIcon()
                          | Text.H3(_stats.PullRequestsCreated.ToString())
                          | Text.Small("Pull Requests").Muted())
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Icons.Zap.ToIcon()
                          | Text.H3(_stats.LongestStreak.ToString())
                          | Text.Small("Longest Streak (days)").Muted())
                      | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                          | Icons.Calendar.ToIcon()
                          | Text.H3(_stats.TotalContributionDays.ToString())
                          | Text.Small("Active Days").Muted()))
                   | new Separator()
                   | (Layout.Vertical().Gap(2).Align(Align.Center)
                      | Text.Block("Thank you for being an awesome developer in 2025!")
                      | Text.Small("Keep coding and building amazing things in 2026!").Muted()));
    }
}
