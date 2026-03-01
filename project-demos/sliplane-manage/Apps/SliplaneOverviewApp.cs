namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Models;
using SliplaneManage.Services;

/// <summary>
/// Overview app — hub with summary cards (Servers, Projects, Services). Use the sidebar to open Servers, Projects, or Services.
/// </summary>
[App(icon: Icons.LayoutGrid, title: "Overview", searchHints: ["hub", "overview", "cards", "servers", "projects", "services"], isVisible: false)]
public class SliplaneOverviewApp : ViewBase
{
    public override object? Build()
    {
        var config  = this.UseService<IConfiguration>();
        var client  = this.UseService<SliplaneApiClient>();
        var auth    = this.UseService<IAuthService>();
        var session = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        var overview = this.UseState<SliplaneOverview?>();

        this.UseEffect(async () =>
        {
            if (string.IsNullOrWhiteSpace(apiToken)) return;
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

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                   | new Card(
                       Layout.Vertical().Align(Align.Center)
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

        var serversCard = new Card(Text.H2($"{serversCount} Servers"))
            .Icon(Icons.Server)
            .Height(Size.Full())
            .Width(Size.Units(110));

        var projectsCard = new Card(Text.H3(projectsCount.ToString()))
            .Title("Projects")
            .Description("Browse, create, rename, and delete your Sliplane projects.")
            .Icon(Icons.FolderOpen)
            .Width(Size.Units(110));

        var servicesCard = new Card(Text.H3(servicesCount.ToString()))
            .Title("Services")
            .Description("Deploy and manage runtime services; view logs, events & metrics.")
            .Icon(Icons.Box)
            .Width(Size.Units(110));

        return Layout.Vertical().Align(Align.TopCenter)
               | (Layout.Horizontal().Align(Align.Center)
                | serversCard
                | projectsCard
                | servicesCard);
    }
}

/// <summary>
/// Servers app — list servers with metrics, reboot, delete.
/// </summary>
[App(icon: Icons.Server, title: "Servers", searchHints: ["servers", "infrastructure"])]
public class SliplaneServersApp : ViewBase
{
    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var auth     = this.UseService<IAuthService>();
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
            return Layout.Center() | Text.Muted("No API token. Sign in or configure Sliplane:ApiToken.");

        return new ServersView(apiToken);
    }
}

/// <summary>
/// Projects app — list, create, rename, delete projects.
/// </summary>
[App(icon: Icons.FolderOpen, title: "Projects", searchHints: ["projects", "repos"])]
public class SliplaneProjectsApp : ViewBase
{
    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var auth     = this.UseService<IAuthService>();
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
            return Layout.Center() | Text.Muted("No API token. Sign in or configure Sliplane:ApiToken.");

        return new ProjectsView(apiToken);
    }
}

/// <summary>
/// Services app — list services, create, edit, pause, delete.
/// </summary>
[App(icon: Icons.Box, title: "Services", searchHints: ["services", "deploy"])]
public class SliplaneServicesApp : ViewBase
{
    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var client   = this.UseService<SliplaneApiClient>();
        var auth     = this.UseService<IAuthService>();
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
            return Layout.Center() | Text.Muted("No API token. Sign in or configure Sliplane:ApiToken.");

        var projects = this.UseState<List<SliplaneProject>>(() => new List<SliplaneProject>());
        this.UseEffect(async () =>
        {
            try
            {
                var list = await client.GetProjectsAsync(apiToken);
                projects.Set(list);
            }
            catch
            {
                projects.Set(new List<SliplaneProject>());
            }
        });

        return new ServicesView(apiToken, projects.Value ?? []);
    }
}
