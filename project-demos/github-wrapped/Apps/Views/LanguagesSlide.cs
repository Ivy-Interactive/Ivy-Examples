namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;
using Ivy.Charts;
using Ivy.Helpers;

public class LanguagesSlide : ViewBase
{
    private readonly GitHubStats _stats;
    private readonly int _targetTotalCommits;
    private readonly KeyValuePair<string, long> _topLanguage;
    private readonly double _targetTopLanguagePercentage;

    public LanguagesSlide(GitHubStats stats)
    {
        _stats = stats;
        // LanguageBreakdown now contains bytes, not commits
        var totalBytes = _stats.LanguageBreakdown.Values.Sum();
        _targetTotalCommits = (int)totalBytes; // Use total bytes for percentage calculation (cast to int for compatibility)
        _topLanguage = _stats.LanguageBreakdown.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        _targetTopLanguagePercentage = _targetTotalCommits > 0 
            ? Math.Round((_topLanguage.Value / (double)totalBytes) * 100, 1)
            : 0;
    }

    public override object? Build()
    {
        var maxBytes = _stats.LanguageBreakdown.Values.DefaultIfEmpty(0L).Max();
        var languagesCount = _stats.LanguageBreakdown.Count;

        // Animated values
        var animatedTotalCommits = this.UseState(0);
        var animatedTopLanguagePercentage = this.UseState(0.0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        // Animate numbers on first render
        this.UseEffect(() =>
        {
            if (hasAnimated.Value) return;

            var scheduler = new JobScheduler(maxParallelJobs: 2);
            var steps = 50;
            var delayMs = 30;

            // Animate Total Commits
            scheduler.CreateJob("Animate Total Commits")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetTotalCommits);
                        animatedTotalCommits.Set(Math.Min(currentValue, _targetTotalCommits));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedTotalCommits.Set(_targetTotalCommits);
                    refresh.Refresh();
                })
                .Build();

            // Animate Top Language Percentage
            scheduler.CreateJob("Animate Top Language Percentage")
                .WithAction(async (_, _, progress, token) =>
                {
                    await Task.Delay(300, token); // Stagger start
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = Math.Round((i / (double)steps) * _targetTopLanguagePercentage, 1);
                        animatedTopLanguagePercentage.Set(Math.Min(currentValue, _targetTopLanguagePercentage));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedTopLanguagePercentage.Set(_targetTopLanguagePercentage);
                    hasAnimated.Set(true);
                    refresh.Refresh();
                })
                .Build();

            _ = Task.Run(async () => await scheduler.RunAsync());
        });

        // Generate dynamic headline based on percentage (avoid repeating language name)
        var languageName = _topLanguage.Key ?? "N/A";
        var headline = _targetTopLanguagePercentage >= 80
            ? "led your coding in 2025"
            : _targetTopLanguagePercentage >= 50
                ? "was at the heart of your projects"
                : "powered most of your work this year";
        
        // Generate dynamic subheadline (avoid repeating language name)
        var subheadline = _targetTopLanguagePercentage >= 66.7
            ? $"Over two thirds of your code came from one language"
            : $"{animatedTopLanguagePercentage.Value}% of your code was written in it";

        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(4).Align(Align.Center)
                  | Text.H2($"{_topLanguage.Key} Language" ?? "N/A").Bold().Italic()
                  | Text.Block(headline).Muted()
                  | Text.Block(subheadline).Muted())
                 .Width(Size.Fraction(0.6f))
               | (Layout.Vertical().Gap(4)
                   | new Spacer().Height(10)
                   | BuildLanguageChart(maxBytes)
                   | new Spacer().Height(10)
                   | BuildInsights(_topLanguage, languagesCount, _targetTotalCommits))
                 .Width(Size.Fraction(0.8f));
    }

    private object BuildLanguageChart(long maxBytes)
    {
        if (_stats.LanguageBreakdown.Count == 0)
        {
            return Layout.Horizontal().Align(Align.Center)
                   | Text.Block("No language data available").Muted();
        }

        // Get top 7 languages (already sorted and limited in service, but ensure we have 7)
        var sortedLanguages = _stats.LanguageBreakdown
            .OrderByDescending(kvp => kvp.Value)
            .Take(7)
            .ToList();

        // Calculate total bytes for percentage calculation
        var totalBytes = _stats.LanguageBreakdown.Values.Sum();

        // Prepare data for bar chart with percentages - each language as separate column
        var chartData = sortedLanguages
            .Select(kvp => new 
            { 
                Language = kvp.Key, 
                Percentage = totalBytes > 0 ? Math.Round((kvp.Value / (double)totalBytes) * 100, 1) : 0.0
            })
            .ToArray();


        // Create bar chart with vertical layout and different color for each language
        var barChart = new BarChart(chartData, 
                new Bar("Percentage")
                    .LegendType(LegendTypes.Square))
            .Layout(Layouts.Horizontal) // Vertical bars (standing)
            .XAxis(new XAxis("Language").TickLine(false).AxisLine(false))
            .YAxis()
            .Tooltip()
            .Legend()
            .Toolbox();

        return barChart;
    }

    private object BuildInsights(KeyValuePair<string, long> topLanguage, int languagesCount, int totalCommits)
    {
        var mainInsight = "";
        var subInsight = "";

        if (topLanguage.Key == null || languagesCount == 0)
        {
            mainInsight = "Your coding journey is just getting started.";
            subInsight = "Every project, experiment, and line of code adds up — keep building!";
        }
        else if (_targetTopLanguagePercentage >= 80)
        {
            mainInsight = $"{topLanguage.Key} was your clear superpower in 2025.";
            subInsight = $"You wrote {_targetTopLanguagePercentage}% of your code in it — that's some serious dedication!";
        }
        else if (_targetTopLanguagePercentage >= 50)
        {
            mainInsight = $"{topLanguage.Key} shaped most of your coding this year.";
            subInsight = $"You explored {languagesCount} languages while keeping a strong core — best of both worlds!";
        }
        else if (languagesCount >= 5)
        {
            mainInsight = "You coded across a wide range of languages.";
            subInsight = $"{languagesCount} languages, {totalCommits} commits — you're a true polyglot programmer!";
        }
        else if (languagesCount >= 3)
        {
            mainInsight = "You balanced focus with experimentation.";
            subInsight = $"{topLanguage.Key} was your anchor, but curiosity drove you further — that's how you grow!";
        }
        else
        {
            mainInsight = $"{topLanguage.Key} was your main tool this year.";
            subInsight = "You stayed focused and turned ideas into working code — consistency wins!";
        }

        return Layout.Vertical().Gap(2).Align(Align.Center)
            | (Layout.Horizontal().Align(Align.Center)
                | Icons.Code.ToIcon()
                | Text.Block(mainInsight).Bold())
            | Text.Block(subInsight).Muted();
    }
}
