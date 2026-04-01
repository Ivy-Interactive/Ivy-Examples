namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class CommitsSlide : ViewBase
{
    private readonly GitHubStats _stats;
    private readonly int _targetCommits;

    public CommitsSlide(GitHubStats stats)
    {
        _stats = stats;
        _targetCommits = stats.TotalCommits;
    }

    public override object? Build()
    {
        var animatedCommits = this.UseState(0);
        var animatedCommitsPerWeek = this.UseState(0.0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        this.UseEffect(() =>
        {
            if (hasAnimated.Value) return;

            var targetCommitsPerWeek = _stats.TotalContributionDays > 0
                ? Math.Round(_targetCommits / (double)(_stats.TotalContributionDays / 7.0), 1)
                : 0.0;

            var scheduler = new JobScheduler(maxParallelJobs: 2);
            var steps = 50;
            var delayMs = 30;

            // Animate Total Commits
            scheduler.CreateJob("Animate Commits")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetCommits);
                        animatedCommits.Set(Math.Min(currentValue, _targetCommits));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedCommits.Set(_targetCommits);
                    refresh.Refresh();
                })
                .Build();

            // Animate Commits Per Week
            scheduler.CreateJob("Animate Commits Per Week")
                .WithAction(async (_, _, progress, token) =>
                {
                    await Task.Delay(300, token); // Stagger start
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = Math.Round((i / (double)steps) * targetCommitsPerWeek, 1);
                        animatedCommitsPerWeek.Set(Math.Min(currentValue, targetCommitsPerWeek));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedCommitsPerWeek.Set(targetCommitsPerWeek);
                    hasAnimated.Set(true);
                    refresh.Refresh();
                })
                .Build();

            _ = Task.Run(async () => await scheduler.RunAsync());
        });

        var maxCommits = _stats.CommitsByMonth.Values.DefaultIfEmpty(0).Max();
        var peakMonth = _stats.CommitsByMonth
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();
        var activeMonths = _stats.CommitsByMonth.Values.Count(v => v > 0);
        var totalMonths = 12;
        var activeMonthsPercentage = Math.Round((activeMonths / (double)totalMonths) * 100);
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var peakMonthIndex = peakMonth.Value > 0 ? Array.IndexOf(months, peakMonth.Key) : -1;
        var monthsAfterPeak = peakMonthIndex >= 0
            ? months.Skip(peakMonthIndex + 1).Count(m => _stats.CommitsByMonth.GetValueOrDefault(m, 0) > 0)
            : 0;

        return Layout.Vertical().Gap(6).AlignContent(Align.Center)
               | (Layout.Vertical().Gap(4).AlignContent(Align.Center)
                  | Text.H2($"{animatedCommits.Value} Commits").Bold().Italic()
                  | Text.Block("shipped in 2025").Muted()
                  | Text.Block($"That's ~{animatedCommitsPerWeek.Value} commits per active week.").Muted())
                 .Width(Size.Fraction(0.6f))
               | (Layout.Vertical().Gap(4)
                   | Layout.Vertical().Height(Size.Units(10))
                   | BuildMonthlyChart(maxCommits)
                   | Layout.Vertical().Height(Size.Units(10))
                   | BuildInsights(activeMonths, activeMonthsPercentage, peakMonth.Key, peakMonth.Value, monthsAfterPeak))
                 .Width(Size.Fraction(0.8f));
    }

    private object BuildMonthlyChart(int maxCommits)
    {
        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var peakMonth = _stats.CommitsByMonth
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        // Filter to show only months with activity, or show all but make empty ones more compact
        var rows = months.Select((month, index) =>
        {
            var count = _stats.CommitsByMonth.GetValueOrDefault(month, 0);
            var isPeak = month == peakMonth.Key && peakMonth.Value > 0;
            return new MonthlyChartRowView(month, count, maxCommits, isPeak, index);
        });

        return Layout.Vertical().Gap(2) | rows;
    }

    private object BuildInsights(int activeMonths, double activeMonthsPercentage, string peakMonth, int peakMonthCommits, int monthsAfterPeak)
    {
        var mainInsight = "";
        var subInsight = "";

        if (activeMonths == 0)
        {
            mainInsight = "Your coding journey in 2025.";
            subInsight = "Every commit counts — keep going!";
        }
        else if (activeMonthsPercentage >= 80)
        {
            mainInsight = "You shipped consistently throughout the year.";
            subInsight = "That's the kind of dedication that builds something great.";
        }
        else if (activeMonthsPercentage >= 50)
        {
            mainInsight = $"You were active in {activeMonths} months.";
            subInsight = "Solid consistency — you're building something real.";
        }
        else if (peakMonthCommits > 0 && monthsAfterPeak >= 2)
        {
            // Started in peak month and continued for at least 2 more months
            var monthName = GetFullMonthName(peakMonth);
            mainInsight = $"{monthName} was your turning point — and you ran with it!";
            subInsight = "You kept the momentum going till the end of the year.";
        }
        else if (peakMonthCommits > 0 && monthsAfterPeak >= 1)
        {
            // Started in peak month and continued for at least 1 more month
            var monthName = GetFullMonthName(peakMonth);
            mainInsight = $"Once you started shipping in {monthName},";
            subInsight = "you never looked back.";
        }
        else if (peakMonthCommits > 0)
        {
            // Peak month exists but limited activity after
            var monthName = GetFullMonthName(peakMonth);
            mainInsight = $"{monthName} was your moment.";
            subInsight = "You made it count — that's what matters.";
        }
        else
        {
            mainInsight = $"You were active in {activeMonths} months.";
            subInsight = "Keep building momentum — you're on the right track.";
        }

        return Layout.Vertical().Gap(2).AlignContent(Align.Center)
            | (Layout.Horizontal().AlignContent(Align.Center)
                | Icons.Activity.ToIcon()
                | Text.Block(mainInsight).Bold())
            | Text.Block(subInsight).Muted();
    }

    private string GetFullMonthName(string monthAbbr)
    {
        return monthAbbr switch
        {
            "Jan" => "January",
            "Feb" => "February",
            "Mar" => "March",
            "Apr" => "April",
            "May" => "May",
            "Jun" => "June",
            "Jul" => "July",
            "Aug" => "August",
            "Sep" => "September",
            "Oct" => "October",
            "Nov" => "November",
            "Dec" => "December",
            _ => monthAbbr
        };
    }
}
