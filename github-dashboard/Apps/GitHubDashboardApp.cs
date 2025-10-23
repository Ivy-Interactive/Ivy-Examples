using GitHubDashboard.Services;
using GitHubDashboard.Models;

namespace GitHubDashboard.Apps;

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
        var selectedTab = UseState("overview");
        var refreshTrigger = UseState(0);

        // Trigger data refresh
        UseEffect(() =>
        {
            refreshTrigger.Set(refreshTrigger.Value + 1);
        }, []);

        var tabs = new[]
        {
            ("overview", "Repository Overview", Icons.Home),
            ("activity", "Activity Trends", Icons.Activity),
            ("contributors", "Contributors", Icons.Users),
            ("languages", "Language Analysis", Icons.Code)
        };

        return Layout.Vertical().Gap(4)
            | Header()
            | RefreshControls(refreshTrigger)
            | TabsLayout(tabs, selectedTab)
            | TabContent(selectedTab.Value, refreshTrigger.Value);
    }

    private object Header()
    {
        return Layout.Horizontal().JustifyBetween().AlignCenter()
            | Text.H1("GitHub Repository Analytics")
            | Layout.Horizontal().Gap(2)
                | Text.Muted($"Repository: {_owner}/{_repo}")
                | Badge.Success("Live Data");
    }

    private object RefreshControls(object refreshTrigger)
    {
        return Layout.Horizontal().Gap(2).AlignCenter()
            | Button("Refresh Data", () => refreshTrigger.Set(refreshTrigger.Value + 1))
                .Variant(ButtonVariants.Outline)
            | Text.Small($"Last refresh: {DateTime.Now:HH:mm:ss}");
    }

    private object TabsLayout((string key, string label, string icon)[] tabs, object selectedTab)
    {
        return Layout.Horizontal().Gap(1)
            | tabs.Select(tab => 
                Button(tab.label, () => selectedTab.Set(tab.key))
                    .Variant(selectedTab.Value == tab.key ? ButtonVariants.Default : ButtonVariants.Ghost)
                    .Width(25)
            ).ToArray();
    }

    private object TabContent(string selectedTab, int refreshTrigger)
    {
        return selectedTab switch
        {
            "overview" => new RepositoryOverviewView(_gitHubService, _owner, _repo, refreshTrigger),
            "activity" => new ActivityTrendsView(_gitHubService, _owner, _repo, refreshTrigger),
            "contributors" => new ContributorsView(_gitHubService, _owner, _repo, refreshTrigger),
            "languages" => new LanguageAnalysisView(_gitHubService, _owner, _repo, refreshTrigger),
            _ => Text.Error("Unknown tab")
        };
    }
}
