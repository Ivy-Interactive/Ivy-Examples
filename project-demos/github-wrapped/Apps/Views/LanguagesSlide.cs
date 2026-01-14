namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class LanguagesSlide : ViewBase
{
    private readonly GitHubStats _stats;

    public LanguagesSlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var totalCommits = _stats.LanguageBreakdown.Values.Sum();
        var topLanguage = _stats.LanguageBreakdown.FirstOrDefault();

        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(2).Align(Align.Center)
                  | Icons.Code.ToIcon()
                  | Text.H1(topLanguage.Key ?? "N/A")
                  | Text.H3("Your Top Language in 2025").Muted())
               | new Card(Layout.Vertical().Gap(4)
                   | (Layout.Horizontal().Align(Align.Center)
                      | Text.H4("Language Breakdown"))
                   | BuildLanguageChart(totalCommits));
    }

    private object BuildLanguageChart(int totalCommits)
    {
        if (_stats.LanguageBreakdown.Count == 0)
        {
            return Layout.Horizontal().Align(Align.Center)
                   | Text.Block("No language data available").Muted();
        }

        var rows = _stats.LanguageBreakdown.Select(kvp =>
        {
            var percentage = totalCommits > 0 ? (kvp.Value * 100.0 / totalCommits) : 0;
            
            return Layout.Horizontal().Gap(3).Align(Align.Center)
                   | new Badge(kvp.Key).Width(100)
                   | (Layout.Horizontal()
                      | Text.Block($"{new string('█', Math.Max(1, (int)(percentage / 2)))}{new string('░', Math.Max(1, (int)((100 - percentage) / 2)))}"))
                        .Width(Size.Fraction(1))
                   | (Layout.Horizontal().Align(Align.Right)
                      | Text.Small($"{percentage:F1}%")).Width(60);
        });

        return Layout.Vertical().Gap(2) | rows;
    }
}
