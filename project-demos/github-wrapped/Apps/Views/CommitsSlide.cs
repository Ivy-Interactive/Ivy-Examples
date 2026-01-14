namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class CommitsSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public CommitsSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var maxCommits = _stats.CommitsByMonth.Values.Max();
        
        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(2).Align(Align.Center)
                  | Icons.Github.ToIcon()
                  | Text.H1(_stats.TotalCommits.ToString())
                  | Text.H3("Commits in 2025").Muted())
               | new Card(Layout.Vertical().Gap(4)
                   | (Layout.Horizontal().Align(Align.Center)
                      | Text.H4("Monthly Breakdown"))
                   | (Layout.Vertical().Gap(2)
                      | BuildMonthlyChart(maxCommits)));
    }

    private object BuildMonthlyChart(int maxCommits)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var progressState = this.UseState(0);
        
        var rows = months.Select(month =>
        {
            var count = _stats.CommitsByMonth.GetValueOrDefault(month, 0);
            var percentage = maxCommits > 0 ? (int)Math.Round(count * 100.0 / maxCommits) : 0;
            
            return Layout.Horizontal().Gap(3).Align(Align.Center)
                   | Text.Small(month).Width(40)
                   | (Layout.Horizontal()
                      | Text.Block($"{new string('█', Math.Max(1, percentage / 5))}{new string('░', Math.Max(1, (100 - percentage) / 5))}"))
                        .Width(Size.Fraction(1))
                   | (Layout.Horizontal().Align(Align.Right)
                      | Text.Small(count.ToString())).Width(40);
        });

        return Layout.Vertical().Gap(2) | rows;
    }
}
