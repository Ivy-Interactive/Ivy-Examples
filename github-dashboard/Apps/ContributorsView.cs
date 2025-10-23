using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

public class ContributorsView : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner;
    private readonly string _repo;
    private readonly int _refreshTrigger;

    public ContributorsView(IGitHubApiService gitHubService, string owner, string repo, int refreshTrigger)
    {
        _gitHubService = gitHubService;
        _owner = owner;
        _repo = repo;
        _refreshTrigger = refreshTrigger;
    }

    public override object? Build()
    {
        var contributors = this.UseState<List<ContributorInfo>?>(() => null);
        var isLoading = this.UseState(true);

        this.UseEffect(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    isLoading.Set(true);
                    var response = await _gitHubService.GetContributorsAsync(_owner, _repo);
                    
                    if (response.Success)
                    {
                        contributors.Set(response.Data);
                    }
                }
                catch (Exception)
                {
                    // Handle error silently for now
                }
                finally
                {
                    isLoading.Set(false);
                }
            });
        });

        if (isLoading.Value)
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Contributors")
                | Text.Muted("Loading contributor data...");
        }

        if (contributors.Value == null || !contributors.Value.Any())
        {
            return Layout.Vertical().Gap(4)
                | Text.H2("Contributors")
                | Text.Muted("No contributor data available");
        }

        return Layout.Vertical().Gap(4)
            | Text.H2("Contributors")
            | TopContributorsCard(contributors.Value)
            | ContributorsListCard(contributors.Value);
    }

    private object TopContributorsCard(List<ContributorInfo> contributors)
    {
        var topContributors = contributors.Take(10).ToList();
        var totalContributions = contributors.Sum(c => c.Contributions);

        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3($"Top Contributors ({contributors.Count} total)")
                | Layout.Vertical().Gap(2)
                    | topContributors.Select((contributor, index) => 
                        Layout.Horizontal().Gap(3)
                            | Text.Small($"#{index + 1}")
                            | Text.Medium(contributor.Login)
                            | Text.Muted($"{contributor.Contributions} contributions")
                            | Text.Small($"({contributor.Contributions * 100.0 / totalContributions:F1}%)")
                    ).ToArray()
        ).Title("Top Contributors");
    }

    private object ContributorsListCard(List<ContributorInfo> contributors)
    {
        var contributorsByRange = new[]
        {
            ("1000+", contributors.Count(c => c.Contributions >= 1000)),
            ("500-999", contributors.Count(c => c.Contributions >= 500 && c.Contributions < 1000)),
            ("100-499", contributors.Count(c => c.Contributions >= 100 && c.Contributions < 500)),
            ("50-99", contributors.Count(c => c.Contributions >= 50 && c.Contributions < 100)),
            ("10-49", contributors.Count(c => c.Contributions >= 10 && c.Contributions < 50)),
            ("1-9", contributors.Count(c => c.Contributions >= 1 && c.Contributions < 10))
        };

        return new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Contribution Distribution")
                | Layout.Vertical().Gap(2)
                    | contributorsByRange.Select(range => 
                        Layout.Horizontal().Gap(3)
                            | Text.Small(range.Item1)
                            | Text.Medium($"{range.Item2} contributors")
                    ).ToArray()
        ).Title("Contribution Distribution");
    }
}