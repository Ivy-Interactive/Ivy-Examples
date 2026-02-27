namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Models;
using SliplaneManage.Services;

/// <summary>
/// Sliplane Overview App — a hub with three clickable cards (Servers, Projects, Services).
/// Clicking a card navigates to a tab with the full view for that resource.
/// </summary>
[App(icon: Icons.LayoutGrid, title: "Sliplane Overview", searchHints: ["hub", "overview", "cards", "servers", "projects", "services"])]
public class SliplaneOverviewApp : ViewBase
{
    // Tab indices
    private const int TabHub      = 0;
    private const int TabServers  = 1;
    private const int TabProjects = 2;
    private const int TabServices = 3;

    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var client   = this.UseService<SliplaneApiClient>();
        var auth     = this.UseService<IAuthService>();
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        var activeTab = this.UseState(TabHub);
        var projects  = this.UseState<List<SliplaneProject>>();
        var overview  = this.UseState<SliplaneOverview?>();

        // Pre-load projects so ServicesView can consume them
        this.UseEffect(async () =>
        {
            if (string.IsNullOrWhiteSpace(apiToken)) return;
            try
            {
                var list = await client.GetProjectsAsync(apiToken);
                projects.Set(list);
            }
            catch
            {
                projects.Set(new List<SliplaneProject>());
            }

            try
            {
                var data = await client.GetOverviewAsync(apiToken);
                overview.Set(data);
            }
            catch
            {
                overview.Set((SliplaneOverview?)null);
            }
        });

        // ── Not configured ─────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                   | new Card(
                       Layout.Vertical().Gap(4).Align(Align.Center)
                       | Icons.LayoutGrid.ToIcon().Size(40).Color(Colors.Orange)
                       | Text.H2("Sliplane Overview")
                       | Text.Block("No API token configured.").Muted()
                       | Text.Block("Sign in with a Sliplane account or add Sliplane:ApiToken to your configuration.")
                     ).Width(Size.Fraction(0.5f));
        }

        var ov           = overview.Value;
        var serversCount = ov?.Servers.Count ?? 0;
        var projectsCount = ov?.Projects.Count ?? 0;
        var servicesCount = ov?.ServicesByProject.Values.Sum(s => s.Count) ?? 0;

        // ── Tabs: Hub | Servers | Projects | Services ──────────────────────────
        return Layout.Tabs(
                new Tab("Overview",  BuildHubView(activeTab, serversCount, projectsCount, servicesCount)).Icon(Icons.LayoutGrid),
                new Tab("Servers",   new ServersView(apiToken)).Icon(Icons.Server),
                new Tab("Projects",  new ProjectsView(apiToken)).Icon(Icons.FolderOpen),
                new Tab("Services",  new ServicesView(apiToken, projects.Value ?? [])).Icon(Icons.Box)
              ).Variant(TabsVariant.Tabs);
    }

    // ── Hub: three clickable cards ─────────────────────────────────────────────

    private static object BuildHubView(
        IState<int> activeTab,
        int serversCount,
        int projectsCount,
        int servicesCount)
    {
        var serversCard = new Card(Text.H2($"{serversCount} Servers"))
            .Icon(Icons.Server)
            .HandleClick(_ => activeTab.Set(TabServers))
            .Width(Size.Units(110));

        var projectsCard = new Card(Text.H3(projectsCount.ToString()))
            .Title("Projects")
            .Description("Browse, create, rename, and delete your Sliplane projects.")
            .Icon(Icons.FolderOpen)
            .HandleClick(_ => activeTab.Set(TabProjects))
            .Width(Size.Units(110));

        var servicesCard = new Card(Text.H3(servicesCount.ToString()))
            .Title("Services")
            .Description("Deploy and manage runtime services; view logs, events & metrics.")
            .Icon(Icons.Box)
            .HandleClick(_ => activeTab.Set(TabServices))
            .Width(Size.Units(110));

        return Layout.Vertical()
            | Text.H2("Sliplane Overview")
            | Text.Muted("Select a resource below to explore your infrastructure.")
            | (Layout.Horizontal()
               | serversCard
               | projectsCard
               | servicesCard);
    }
}
