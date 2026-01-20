namespace GitHubWrapped.Apps;

using GitHubWrapped.Models;
using GitHubWrapped.Services;
using GitHubWrapped.Apps.Views;

[App(icon: Icons.Github, title: "GitHub Wrapped 2025")]
public class GitHubWrappedApp : ViewBase
{
    public override object? Build()
    {
        var auth = this.UseService<IAuthService>();
        var statsService = this.UseService<GitHubStatsService>();
        
        var stats = this.UseState<GitHubStats?>();
        var loading = this.UseState<bool>(true);
        var error = this.UseState<string?>();
        var selectedIndex = this.UseState(0);

        this.UseEffect(async () =>
        {
            try
            {
                loading.Set(true);
                var userInfo = await auth.GetUserInfoAsync();
                
                if (userInfo != null)
                {
                    var fetchedStats = await statsService.FetchStatsAsync(auth);
                    stats.Set(fetchedStats);
                }
            }
            catch (Exception ex)
            {
                error.Set($"Failed to load GitHub stats: {ex.Message}");
            }
            finally
            {
                loading.Set(false);
            }
        });

        // Loading state
        if (loading.Value)
        {
            return Layout.Vertical().Height(Size.Full()).Align(Align.TopCenter)

                    | (Layout.Vertical().Width(Size.Fraction(0.7f)).Height(Size.Full())
                        | new Skeleton().Height(Size.Units(30)).Width(Size.Full())
                    | (Layout.Vertical().Height(Size.Full()).Align(Align.Center)
                        | new Skeleton().Height(Size.Units(150)).Width(Size.Fraction(0.8f)))
                    | (Layout.Vertical().Align(Align.BottomCenter)
                        | (Layout.Horizontal().Gap(3)
                            | (Layout.Vertical().Align(Align.Left)
                                | new Skeleton().Height(Size.Units(15)).Width(Size.Units(50)))
                            | (Layout.Vertical().Align(Align.Right)
                                | new Skeleton().Height(Size.Units(15)).Width(Size.Units(70))))));
        }

        // Error state
        if (error.Value != null)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(3)
                       | Text.H2("Error")
                       | Text.Block(error.Value).Muted())
                     .Width(Size.Fraction(0.5f));
        }

        // Not authenticated
        if (stats.Value == null)
        {
            return Layout.Center()
                   | new Card(Layout.Vertical().Gap(4).Align(Align.Center)
                       | Icons.Github.ToIcon()
                       | Text.H2("Welcome to GitHub Wrapped 2025")
                       | Text.Block("Please login via the navigation bar to see your GitHub activity from 2025.")
                       | Text.Block("Click the login button in the top right corner to authenticate with GitHub.").Muted())
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
                    | (Layout.Vertical().Width(Size.Fraction(0.7f)).Height(Size.Full())
                        | new Stepper(OnSelect, selectedIndex.Value, stepperItems)
                            .AllowSelectForward()
                    | (Layout.Vertical().Height(Size.Full()).Align(Align.Center)
                        | BuildCurrentSlide(selectedIndex.Value, stats.Value))
                    | (Layout.Vertical().Align(Align.BottomCenter)
                        | (Layout.Horizontal().Gap(3)
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
                                | new Button(selectedIndex.Value == 0 ? "Start the recap" : "Show me more")
                                    .Icon(Icons.ChevronRight, Align.Right)
                                    .HandleClick(() =>
                                    {
                                        selectedIndex.Set(Math.Min(stepperItems.Length - 1, selectedIndex.Value + 1));
                                    })
                                    .Disabled(selectedIndex.Value == stepperItems.Length - 1)))));

        ValueTask OnSelect(Event<Stepper, int> e)
        {
            selectedIndex.Set(e.Value);
            return ValueTask.CompletedTask;
        }
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
}

