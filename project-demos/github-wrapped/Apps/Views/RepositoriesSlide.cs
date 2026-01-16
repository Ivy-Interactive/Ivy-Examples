namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;
using Ivy.Helpers;

public class RepositoriesSlide : ViewBase
{
    private readonly GitHubStats _stats;
    private readonly int _targetTotalRepos;
    private readonly int _targetTopRepoCommits;

    public RepositoriesSlide(GitHubStats stats)
    {
        _stats = stats;
        _targetTotalRepos = _stats.TopRepos.Count;
        _targetTopRepoCommits = _stats.TopRepos.FirstOrDefault()?.CommitCount ?? 0;
    }

    public override object? Build()
    {
        var topRepo = _stats.TopRepos.FirstOrDefault();
        var client = this.UseService<IClientProvider>();
        var maxCommits = _stats.TopRepos.DefaultIfEmpty().Max(r => r?.CommitCount ?? 0);

        // Animated values
        var animatedRepoCount = this.UseState(0);
        var animatedTopRepoCommits = this.UseState(0);
        var refresh = this.UseRefreshToken();
        var hasAnimated = this.UseState(false);

        // Animate numbers on first render
        this.UseEffect(() =>
        {
            if (hasAnimated.Value) return;

            var scheduler = new JobScheduler(maxParallelJobs: 2);
            var steps = 50;
            var delayMs = 30;

            // Animate Total Repositories
            scheduler.CreateJob("Animate Repositories")
                .WithAction(async (_, _, progress, token) =>
                {
                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        var currentValue = (int)Math.Round((i / (double)steps) * _targetTotalRepos);
                        animatedRepoCount.Set(Math.Min(currentValue, _targetTotalRepos));
                        refresh.Refresh();
                        progress.Report(i / (double)steps);
                        await Task.Delay(delayMs, token);
                    }
                    animatedRepoCount.Set(_targetTotalRepos);
                    refresh.Refresh();
                })
                .Build();

            // Animate Top Repo Commits
            if (topRepo != null)
            {
                scheduler.CreateJob("Animate Top Repo Commits")
                    .WithAction(async (_, _, progress, token) =>
                    {
                        await Task.Delay(300, token); // Stagger start
                        for (int i = 0; i <= steps; i++)
                        {
                            if (token.IsCancellationRequested) break;
                            var currentValue = (int)Math.Round((i / (double)steps) * _targetTopRepoCommits);
                            animatedTopRepoCommits.Set(Math.Min(currentValue, _targetTopRepoCommits));
                            refresh.Refresh();
                            progress.Report(i / (double)steps);
                            await Task.Delay(delayMs, token);
                        }
                        animatedTopRepoCommits.Set(_targetTopRepoCommits);
                        hasAnimated.Set(true);
                        refresh.Refresh();
                    })
                    .Build();
            }
            else
            {
                hasAnimated.Set(true); // No animation if no repos
            }

            _ = Task.Run(async () => await scheduler.RunAsync());
        });

        // Generate dynamic headline
        var repoName = topRepo?.Name ?? "N/A";
        var headline = _targetTotalRepos >= 10
            ? "was your coding playground"
            : _targetTotalRepos >= 5
                ? "was where you made your mark"
                : "was your main project";

        // Generate dynamic subheadline
        var subheadline = topRepo != null
            ? $"{animatedTopRepoCommits.Value} commits in your top repository"
            : "Start building your next project";

        return Layout.Vertical().Gap(6).Align(Align.Center)
               | (Layout.Vertical().Gap(4).Align(Align.Center)
                  | Text.H2($"{repoName} Repository").Bold().Italic()
                  | Text.Block(headline).Muted()
                  | Text.Small(subheadline).Muted())
                 .Width(Size.Fraction(0.6f))
               | (Layout.Vertical().Gap(4)
                   | new Spacer().Height(5)
                   | BuildRepoList(client, maxCommits)
                   | new Spacer().Height(5)
                   | BuildInsights(topRepo, _targetTotalRepos, _targetTopRepoCommits))
                 .Width(Size.Fraction(0.8f));
    }

    private object BuildRepoList(IClientProvider client, int maxCommits)
    {
        if (_stats.TopRepos.Count == 0)
        {
            return Layout.Horizontal().Align(Align.Center)
                   | Text.Block("No repository data available").Muted();
        }

        // Calculate total commits across all repos for percentage
        var totalCommits = _stats.TopRepos.Sum(r => r.CommitCount);

        var repoCards = _stats.TopRepos.Select((repo, index) =>
        {
            var percentage = totalCommits > 0 ? Math.Round((repo.CommitCount / (double)totalCommits) * 100, 1) : 0.0;
            var animatedPercentage = this.UseState(0.0);
            var refresh = this.UseRefreshToken();

            // Animate percentage
            this.UseEffect(() =>
            {
                if (repo.CommitCount == 0) return;

                var finalValue = percentage;
                var steps = 30;
                var delay = index * 50; // Stagger by index

                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);

                    for (int i = 0; i <= steps; i++)
                    {
                        var currentValue = Math.Round((i / (double)steps) * finalValue, 1);
                        animatedPercentage.Set(currentValue);
                        refresh.Refresh();
                        await Task.Delay(30);
                    }

                    animatedPercentage.Set(finalValue);
                    refresh.Refresh();
                });
            });

            return new Card(Layout.Vertical().Align(Align.Center)
                    | Text.H2($"{animatedPercentage.Value}%").Bold().Italic()
                    | Text.Block($"{repo.CommitCount} commits").Muted())
                .Title($"{repo.Name}")
                .Icon(Icons.Folder)
                .HandleClick(_ => client.OpenUrl(repo.HtmlUrl));
        });

        // Arrange cards: first row with 2 cards, second row with 3 cards
        var cardsList = repoCards.ToList();
        var rows = new List<object>();
        
        // First row: 2 cards
        if (cardsList.Count > 0)
        {
            var firstRow = cardsList.Take(2).ToList();
            if (firstRow.Count > 0)
            {
                rows.Add(Layout.Horizontal().Gap(3).Align(Align.Center) | firstRow);
            }
        }
        
        // Second row: 3 cards
        if (cardsList.Count > 2)
        {
            var secondRow = cardsList.Skip(2).Take(3).ToList();
            if (secondRow.Count > 0)
            {
                rows.Add(Layout.Horizontal().Gap(3).Align(Align.Center) | secondRow);
            }
        }
        
        // Remaining cards (if more than 5)
        if (cardsList.Count > 5)
        {
            var remainingRow = cardsList.Skip(5).ToList();
            if (remainingRow.Count > 0)
            {
                rows.Add(Layout.Horizontal().Gap(3).Align(Align.Center) | remainingRow);
            }
        }

        return Layout.Vertical().Gap(3).Align(Align.Center) | rows;
    }

    private object BuildInsights(RepoStats? topRepo, int repoCount, int topRepoCommits)
    {
        var mainInsight = "";
        var subInsight = "";

        if (topRepo == null || repoCount == 0)
        {
            mainInsight = "Your repository journey is just beginning.";
            subInsight = "Every project starts with a single commit — keep building!";
        }
        else if (repoCount == 1)
        {
            mainInsight = $"You focused all your energy on {topRepo.Name}.";
            subInsight = $"{topRepoCommits} commits — one project, maximum impact!";
        }
        else if (repoCount >= 5)
        {
            mainInsight = $"You worked across your top {repoCount} repositories.";
            subInsight = $"{topRepo.Name} was your main focus, but you didn't stop there — versatility wins!";
        }
        else if (topRepoCommits >= 100)
        {
            mainInsight = $"{topRepo.Name} was your coding home this year.";
            subInsight = $"{topRepoCommits} commits — that's some serious dedication to one project!";
        }
        else
        {
            mainInsight = $"{topRepo.Name} was where you made your biggest impact.";
            subInsight = $"You balanced your top {repoCount} repositories while keeping focus — that's how you ship!";
        }

        return Layout.Vertical().Gap(2).Align(Align.Center)
            | (Layout.Horizontal().Align(Align.Center)
                | Icons.Folder.ToIcon()
                | Text.Block(mainInsight).Bold())
            | Text.Small(subInsight).Muted();
    }
}
