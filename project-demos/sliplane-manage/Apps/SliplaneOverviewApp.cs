namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Models;
using SliplaneManage.Services;

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
[App(id: "sliplane-services-app", icon: Icons.Box, title: "Services", searchHints: ["services", "deploy"])]
public class SliplaneServicesApp : ViewBase
{
    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var client   = this.UseService<SliplaneApiClient>();
        var auth     = this.UseService<IAuthService>();
        var refreshReceiver = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        var reloadCounter = this.UseState(0);
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
            return Layout.Center() | Text.Muted("No API token. Sign in or configure Sliplane:ApiToken.");

        var projectsQuery = this.UseQuery(
            key: ("services-projects", apiToken, reloadCounter.Value),
            fetcher: async ct => await client.GetProjectsAsync(apiToken));

        this.UseEffect(() => refreshReceiver.Receive(_ =>
        {
            reloadCounter.Set(reloadCounter.Value + 1);
            return new Unit();
        }));

        var projects = projectsQuery.Value ?? new List<SliplaneProject>();
        return new ServicesView(apiToken, projects);
    }
}
