namespace GitHubWrapped.Apps;

using GitHubWrapped.Models;
using GitHubWrapped.Services;
using GitHubWrapped.Apps.Views;
using Ivy.Helpers;

[App(icon: Icons.Github, title: "GitHub Wrapped 2025")]
public class GitHubWrappedApp : ViewBase
{
    public override object? Build()
    {
        var auth = this.UseService<IAuthService>();
        var apiClient = this.UseService<GitHubApiClient>();
        
        var stats = this.UseState<GitHubStats?>();
        var selectedIndex = this.UseState(0);
        var refresh = this.UseRefreshToken();

        var scheduler = this.UseMemo(() => BuildScheduler(auth, apiClient, stats));

        this.UseEffect(() =>
        {
            var sub = scheduler.Subscribe(_ => refresh.Refresh());
            
            // Auto-start if not already finished or running
            if (stats.Value == null)
            {
                _ = scheduler.RunAsync();
            }
            
            return sub;
        });

        // 1. Loading / Executing state
        // When stats are null, the scheduler is either running or failed.
        // We show the scheduler view which provides rich status feedback.
        if (stats.Value == null)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(4).Align(Align.Center)
                       | Icons.Github.ToIcon().Height(Size.Units(40)).Width(Size.Units(40))
                       | Text.H2("Preparing your 2025 Wrap...").Bold()
                       | Text.Small("Gathering your GitHub activity data").Muted()
                       | scheduler.ToView())
                     .Width(Size.Fraction(0.5f));
        }

        // Main wrapped experience with stepper
        var stepperItems = new[]
        {
            new StepperItem("1", selectedIndex.Value > 0 ? Icons.Check : null, "Welcome", "Your 2025 journey"),
            new StepperItem("2", selectedIndex.Value > 1 ? Icons.Check : null, "Commits", "Your code contributions"),
            new StepperItem("3", selectedIndex.Value > 2 ? Icons.Check : null, "Pull Requests", "Collaboration stats"),
            new StepperItem("4", selectedIndex.Value > 3 ? Icons.Check : null, "Languages", "Tech stack"),
            new StepperItem("5", selectedIndex.Value > 4 ? Icons.Check : null, "Repositories", "Top projects"),
            new StepperItem("6", null, "Summary", "2025 highlights")
        };

        return Layout.Vertical().Height(Size.Full()).Align(Align.TopCenter)
                    | (Layout.Vertical().Height(Size.Fit()).Width(Size.Fraction(0.7f))
                        | new Stepper(OnSelect, selectedIndex.Value, stepperItems)
                            .AllowSelectForward())
                    | (Layout.Vertical().Height(Size.Full()).Width(Size.Fraction(0.7f))
                    | new FooterLayout(
                        footer: (Layout.Horizontal()
                            | (Layout.Vertical().Align(Align.Left)
                                | new Button("Previous")
                                    .Icon(Icons.ChevronLeft)
                                    .Variant(ButtonVariant.Outline)
                                    .Disabled(selectedIndex.Value == 0)
                                    .HandleClick(() =>
                                    {
                                        selectedIndex.Set(Math.Max(0, selectedIndex.Value - 1));
                                    }))
                            | (Layout.Vertical().Align(Align.Right)
                                | (selectedIndex.Value == stepperItems.Length - 1 
                                    ? BuildShareButton(stats.Value)
                                    : new Button(selectedIndex.Value == 0 ? "Start the recap" : "Show me more")
                                        .Icon(Icons.ChevronRight, Align.Right)
                                        .HandleClick(() =>
                                        {
                                            selectedIndex.Set(Math.Min(stepperItems.Length - 1, selectedIndex.Value + 1));
                                        })))),
                        content: (Layout.Vertical().Align(Align.Center)
                            | BuildCurrentSlide(selectedIndex.Value, stats.Value))));

        ValueTask OnSelect(Event<Stepper, int> e)
        {
            selectedIndex.Set(e.Value);
            return ValueTask.CompletedTask;
        }
    }

    private object BuildShareButton(GitHubStats stats)
    {
        var downloadUrl = this.UseDownload(() => SummarySlide.GenerateSummaryImage(stats), "image/png", "github-wrapped-2025.png");
        
        var shareButton = new Button("Share").Icon(Icons.Share2);
        if (!string.IsNullOrEmpty(downloadUrl.Value))
        {
            shareButton = shareButton.Url(downloadUrl.Value);
        }
        return shareButton;
    }
    
    private object BuildCurrentSlide(int index, GitHubStats stats)
    {
        return index switch
        {
            0 => new WelcomeSlide(stats),
            1 => new CommitsSlide(stats),
            2 => new PullRequestsSlide(stats),
            3 => new LanguagesSlide(stats),
            4 => new RepositoriesSlide(stats),
            5 => new SummarySlide(stats),
            _ => Text.Block("Unknown slide")
        };
    }

    private JobScheduler BuildScheduler(
        IAuthService auth,
        GitHubApiClient apiClient,
        IState<GitHubStats?> statsState)
    {
        var scheduler = new JobScheduler(maxParallelJobs: 3);
        
        // Shared state for the jobs
        var context = new ExtractionContext();
        var options = new GitHubStatsOptions();

        var authJob = scheduler.CreateJob("Authenticating")
            .WithAction(async (_, _, progress, token) =>
            {
                progress.Report(0.2);
                context.User = await auth.GetUserInfoAsync();
                if (context.User == null) throw new Exception("Not authenticated");
                
                var authToken = auth.GetAuthSession().AuthToken?.AccessToken;
                if (string.IsNullOrWhiteSpace(authToken)) throw new Exception("No access token");
                context.Token = authToken;

                progress.Report(0.6);
                context.Username = await apiClient.GetUsernameAsync(context.Token);
                if (string.IsNullOrWhiteSpace(context.Username)) throw new Exception("Could not find username");
                
                progress.Report(1.0);
            })
            .Build();

        var reposJob = scheduler.CreateJob("Fetching Repositories")
            .DependsOn(authJob)
            .WithAction(async (_, _, progress, token) =>
            {
                progress.Report(0.1);
                context.Repositories = await apiClient.GetRepositoriesAsync(context.Token!, options);
                progress.Report(1.0);
            })
            .Build();

        var commitsJob = scheduler.CreateJob("Fetching Commits")
            .DependsOn(authJob)
            .WithAction(async (_, _, progress, token) =>
            {
                progress.Report(0.1);
                context.Commits = await apiClient.GetCommitsAsync(context.Token!, context.Username!, options);
                progress.Report(1.0);
            })
            .Build();

        var prsJob = scheduler.CreateJob("Fetching Pull Requests")
            .DependsOn(authJob)
            .WithAction(async (_, _, progress, token) =>
            {
                progress.Report(0.1);
                context.PullRequests = await apiClient.GetPullRequestsAsync(context.Token!, context.Username!, options);
                progress.Report(1.0);
            })
            .Build();
            
        var streakJob = scheduler.CreateJob("Calculating Streak")
             .DependsOn(authJob)
             .WithAction(async (_, _, progress, token) =>
             {
                 progress.Report(0.1);
                 var (longest, current, total) = await apiClient.GetContributionStreakAsync(context.Token!, context.Username!, options);
                 context.LongestStreak = longest;
                 context.CurrentStreak = current;
                 context.TotalDays = total;
                 progress.Report(1.0);
             })
             .Build();

        scheduler.CreateJob("Analyzing Data")
            .DependsOn(reposJob, commitsJob, prsJob, streakJob)
            .WithAction(async (_, _, progress, token) =>
            {
                progress.Report(0.1);
                var calculator = new GitHubStatsCalculator(options);
                
                var commitsByMonth = calculator.CalculateCommitsByMonth(context.Commits!);
                var languageBreakdown = calculator.CalculateLanguageBreakdown(context.Repositories!);
                var topRepos = calculator.CalculateTopRepos(context.Repositories!, context.Commits!);
                var (prsCreated, prsMerged) = calculator.CalculatePullRequestStats(context.PullRequests!);
                var starsReceived = calculator.CalculateStarsReceived(context.Repositories!);

                progress.Report(0.8);

                var finalStats = new GitHubStats(
                    UserInfo: context.User!,
                    TotalCommits: context.Commits!.Count,
                    CommitsByMonth: commitsByMonth,
                    LanguageBreakdown: languageBreakdown,
                    TopRepos: topRepos,
                    PullRequestsCreated: prsCreated,
                    PullRequestsMerged: prsMerged,
                    LongestStreak: context.LongestStreak,
                    TotalContributionDays: context.TotalDays,
                    StarsGiven: 0,
                    StarsReceived: starsReceived
                );

                statsState.Set(finalStats);
                progress.Report(1.0);
            })
            .Build();
            
        return scheduler;
    }

    private class ExtractionContext
    {
        public UserInfo? User { get; set; }
        public string? Token { get; set; }
        public string? Username { get; set; }
        public List<GitHubRepository>? Repositories { get; set; }
        public List<GitHubCommit>? Commits { get; set; }
        public List<GitHubPullRequest>? PullRequests { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalDays { get; set; }
    }
}

