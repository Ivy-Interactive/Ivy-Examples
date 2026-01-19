namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;
using Ivy.Helpers;

public class SummarySlide : ViewBase
{
    private readonly GitHubStats _stats;

    public SummarySlide(GitHubStats stats)
    {
        _stats = stats;
    }

    public override object? Build()
    {
        var userName = _stats.UserInfo.FullName ?? _stats.UserInfo.Id;
        var userStatus = DetermineUserStatus();
        var topLanguage = _stats.LanguageBreakdown
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();
        
        // Animated values
        var animatedCommits = this.UseState(0);
        var animatedPRs = this.UseState(0);
        var animatedStreak = this.UseState(0);
        var animatedDays = this.UseState(0);
        var refresh = this.UseRefreshToken();
        
        this.UseEffect(() =>
        {
            var scheduler = new JobScheduler(maxParallelJobs: 2);
            var steps = 50;
            var delayMs = 20;
            
            scheduler.CreateJob("Animate Stats")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentProgress = i / (double)steps;
                        animatedCommits.Set((int)(_stats.TotalCommits * currentProgress));
                        animatedPRs.Set((int)(_stats.PullRequestsCreated * currentProgress));
                        animatedStreak.Set((int)(_stats.LongestStreak * currentProgress));
                        animatedDays.Set((int)(_stats.TotalContributionDays * currentProgress));
                        refresh.Refresh();
                        progress.Report(currentProgress);
                        await Task.Delay(delayMs, token);
                    }
                    animatedCommits.Set(_stats.TotalCommits);
                    animatedPRs.Set(_stats.PullRequestsCreated);
                    animatedStreak.Set(_stats.LongestStreak);
                    animatedDays.Set(_stats.TotalContributionDays);
                    refresh.Refresh();
                })
                .Build();
            
            _ = Task.Run(async () => await scheduler.RunAsync());
        });
        
        return Layout.Vertical().Gap(4).Align(Align.Center)
                | Text.H1("My2025").Bold().WithConfetti(AnimationTrigger.Auto)
                | (Layout.Horizontal().Gap(4).Align(Align.Stretch).Width(Size.Fraction(0.8f)).Height(Size.Full())
                    | BuildStatsCard(animatedCommits.Value, animatedPRs.Value, animatedDays.Value)
                    | (Layout.Vertical().Gap(3).Height(Size.Full())
                        | BuildStatusCard(userStatus)
                        | BuildTopLanguageCard(topLanguage)));
    }

    private (string Title, string MainText, string SubText, string Narrative) DetermineUserStatus()
    {
        var commits = _stats.TotalCommits;
        var prs = _stats.PullRequestsCreated;
        var prMerged = _stats.PullRequestsMerged;
        var streak = _stats.LongestStreak;
        var activeDays = _stats.TotalContributionDays;
        var languages = _stats.LanguageBreakdown.Count;
        
        // Code Master
        if (commits >= 300 && activeDays >= 100 && prs >= 50)
        {
            return ("Code Master", 
                "You showed up, you shipped, you collaborated.",
                "Master-level developer with exceptional dedication",
                "In 2025 you balanced high activity with consistency, building mastery through daily practice.");
        }

        // Productivity Champion
        if (commits >= 500)
        {
            return ("Productivity Champion",
                "You turned ideas into code at an incredible pace.",
                "Your keyboard was on fire this year",
                "You shipped an incredible amount of code, turning ideas into reality faster than most.");
        }

        // Collaboration Hero
        if (prs >= 80 && prMerged >= 60)
        {
            return ("Collaboration Hero",
                "You worked with others, reviewed code, and shipped together.",
                "Code review was your superpower",
                "You worked with others, reviewed code, and shipped together. That's how great products are built.");
        }

        // Streak Legend
        if (streak >= 60)
        {
            return ("Consistency Beast",
                "Day after day, you showed up and shipped.",
                $"A {streak}-day streak isn't luck — it's dedication",
                "You showed up day after day. That's how mastery is built.");
        }

        // Language Explorer
        if (languages >= 7)
        {
            return ("Polyglot",
                "You explored the ecosystem without limits.",
                $"{languages} languages, countless possibilities",
                "You didn't pick favorites — you explored the entire ecosystem.");
        }

        // Consistent Contributor
        if (commits >= 150 && activeDays >= 50)
        {
            return ("Consistent",
                "You built momentum and kept it going.",
                "Steady progress wins the race",
                "You built momentum and kept it going throughout the year.");
        }

        // Focused Builder
        if (commits >= 100)
        {
            return ("Focused",
                "You picked your battles and won them.",
                "Quality over quantity",
                "You stayed focused and turned ideas into working code.");
        }

        // Active Learner
        if (commits >= 30 && activeDays >= 15)
        {
            return ("Growing",
                "You're building your foundation.",
                "Every commit counts",
                "You're building your foundation, one commit at a time.");
        }

        // Getting Started
        return ("Starting",
            "Your coding journey is taking shape.",
            "The best is yet to come",
            "You kept building and improving throughout the year.");
    }

    private object BuildStatusCard((string Title, string MainText, string SubText, string Narrative) userStatus)
    {
        return new Card(Layout.Horizontal().Gap(3).Height(Size.Full())
            | (Layout.Vertical().Gap(3).Align(Align.Center).Width(Size.Fit()).Padding(3)
                | Icons.Trophy.ToIcon().Height(40).Width(40))
            | (Layout.Vertical().Gap(3).Align(Align.Center)
                | Text.Small("Your Developer Status — 2025").Muted()
                | Text.H1(userStatus.Title.ToUpper()).Bold()
                | Text.Block(userStatus.MainText)
                | Text.Small(userStatus.SubText).Muted()))
            .Width(Size.Full());
    }

    private object BuildStatsCard(int animatedCommits, int animatedPRs, int animatedDays)
    {
        return new Card(Layout.Vertical()
            | (Layout.Vertical().Align(Align.Center)
                | Text.H2($"{animatedDays.ToString()} days").Bold()
                | Text.Small("You showed up again and again").Muted())
            | (Layout.Vertical().Gap(2).Align(Align.Center)
                | Text.H2($"{animatedCommits.ToString()} commits").Bold()
                | Text.Small("Progress in small steps").Muted())
            | (Layout.Vertical().Gap(2).Align(Align.Center)
                | Text.H2($"{animatedPRs.ToString()} PRs").Bold()
                | Text.Small("You didn't just code — you shipped").Muted())).Width(Size.Fraction(0.5f));
    }

    private object BuildTopLanguageCard(KeyValuePair<string, long> topLanguage)
    {
        var totalBytes = _stats.LanguageBreakdown.Values.Sum();
        var languagePercentage = totalBytes > 0
            ? Math.Round((topLanguage.Value / (double)totalBytes) * 100, 0)
            : 0;
        
        var languageName = topLanguage.Key ?? "N/A";
        var mainText = languagePercentage > 0
            ? "THIS WAS YOUR LANGUAGE"
            : "Your main programming language";
        var subText = languagePercentage > 0
            ? $"{languagePercentage}% of your code was written in {languageName}"
            : $"{languageName} — your comfort zone and powerful tool";
        var motivationalText = languagePercentage >= 70
            ? "This is your superpower! You created most of your code with it."
            : languagePercentage >= 50
                ? "More than half of your code was written in this language. You know what you're doing!"
                : "This language helped you realize most of your ideas this year. Keep it up!";

        return new Card(Layout.Vertical().Gap(4).Align(Align.Center).Height(Size.Full())
            | Text.H1($"{languageName.ToUpper()} - {mainText} ").Bold()
            | Text.Block(subText).Muted()
            | Text.Block(motivationalText).Muted())
            .Width(Size.Full());
    }
}
