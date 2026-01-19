namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;
using Ivy.Helpers;

public class PullRequestsSlide : ViewBase
{
    private readonly GitHubStats _stats;
    private readonly int _targetCreated;
    private readonly int _targetMerged;
    private readonly int _targetMergeRate;

    public PullRequestsSlide(GitHubStats stats)
    {
        _stats = stats;
        _targetCreated = stats.PullRequestsCreated;
        _targetMerged = stats.PullRequestsMerged;
        _targetMergeRate = _targetCreated > 0 
            ? (int)Math.Round(_targetMerged * 100.0 / _targetCreated)
            : 0;
    }

    public override object? Build()
    {
        var animatedCreated = this.UseState(0);
        var animatedMerged = this.UseState(0);
        var animatedRate = this.UseState(0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        // Animate numbers on first render
        this.UseEffect(() =>
        {
            if (hasAnimated.Value) return;

            var scheduler = new JobScheduler(maxParallelJobs: 3);
            var steps = 50;
            var delayMs = 30;

            // Animate Pull Requests Created
            scheduler.CreateJob("Animate Created")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetCreated);
                        animatedCreated.Set(Math.Min(currentValue, _targetCreated));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedCreated.Set(_targetCreated);
                    refresh.Refresh();
                })
                .Build();

            // Animate Pull Requests Merged
            scheduler.CreateJob("Animate Merged")
                .WithAction(async (_, _, progress, token) =>
                {
                    await Task.Delay(200, token); // Stagger start
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetMerged);
                        animatedMerged.Set(Math.Min(currentValue, _targetMerged));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedMerged.Set(_targetMerged);
                    refresh.Refresh();
                })
                .Build();

            // Animate Merge Rate
            scheduler.CreateJob("Animate Rate")
                .WithAction(async (_, _, progress, token) =>
                {
                    await Task.Delay(400, token); // Stagger start
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetMergeRate);
                        animatedRate.Set(Math.Min(currentValue, _targetMergeRate));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedRate.Set(_targetMergeRate);
                    hasAnimated.Set(true);
                    refresh.Refresh();
                })
                .Build();

            _ = Task.Run(async () => await scheduler.RunAsync());
        });

        var insight = BuildInsight(_targetMergeRate, _targetMerged, _targetCreated);

        return Layout.Vertical().Gap(4).Align(Align.Center)
               | Text.H2($"{animatedCreated.Value} Pull Requests").Bold().Italic()
               | Text.Block("ideas turned into pull requests").Muted()
               | new Spacer().Height(10)
               | (Layout.Grid().Gap(3).Columns(2).Width(Size.Fraction(0.8f))
                   | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                       | Text.H2(animatedMerged.Value.ToString()).Bold().Italic()
                       | Text.Small("Pull Requests successfully merged").Muted())
                       .Title("Merged").Icon(Icons.GitPullRequestArrow)
                   | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                       | Text.H2($"{animatedRate.Value}%").Bold().Italic()
                       | Text.Small("Merge success rate").Muted())
                       .Title("Success Rate").Icon(Icons.TrendingUp))
               | new Spacer().Height(10)
               | insight;
    }

    private object BuildInsight(int mergeRate, int merged, int created)
    {
        var mainInsight = "";
        var subInsight = "";

        if (created == 0)
        {
            mainInsight = "Your collaboration journey in 2025.";
            subInsight = "Every contribution matters.";
        }
        else if (mergeRate >= 90)
        {
            mainInsight = "Your pull requests were merged more often than average.";
            subInsight = "Maintainers trusted your changes — that's impressive!";
        }
        else if (mergeRate >= 75)
        {
            mainInsight = "Most of your pull requests made it into the final codebase.";
            subInsight = "You were a reliable contributor across projects.";
        }
        else if (mergeRate >= 50)
        {
            mainInsight = $"You opened {created} pull requests this year.";
            subInsight = $"{merged} of them were successfully merged.";
        }
        else
        {
            mainInsight = "You contributed through pull requests.";
            subInsight = "Keep collaborating and improving codebases.";
        }

        return Layout.Vertical().Gap(2).Align(Align.Center)
            | (Layout.Horizontal().Align(Align.Center)
                | Icons.GitPullRequestCreate.ToIcon()
                | Text.Block(mainInsight).Bold())
            | Text.Small(subInsight).Muted();
    }
}
