using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

[App(icon: Icons.Code, title: "GitHub Dashboard")]
public class GitHubDashboardApp : ViewBase
{
    private readonly IGitHubApiService _gitHubService;
    private readonly string _owner = "Ivy-Interactive";
    private readonly string _repo = "Ivy-Framework";

    public GitHubDashboardApp(IGitHubApiService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    public override object? Build()
    {
        var selectedTab = this.UseState(0);
        var refreshTrigger = this.UseState(0);
        
        var tabs = new[]
        {
            ("Overview", "Repository overview and metrics"),
            ("Activity", "Commit activity and trends"),
            ("Contributors", "Contributor statistics"),
            ("Languages", "Language analysis")
        };

        return Layout.Vertical().Gap(4)
            | Header(refreshTrigger)
            | TabContent(selectedTab.Value, refreshTrigger.Value, tabs);
    }

    private object Header(IState<int> refreshTrigger)
    {
        return Layout.Horizontal().Gap(4)
            | Text.H1("GitHub Repository Analytics")
            | Layout.Horizontal().Gap(2)
                | Text.Muted($"Repository: {_owner}/{_repo}")
                | new Badge("Live Data", variant: BadgeVariant.Success)
            | new Button("Refresh Data", _ => refreshTrigger.Set(refreshTrigger.Value + 1))
                .Variant(ButtonVariant.Outline);
    }

    private object TabContent(int selectedTab, int refreshTrigger, (string, string)[] tabs)
    {
        return Layout.Vertical().Gap(4)
            | TabButtons(selectedTab, tabs)
            | (selectedTab switch
            {
                0 => new RepositoryOverviewView(_gitHubService, _owner, _repo, refreshTrigger),
                1 => new ActivityTrendsView(_gitHubService, _owner, _repo, refreshTrigger),
                2 => new ContributorsView(_gitHubService, _owner, _repo, refreshTrigger),
                3 => new LanguageAnalysisView(_gitHubService, _owner, _repo, refreshTrigger),
                _ => Text.P("Unknown tab")
            });
    }

    private object TabButtons(int selectedTab, (string, string)[] tabs)
    {
        return Layout.Horizontal().Gap(1)
            | tabs.Select((tab, index) => 
                new Button(tab.Item1, _ => { })
                    .Variant(selectedTab == index ? ButtonVariant.Primary : ButtonVariant.Ghost)
                    .Width(Size.Units(25))
            ).ToArray();
    }
}