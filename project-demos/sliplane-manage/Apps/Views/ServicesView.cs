namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;

/// <summary>
/// Services management view: per-project service list with pause/unpause/redeploy/delete
/// and an inline logs + events drawer.
/// </summary>
public class ServicesView : ViewBase
{
    private readonly string _apiToken;
    private readonly List<SliplaneProject> _projects;

    public ServicesView(string apiToken, List<SliplaneProject> projects)
    {
        _apiToken = apiToken;
        _projects  = projects;
    }

    public override object? Build()
    {
        var client          = this.UseService<SliplaneApiClient>();
        var selectedProject = this.UseState(_projects.FirstOrDefault()?.Id ?? string.Empty);
        var services        = this.UseState<List<SliplaneService>>();
        var loading         = this.UseState(true);
        var error           = this.UseState<string?>();
        var busy            = this.UseState(false);
        var refresh         = this.UseRefreshToken();
        var detailService   = this.UseState<SliplaneService?>();
        var detailTab       = this.UseState(0); // 0=Logs 1=Events 2=Metrics
        // Create service form
        var newServiceName  = this.UseState(string.Empty);
        var newGitRepo      = this.UseState(string.Empty);
        var newBranch       = this.UseState("main");
        var newServerId     = this.UseState(string.Empty);
        var newAutoDeploy   = this.UseState(true);
        var servers         = this.UseState<List<SliplaneServer>>();

        async Task LoadServicesAsync()
        {
            if (string.IsNullOrEmpty(selectedProject.Value))
            {
                loading.Set(false);
                return;
            }

            loading.Set(true);
            error.Value = null;
            try
            {
                var list = await client.GetServicesAsync(_apiToken, selectedProject.Value);
                services.Set(list);
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                loading.Set(false);
            }
        }

        async Task LoadServersAsync()
        {
            try
            {
                var list = await client.GetServersAsync(_apiToken);
                servers.Set(list);
                if (list.Count > 0 && string.IsNullOrEmpty(newServerId.Value))
                    newServerId.Set(list[0].Id);
            }
            catch { /* ignore */ }
        }

        async Task CreateAndDeployServiceAsync()
        {
            if (busy.Value) return;

            if (string.IsNullOrWhiteSpace(selectedProject.Value))
            {
                error.Set("Select a project first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newServiceName.Value))
            {
                error.Set("Please enter an app name.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newGitRepo.Value))
            {
                error.Set("Please enter a Git repository URL.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newServerId.Value))
            {
                error.Set("Please select a server.");
                return;
            }

            error.Set((string?)null);
            busy.Set(true);

            try
            {
                var request = new CreateServiceRequest(
                    Name: newServiceName.Value.Trim(),
                    ServerId: newServerId.Value,
                    Network: new ServiceNetworkRequest(Public: true, Protocol: "http"),
                    Deployment: new RepositoryDeployment(
                        Url: newGitRepo.Value.Trim(),
                        Branch: string.IsNullOrWhiteSpace(newBranch.Value) ? "main" : newBranch.Value.Trim(),
                        AutoDeploy: newAutoDeploy.Value
                    )
                );

                await client.CreateServiceAsync(_apiToken, selectedProject.Value, request);

                // Clear form and reload list
                newServiceName.Set(string.Empty);
                newGitRepo.Set(string.Empty);
                newBranch.Set("main");
                newAutoDeploy.Set(true);

                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                busy.Set(false);
            }
        }

        this.UseEffect(async () =>
        {
            await LoadServersAsync();
            await LoadServicesAsync();
        });

        if (_projects.Count == 0)
            return new Callout("No projects found. Create a project first.", variant: CalloutVariant.Info);

        var projectOptions = _projects
            .Select(p => new Option<string>(p.Name, p.Id))
            .ToArray();

        var serverOptions = (servers.Value ?? new List<SliplaneServer>())
            .Select(s => new Option<string>(s.Name, s.Id))
            .ToArray();

        var currentServices = services.Value ?? new List<SliplaneService>();

        var createCard = new Card(
            Layout.Vertical().Gap(2)
            | Text.H3("Create new service")
            | Text.Muted("Deploy a service from a Git repository to the selected project.")
            | newServiceName.ToTextInput().Placeholder("Service name (e.g. my-api)")
            | newGitRepo.ToTextInput().Placeholder("Git repository URL (e.g. https://github.com/user/repo)")
            | newBranch.ToTextInput().Placeholder("Branch (default: main)")
            | (Layout.Horizontal().Gap(3).Align(Align.Center)
               | Text.Block("Server:").Bold()
               | newServerId.ToSelectInput(serverOptions))
            | (Layout.Horizontal().Gap(3).Align(Align.Center)
               | Text.Block("Auto-deploy on push:").Bold()
               | newAutoDeploy.ToBoolInput())
            | (error.Value is { Length: > 0 } err
                ? (object)new Callout(err, variant: CalloutVariant.Error)
                : Layout.Vertical())
            | (Layout.Horizontal().Gap(2).Align(Align.Right)
               | new Button("Create service")
                    .Icon(Icons.Rocket)
                    .Variant(ButtonVariant.Primary)
                    .Loading(busy.Value)
                    .HandleClick(async () => await CreateAndDeployServiceAsync()))
        ).Width(Size.Fraction(0.6f));

        return Layout.Vertical().Gap(5)
            | Text.H2("Services")
            | Text.Muted("Deploy and manage runtime services for your projects.")
            | (Layout.Horizontal().Gap(3).Align(Align.Center)
               | Text.Block("Project:").Bold()
               | selectedProject.ToSelectInput(projectOptions))
            | createCard
            | (loading.Value
                ? (object)(Layout.Center() | Text.Muted("Loading services..."))
                : BuildServiceTable(client, currentServices, selectedProject.Value, busy, refresh, detailService))
            | (detailService.Value != null
                ? BuildServiceDetail(client, detailService.Value, selectedProject.Value, detailTab)
                : currentServices.Count > 0
                    ? new Callout("Select a service above to view logs, events, and metrics.", variant: CalloutVariant.Info)
                    : Layout.Vertical());
    }

    // ── Service table ─────────────────────────────────────────────────────────

    private object BuildServiceTable(
        SliplaneApiClient client,
        List<SliplaneService> svcs,
        string projectId,
        IState<bool> busy,
        RefreshToken refresh,
        IState<SliplaneService?> detail)
    {
        if (svcs.Count == 0)
            return new Callout("No services in this project yet. Create a service for this project in Sliplane, then open this tab again.", variant: CalloutVariant.Info);

        var rows = svcs
            .Select(s =>
                Layout.Horizontal().Gap(1)
                | Text.Block(s.Name)
                | (s.Image != null ? Text.InlineCode(s.Image) : Text.Muted("git"))
                | Text.Block(s.Port?.ToString() ?? "—")
                | Text.Muted(s.UpdatedAt?.ToString("MM/dd HH:mm") ?? "—")
                | new Spacer()
                | new Button("Logs").Icon(Icons.FileText)
                    .Variant(ButtonVariant.Ghost)
                    .HandleClick(() => detail.Set(detail.Value?.Id == s.Id ? null : s))
                | BuildPauseButton(client, s, projectId, busy, refresh)
                | new Button("Deploy").Icon(Icons.CloudUpload)
                    .Variant(ButtonVariant.Outline)
                    .Loading(busy.Value)
                    .HandleClick(async () =>
                    {
                        busy.Set(true);
                        await client.DeployServiceAsync(_apiToken, projectId, s.Id);
                        busy.Set(false);
                        refresh.Refresh();
                    })
                | new Button("Delete").Icon(Icons.Trash2)
                    .Variant(ButtonVariant.Outline)
                    .HandleClick(async () =>
                    {
                        busy.Set(true);
                        await client.DeleteServiceAsync(_apiToken, projectId, s.Id);
                        busy.Set(false);
                        refresh.Refresh();
                    })
            )
            .ToArray();

        return Layout.Vertical().Gap(2) | rows;
    }

    private object BuildPauseButton(
        SliplaneApiClient client,
        SliplaneService svc,
        string projectId,
        IState<bool> busy,
        RefreshToken refresh)
    {
        var isPaused = svc.Status?.Equals("paused", StringComparison.OrdinalIgnoreCase) == true;

        return isPaused
            ? new Button("Unpause").Icon(Icons.Play)
                .Variant(ButtonVariant.Outline)
                .Loading(busy.Value)
                .HandleClick(async () =>
                {
                    busy.Set(true);
                    await client.UnpauseServiceAsync(_apiToken, projectId, svc.Id);
                    busy.Set(false);
                    refresh.Refresh();
                })
            : new Button("Pause").Icon(Icons.Pause)
                .Variant(ButtonVariant.Outline)
                .Loading(busy.Value)
                .HandleClick(async () =>
                {
                    busy.Set(true);
                    await client.PauseServiceAsync(_apiToken, projectId, svc.Id);
                    busy.Set(false);
                    refresh.Refresh();
                });
    }

    // ── Service detail drawer ─────────────────────────────────────────────────

    private object BuildServiceDetail(
        SliplaneApiClient client,
        SliplaneService svc,
        string projectId,
        IState<int> tab)
    {
        var logs    = this.UseState<List<SliplaneServiceLog>>();
        var events  = this.UseState<List<SliplaneServiceEvent>>();
        var metrics = this.UseState<SliplaneServiceMetrics?>();

        async Task LoadDetailsAsync()
        {
            var l = client.GetServiceLogsAsync(_apiToken, projectId, svc.Id);
            var e = client.GetServiceEventsAsync(_apiToken, projectId, svc.Id);
            var m = client.GetServiceMetricsAsync(_apiToken, projectId, svc.Id);
            await Task.WhenAll(l, e, m);
            logs.Set(await l);
            events.Set(await e);
            metrics.Set(await m);
        }

        // Fire-and-forget; we don't need triggers here
        _ = LoadDetailsAsync();

        var tabs = new[] { "Logs", "Events", "Metrics" };

        return new Card(
            Layout.Vertical().Gap(3)
            | Text.H4($"Service: {svc.Name}")
            | Layout.Tabs(
                new Tab("Logs",    BuildLogs(logs.Value)),
                new Tab("Events",  BuildEvents(events.Value)),
                new Tab("Metrics", BuildMetrics(metrics.Value))
              ).Variant(TabsVariant.Tabs)
        );
    }

    private static object BuildLogs(List<SliplaneServiceLog>? logs)
    {
        if (logs == null) return Text.Muted("Loading logs...");
        if (logs.Count == 0) return Text.Muted("No log entries found.");

        return Layout.Vertical().Gap(1).Height(Size.Units(300)).Scroll()
               | logs.TakeLast(100).Select(l =>
                   Layout.Horizontal().Gap(2)
                   | Text.Muted(l.Timestamp.ToString("HH:mm:ss"))
                   | Text.Block(l.Line));
    }

    private static object BuildEvents(List<SliplaneServiceEvent>? events)
    {
        if (events == null) return Text.Muted("Loading events...");
        if (events.Count == 0) return Text.Muted("No events found.");

        var rows = events
            .Select(e =>
                Layout.Horizontal().Gap(2)
                | Text.Muted(e.CreatedAt.ToString("MM/dd HH:mm"))
                | new Badge(e.Type)
                | Text.Block(e.Message)
            )
            .ToArray();

        return Layout.Vertical().Gap(1) | rows;
    }

    private static object BuildMetrics(SliplaneServiceMetrics? m)
    {
        if (m == null) return Text.Muted("Loading metrics...");

        return Layout.Horizontal().Gap(4).Wrap()
               | MetricBox("CPU Usage", $"{m.CpuUsagePercent:F1}%", Icons.Cpu)
               | MetricBox("Memory", $"{m.MemoryUsageMb:F0} / {m.MemoryTotalMb:F0} MB", Icons.HardDrive);
    }

    private static object MetricBox(string label, string value, Icons icon)
    {
        return new Card(
            Layout.Vertical().Gap(1).Align(Align.Center)
            | icon.ToIcon()
            | Text.H3(value).Bold()
            | Text.Muted(label)
        ).Width(Size.Units(180));
    }
}
