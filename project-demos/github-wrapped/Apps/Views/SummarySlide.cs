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
        
        return Layout.Vertical().Gap(4).Align(Align.Center).Width(Size.Fraction(0.8f))
               | Text.H1("My2025").Bold()
               | BuildStatusCard(userStatus)
               | BuildCollageGrid(userStatus, topLanguage, animatedCommits.Value, animatedPRs.Value, animatedStreak.Value, animatedDays.Value);
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
        return new Card(Layout.Horizontal().Gap(3).Align(Align.Center)
            | (Layout.Vertical().Gap(3).Align(Align.Center)
                | Icons.Trophy.ToIcon().Height(50).Width(50))
            | (Layout.Vertical().Gap(3).Align(Align.Center)
                | Text.Block("Your Developer Status — 2025").Muted()
                | Text.H1(userStatus.Title.ToUpper()).Bold()
                | Text.Block(userStatus.MainText)
                | Text.Block(userStatus.SubText).Muted()))
            .Width(Size.Full());
    }

    private object BuildCollageGrid((string Title, string MainText, string SubText, string Narrative) userStatus, 
                                     KeyValuePair<string, long> topLanguage,
                                     int animatedCommits,
                                     int animatedPRs,
                                     int animatedStreak,
                                     int animatedDays)
    {
        var totalBytes = _stats.LanguageBreakdown.Values.Sum();
        var languagePercentage = totalBytes > 0 
            ? Math.Round((topLanguage.Value / (double)totalBytes) * 100, 0) 
            : 0;
        
        return Layout.Vertical().Gap(3)
               // First row: Top Language + Streak (2 cards)
               | (Layout.Grid().Columns(2).Gap(3)
                  | new Card(Layout.Vertical()
                      | Text.H1($"{topLanguage.Key ?? "N/A"}").Bold()
                      | Text.Block("Your comfort zone & power tool").Muted())
                      .Title("Top Language").Icon(Icons.Code)
                  | new Card(Layout.Vertical()
                      | Text.H1($"{animatedStreak} days").Bold()
                      | Text.Block("No excuses. Just code.").Muted())
                      .Title("Longest Streak").Icon(Icons.Zap))
               // Second row: 3 cards (Commits, PRs, Active Days)
               | (Layout.Grid().Columns(3).Gap(3)
                  | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                      | Text.H2($"{animatedCommits.ToString()} commits").Bold()
                      | Text.Block("Progress in small steps").Muted())
                      .Title("Commits").Icon(Icons.GitCommitVertical)
                  | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                      | Text.H2($"{animatedPRs.ToString()} PRs").Bold()
                      | Text.Block("You didn't just code — you shipped").Muted())
                      .Title("Pull Requests").Icon(Icons.GitPullRequestCreate)
                  | new Card(Layout.Vertical().Gap(2).Align(Align.Center)
                      | Text.H2($"{animatedDays.ToString()} days").Bold()
                      | Text.Block("You showed up again and again").Muted())
                      .Title("Active Days").Icon(Icons.Activity));
    }
}
