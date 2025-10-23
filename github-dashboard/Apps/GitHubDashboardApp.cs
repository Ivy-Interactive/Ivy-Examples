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
        
        var tabs = new[]
        {
            ("Overview", "Repository overview and metrics"),
            ("Activity", "Commit activity and trends"),
            ("Contributors", "Contributor statistics"),
            ("Languages", "Language analysis")
        };

        var tabMenuItems = tabs
            .Select((tab, index) => new MenuItem(tab.Item1, () => selectedTab.Set(index)))
            .ToArray();

        return Layout.Vertical().Gap(4)
            | Header()
            | TabContent(selectedTab.Value, tabMenuItems);
    }

    private object Header()
    {
        return Layout.Horizontal().JustifyBetween().AlignCenter()
            | Text.H1("GitHub Repository Analytics")
            | Layout.Horizontal().Gap(2)
                | Text.Muted($"Repository: {_owner}/{_repo}")
                | Badge.Success("Live Data");
    }

    private object TabContent(int selectedTab, MenuItem[] tabMenuItems)
    {
        return Layout.Vertical().Gap(4)
            | Layout.Horizontal().Gap(1)
                | tabMenuItems.Select((item, index) => 
                    Button(item.Text, item.Action)
                        .Variant(selectedTab == index ? ButtonVariants.Default : ButtonVariants.Ghost)
                        .Width(25)
                ).ToArray()
            | (selectedTab switch
            {
                0 => new RepositoryOverviewView(_gitHubService, _owner, _repo),
                1 => new ActivityTrendsView(_gitHubService, _owner, _repo),
                2 => new ContributorsView(_gitHubService, _owner, _repo),
                3 => new LanguageAnalysisView(_gitHubService, _owner, _repo),
                _ => Text.Error("Unknown tab")
            });
    }
}