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
[App(icon: Icons.Box, title: "Services", searchHints: ["services", "deploy"])]
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
        }, reloadCounter);

        this.UseEffect(() => refreshReceiver.Receive(_ =>
        {
            reloadCounter.Set(reloadCounter.Value + 1);
            return new Unit();
        }));

        return new ServicesView(apiToken, projects.Value ?? []);
    }
}
