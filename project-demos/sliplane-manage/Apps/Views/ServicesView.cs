namespace SliplaneManage.Apps.Views;

using System.Text.Json;
using SliplaneManage.Models;
using SliplaneManage.Services;
using Ivy.Helpers;

/// <summary>
/// Services view: list all services in a DataTable; details/create/edit in sheets.
///
/// Refresh flow (simple and reliable, using UseRefreshToken):
///   1. UseRefreshToken is the single source of truth for \"reload services list\".
///   2. UseEffect(OnMount + refreshToken) calls SliplaneApiClient.GetOverviewAsync
///      and stores servers + services in UseState.
///   3. DataTable is always built from the current state (rows.AsQueryable()).
///   4. Sheets send SliplaneRefreshSignal after mutations; ServicesView reacts
///      with refreshToken.Refresh(), which restarts the effect and reloads the table.
///
/// BuildServiceRows is a pure function: no hooks, no UseQuery.
/// </summary>
public class ServicesView : ViewBase
{
    private readonly string _apiToken;
    private readonly List<SliplaneProject> _projects;

    public ServicesView(string apiToken, List<SliplaneProject> projects)
    {
        _apiToken = apiToken;
        _projects = projects;
    }

    public override object? Build()
{
    // ── infrastructure ────────────────────────────────────────────────────
    var client          = this.UseService<SliplaneApiClient>();
    var refreshToken    = this.UseRefreshToken();
    var serviceDetailOpen      = this.UseState(false);
    var serviceDetailSelection = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
    var (createSheetView, openCreateSheet) = this.UseTrigger(
        (IState<bool> isOpen) => new CreateServiceSheet(isOpen, _apiToken, _projects));

    // ── 1) data loading: UseQuery with polling ───────────────────────────────
    var overviewKey = $"overview:{_apiToken}";
    var overviewQuery = this.UseQuery<SliplaneOverview?, string>(
        key: overviewKey,
        fetcher: async ct =>
        {
            var result = await client.GetOverviewAsync(_apiToken);

            // Tell the DataTable to re-read rows after each successful fetch.
            refreshToken.Refresh();
            return result;
        },
        options: new QueryOptions
        {
            KeepPrevious = true,
            RefreshInterval = TimeSpan.FromSeconds(3),
            RevalidateOnMount = true
        });

    // Current data for the table derived directly from overviewQuery
    var overview = overviewQuery.Value;
        var currentServices = overview == null
            ? new List<(string ProjectId, string ProjectName, SliplaneService Service)>()
            : overview.ServicesByProject
                .SelectMany(kv =>
                {
                    var projectName = overview.Projects.FirstOrDefault(p => p.Id == kv.Key)?.Name ?? kv.Key;
                    return kv.Value.Select(svc => (ProjectId: kv.Key, ProjectName: projectName, Service: svc));
                })
                .ToList();
    var currentServers  = overview?.Servers ?? new List<SliplaneServer>();
    var eventsByService = overview?.EventsByService ?? new Dictionary<string, List<SliplaneServiceEvent>>();

    // ── build rows (pure – no hooks) ──────────────────────────────────────
    var rows = BuildServiceRows(currentServices, currentServers, eventsByService);

    // ── UI ────────────────────────────────────────────────────────────────
    void ShowServiceSheet(string projectId, string projectName, SliplaneService svc)
    {
        serviceDetailSelection.Set((projectId, projectName, svc));
        serviceDetailOpen.Set(true);
    }

    // Use overviewQuery directly for loading/error state to avoid stale local flags.
    if (overviewQuery.Loading && overviewQuery.Value == null && currentServices.Count == 0)
        return Layout.Center() | Text.Muted("Loading all services...");

    if (overviewQuery.Error is { } errEx)
        return new Callout($"Error: {errEx.Message}", variant: CalloutVariant.Error);

    var headerRow      = Layout.Horizontal().Height(Size.Fit()) | Text.H2("Services");
    var addServiceBtn  = new Button("Add service").Icon(Icons.Plus).OnClick(_ => openCreateSheet()).Large().Secondary().BorderRadius(BorderRadius.Full);
    var addServiceFloat = new FloatingPanel(addServiceBtn, Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

    // ── DataTable + RefreshToken as in the official example ───────────────
    var table = rows
        .AsQueryable()
        .ToDataTable(r => r.ServiceId)
        .RefreshToken(refreshToken)
        .Height(Size.Full())
        .Hidden(r => r.ServiceId)
        .Header(r => r.Name, "Service")
        .Header(r => r.Project, "Project")
        .Header(r => r.Server, "Server")
        .Header(r => r.StatusIcon, "Icon")
        .Header(r => r.Status, "Name")
        .Header(r => r.LastUpdated, "Last updated")
        .Header(r => r.DeployStatus, "Logs")
        .Header(r => r.Url, "URL")
        .Group(r => r.Name, "Identity")
        .Group(r => r.Project, "Identity")
        .Group(r => r.Server, "Identity")
        .Group(r => r.StatusIcon, "Status")
        .Group(r => r.Status, "Status")
        .Group(r => r.LastUpdated, "Deploy")
        .Group(r => r.DeployStatus, "Deploy")
        .Group(r => r.Url, "Routing")
        .Width(r => r.StatusIcon, Size.Px(50))
        .Width(r => r.Status, Size.Px(120))
        .Width(r => r.LastUpdated, Size.Px(130))
        .Width(r => r.DeployStatus, Size.Px(200))
        .Config(config =>
        {
            config.ShowGroups      = true;
            config.ShowIndexColumn = false;
            config.AllowSorting    = true;
            config.AllowFiltering  = true;
            config.ShowSearch      = true;
            config.SelectionMode   = SelectionModes.Rows;
        })
        .RowActions(MenuItem.Default(Icons.Eye, "view").Label("View"))
        .OnRowAction(async e =>
        {
            var args = e.Value;
            if (args is null) return;
            var id = args.Id?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id)) return;

            var match = currentServices.FirstOrDefault(cs => cs.Service.Id == id);
            if (match.Service == null || string.IsNullOrWhiteSpace(match.ProjectId)) return;
            ShowServiceSheet(match.ProjectId, match.ProjectName, match.Service);
            await ValueTask.CompletedTask;
        })
        .Renderer(e => e.Url, new LinkDisplayRenderer { Type = LinkDisplayType.Url });

    object content = currentServices.Count == 0
        ? (object)(Layout.Vertical() | headerRow | new Callout("No services found.", variant: CalloutVariant.Info) | table)
        : Layout.Vertical().Height(Size.Full()) | headerRow | table;

    return new Fragment(
        content,
        addServiceFloat,
        serviceDetailOpen.Value ? new ServiceDetailsSheet(serviceDetailOpen, _apiToken, serviceDetailSelection) : null,
        createSheetView);
}

    // ── pure data types ────────────────────────────────────────────────────────

    private sealed record ServiceRow(
        string ServiceId,
        string Name,
        string Project,
        string Server,
        string Status,
        Icons  StatusIcon,
        string LastUpdated,
        string DeployStatus,
        string Url);

    // ── static helpers (no hooks) ──────────────────────────────────────────────

    internal static (string Label, Icons Icon, string? EventType) GetServiceStatus(
        SliplaneService svc, List<SliplaneServiceEvent> events)
    {
        var rawStatus = svc.Status ?? string.Empty;

        var ev = events.OrderByDescending(e => e.CreatedAt).FirstOrDefault(e =>
            e.Type is "service_suspend" or "service_suspend_success"
                   or "service_resume"  or "service_resume_success");

        if (ev != null)
        {
            var msg = ev.Message ?? string.Empty;
            return ev.Type switch
            {
                "service_suspend"         => (msg, Icons.LoaderCircle, ev.Type),
                "service_resume"          => (msg, Icons.LoaderCircle, ev.Type),
                "service_suspend_success" => ("suspended", Icons.Pause,       ev.Type),
                "service_resume_success"  => ("live",      Icons.CircleCheck, ev.Type),
                _                         => (rawStatus,   Icons.MonitorStop, ev.Type)
            };
        }

        return rawStatus.ToLowerInvariant() switch
        {
            "live"                => ("live",      Icons.CircleCheck, null),
            "suspended" or "paused" => ("suspended", Icons.Pause,    null),
            "error"     or "failed" => ("error",     Icons.CircleX,  null),
            "pending"               => ("pending",   Icons.LoaderCircle, null),
            _ => (string.IsNullOrWhiteSpace(rawStatus) ? "—" : rawStatus, Icons.MonitorStop, null)
        };
    }

    /// <summary>
    /// Converts a raw Sliplane event type string to a human-readable log label.
    /// </summary>
    private static string FormatEventType(string? type) => type switch
    {
        "service_resume_success"  => "Service resumed successfully",
        "service_resume"          => "Service resume requested",
        "service_suspend_success" => "Service suspended successfully",
        "service_suspend"         => "Service suspension requested",
        "service_deploy_success"  => "Service deployed successfully",
        "service_deploy"          => "Service deploy started",
        "service_deploy_failed"   => "Service deploy failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };

    /// <summary>
    /// Pure function – builds ServiceRow[] from already-loaded data.
    /// Must NOT call any Ivy hooks (UseQuery, UseState, UseEffect, …).
    /// </summary>
    private static ServiceRow[] BuildServiceRows(
        List<(string ProjectId, string ProjectName, SliplaneService Service)> currentServices,
        List<SliplaneServer> serverList,
        Dictionary<string, List<SliplaneServiceEvent>> eventsByService)
    {
        return currentServices
            .Select(t =>
            {
                var (_, projectName, svc) = t;

                var serverLabel = string.IsNullOrWhiteSpace(svc.ServerId)
                    ? "—"
                    : (serverList.FirstOrDefault(s => s.Id == svc.ServerId)?.Name ?? svc.ServerId);

                var events = eventsByService.TryGetValue(svc.Id, out var ev)
                    ? ev
                    : new List<SliplaneServiceEvent>();

                var (statusLabel, statusIcon, _) = GetServiceStatus(svc, events);

                var lastUpdatedInstant = svc.UpdatedAt ?? svc.CreatedAt;

                string deployStatus = "—";
                if (events.Count > 0)
                {
                    // Format each event as:
                    //   Service resumed successfully
                    //   11.03.2026, 16:47:19
                    //   triggered by manual deploy
                    deployStatus = string.Join("\n\n",
                        events
                            .OrderByDescending(e => e.CreatedAt)
                            .Take(10)
                            .Select(e =>
                            {
                                var label = string.IsNullOrWhiteSpace(e.Message)
                                    ? FormatEventType(e.Type)
                                    : e.Message;
                                var dateStr = e.CreatedAt.ToLocalTime()
                                    .ToString("dd.MM.yyyy, HH:mm:ss");
                                var trigger = "triggered by manual deploy";
                                return $"{label}\n{dateStr}\n{trigger}";
                            }));
                }

                var siteUrl = svc.Network?.CustomDomains?.FirstOrDefault()?.Domain
                              ?? svc.Network?.ManagedDomain
                              ?? string.Empty;
                var siteUrlAbsolute = string.IsNullOrWhiteSpace(siteUrl) ? string.Empty
                    : (siteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                       || siteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        ? siteUrl
                        : "https://" + siteUrl);

                return new
                {
                    SortKey = lastUpdatedInstant,
                    Row = new ServiceRow(
                        ServiceId:    svc.Id,
                        Name:         svc.Name,
                        Project:      projectName,
                        Server:       serverLabel,
                        Status:       statusLabel,
                        StatusIcon:   statusIcon,
                        LastUpdated:  lastUpdatedInstant.ToString("yyyy-MM-dd HH:mm"),
                        DeployStatus: deployStatus,
                        Url:          siteUrlAbsolute)
                };
            })
            .OrderByDescending(x => x.SortKey)
            .Select(x => x.Row)
            .ToArray();
    }
}

/// <summary>
/// Sheet with full service settings (read-only). Footer: Edit, Pause/Resume, Delete.
/// </summary>
public class ServiceDetailsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly IState<(string ProjectId, string ProjectName, SliplaneService Service)?> _selection;

    public ServiceDetailsSheet(
        IState<bool> isOpen,
        string apiToken,
        IState<(string ProjectId, string ProjectName, SliplaneService Service)?> selection)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _selection = selection;
    }

    public override object? Build()
    {
        var sel = _selection.Value;
        if (sel == null || !_isOpen.Value)
            return null;

        var (projectId, projectName, service) = sel.Value;
        var client = this.UseService<SliplaneApiClient>();
        // SliplaneRefreshSignal expects string payloads.
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();
        var busy = this.UseState(false);

        var dep = service.Deployment;
        var net = service.Network;

        var eventsQuery = this.UseQuery<List<SliplaneServiceEvent>?, (string, string, string)>(
            key: ("events", projectId, service.Id),
            fetcher: async _ => await client.GetServiceEventsAsync(_apiToken, projectId, service.Id),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(3), KeepPrevious = true });
            
        var events = eventsQuery.Value ?? new List<SliplaneServiceEvent>();
        var (statusLabel, _, eventType) = ServicesView.GetServiceStatus(service, events);

        var basicModel = new
        {
            Project = projectName,
            Name = service.Name,
            Status = statusLabel,
            ServerId = service.ServerId,
            Image = service.Image ?? "—",
            Port = service.Port?.ToString() ?? "—",
            Created = service.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            Updated = service.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"
        };

        var deploymentModel = new
        {
            Url = dep?.Url ?? "—",
            Branch = dep?.Branch ?? "—",
            Dockerfile = dep?.DockerfilePath ?? "—",
            Context = dep?.DockerContext ?? "—",
            AutoDeploy = dep?.AutoDeploy == true ? "Yes" : "No"
        };

        var networkModel = new
        {
            Public = net?.Public == true ? "Yes" : "No",
            Protocol = net?.Protocol ?? "—",
            ManagedDomain = net?.ManagedDomain ?? "—",
            InternalDomain = net?.InternalDomain ?? "—"
        };

        bool isResumingOrSuspending = eventType == "service_suspend" || eventType == "service_resume";
        var isPaused = statusLabel == "suspended";
        var pauseLabel = isPaused ? "Resume" : "Pause";

        var (editSheetView, openEditSheet) = this.UseTrigger((IState<bool> isOpen) =>
            new EditServiceSheet(isOpen, _apiToken, projectId, projectName, service, _selection));

        var footer = Layout.Horizontal()
            | new Button("Edit").Icon(Icons.Pencil).Variant(ButtonVariant.Outline).OnClick(_ => openEditSheet())
            | new Button(pauseLabel).Icon(isPaused ? Icons.Play : Icons.Pause).Variant(ButtonVariant.Outline).Loading(busy.Value || isResumingOrSuspending)
                .OnClick(async _ => 
                {
                    if (busy.Value || isResumingOrSuspending) return;
                    busy.Set(true);
                    try
                    {
                        SliplaneService updatedService;
                        if (isPaused)
                        {
                            await client.UnpauseServiceAsync(_apiToken, projectId, service.Id);
                            // Optimistic local update so the sheet status changes immediately
                            updatedService = service with { Status = "live" };
                        }
                        else
                        {
                            await client.PauseServiceAsync(_apiToken, projectId, service.Id);
                            // Optimistic local update so the sheet status changes immediately
                            updatedService = service with { Status = "suspended" };
                        }

                        // Ensure selection uses the updated service instance so Build() re-runs with new status
                        _selection.Set((projectId, projectName, updatedService));

                        // Refresh local events so the status text in the sheet updates quickly
                        eventsQuery.Mutator.Revalidate();

                        // Also notify the main list via JSON payload so that the DataTable
                        // can optimistically patch the full service (name, status, etc.).
                        await refreshSender.Send("service-json:" + JsonSerializer.Serialize(updatedService));
                    }
                    finally { busy.Set(false); }
                })
            | new Button("Delete", onClick: async _ => 
                {
                    if (busy.Value) return;
                    busy.Set(true);
                    try
                    {
                        await client.DeleteServiceAsync(_apiToken, projectId, service.Id);
                        _isOpen.Set(false);
                        await refreshSender.Send("services");
                    }
                    finally { busy.Set(false); }
                })
                .Icon(Icons.Trash).Variant(ButtonVariant.Destructive).Loading(busy.Value)
                .WithConfirm("Are you sure you want to delete this service?", "Delete service");

        var body = Layout.Vertical()
            | basicModel.ToDetails()
            | Text.H4("Deployment")
            | deploymentModel.ToDetails()
            | Text.H4("Network")
            | networkModel.ToDetails()
            | (service.Domains?.Count > 0 == true
                ? Layout.Vertical()
                    | Text.H4("Domains")
                    | (Layout.Vertical() | service.Domains.Select(d => Text.Block($"{d.Domain} (custom: {d.IsCustom})")).ToArray<object>())
                : Layout.Vertical());

        if (!_isOpen.Value)
            return null;

        var sheetBody = new FooterLayout(footer, body);
        return new Fragment(
            new Sheet(_ => _isOpen.Set(false), sheetBody, title: $"Service: {service.Name}").Width(Size.Fraction(1 / 3f)),
            editSheetView
        );
    }
}

/// <summary>
/// Sheet to PATCH service: name, deployment, env, healthcheck, cmd.
/// </summary>
public class EditServiceSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _projectName;
    private readonly SliplaneService _service;
    private readonly IState<(string ProjectId, string ProjectName, SliplaneService Service)?> _selection;

    public EditServiceSheet(
        IState<bool> isOpen,
        string apiToken,
        string projectId,
        string projectName,
        SliplaneService service,
        IState<(string ProjectId, string ProjectName, SliplaneService Service)?> selection)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projectId = projectId;
        _projectName = projectName;
        _service = service;
        _selection = selection;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();
        var dep = _service.Deployment;
        var name = this.UseState(_service.Name ?? string.Empty);
        var deployUrl = this.UseState(dep?.Url ?? string.Empty);
        var branch = this.UseState(dep?.Branch ?? "main");
        var dockerfilePath = this.UseState(dep?.DockerfilePath ?? "Dockerfile");
        var dockerContext = this.UseState(dep?.DockerContext ?? ".");
        var autoDeploy = this.UseState(dep?.AutoDeploy ?? true);
        var cmd = this.UseState(string.Empty);
        var healthcheck = this.UseState(string.Empty);
        var busy = this.UseState(false);
        var error = this.UseState<string?>(() => (string?)null);
        var envList = this.UseState<List<EnvironmentVariable>>(() => new List<EnvironmentVariable>());
        var showAddEnvDialog = this.UseState(false);
        var addEnvKey = this.UseState(string.Empty);
        var addEnvValue = this.UseState(string.Empty);

        async Task SaveAsync()
        {
            if (busy.Value) return;
            if (string.IsNullOrWhiteSpace(name.Value)) { error.Set("Enter service name."); return; }
            if (string.IsNullOrWhiteSpace(deployUrl.Value)) { error.Set("Enter deployment URL."); return; }
            error.Set((string?)null);
            busy.Set(true);
            try
            {
                var request = ServiceRequestFactory.BuildUpdateRequest(
                    name: name.Value,
                    deployUrl: deployUrl.Value,
                    branch: branch.Value,
                    dockerfilePath: dockerfilePath.Value,
                    dockerContext: dockerContext.Value,
                    autoDeploy: autoDeploy.Value,
                    cmd: cmd.Value,
                    healthcheck: healthcheck.Value,
                    env: envList.Value
                );
                await client.UpdateServiceAsync(_apiToken, _projectId, _service.Id, request);
                var updated = await client.GetServiceAsync(_apiToken, _projectId, _service.Id);
                if (updated != null)
                {
                    _selection.Set((_projectId, _projectName, updated));
                    // Optimistically update the main list/DataTable via JSON payload.
                    await refreshSender.Send("service-json:" + JsonSerializer.Serialize(updated));
                }
                _isOpen.Set(false);
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

        var envItems = envList.Value ?? new List<EnvironmentVariable>();
        var envHeaderRow = new TableRow(
            new TableCell("Key").IsHeader(),
            new TableCell("Value").IsHeader(),
            new TableCell("Actions").IsHeader().Width(Size.Fit()));
        var envDataRows = envItems
            .Select((e, idx) =>
            {
                var index = idx;
                return new TableRow(
                    new TableCell(e.Key),
                    new TableCell(e.Value ?? ""),
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).OnClick(_ =>
                    {
                        var next = envList.Value.Where((_, i) => i != index).ToList();
                        envList.Set(next);
                    })).Width(Size.Fit()));
            })
            .ToArray();
        object envTableContent = envDataRows.Length == 0
            ? (object)Text.Muted("No variables.")
            : new Table(new[] { envHeaderRow }.Concat(envDataRows).ToArray()).Width(Size.Full());

        var content = Layout.Vertical()
            | Text.H4("Basic")
            | name.ToTextInput().Placeholder("Service name")
            | Text.H4("Deployment")
            | deployUrl.ToTextInput().Placeholder("Repository or image URL")
            | branch.ToTextInput().Placeholder("Branch")
            | dockerfilePath.ToTextInput().Placeholder("Dockerfile path")
            | dockerContext.ToTextInput().Placeholder("Docker context")
            | autoDeploy.ToBoolInput().Label("Auto-deploy on push")
            | Text.H4("Optional")
            | cmd.ToTextInput().Placeholder("Start command (e.g. npm start)")
            | healthcheck.ToTextInput().Placeholder("Health check path (e.g. /health)")
            | Text.H4("Environment variables")
            | envTableContent
            | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).OnClick(_ => showAddEnvDialog.Set(true))
            | (error.Value is { Length: > 0 } err ? (object)new Callout(err, variant: CalloutVariant.Error) : Layout.Vertical());

        var footer = Layout.Horizontal()
            | new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => _isOpen.Set(false))
            | new Button("Save").Icon(Icons.Check).Variant(ButtonVariant.Primary).Loading(busy.Value).OnClick(async _ => await SaveAsync());

        Dialog? addEnvDialog = null;
        if (showAddEnvDialog.Value)
        {
            void SaveEnv()
            {
                if (string.IsNullOrWhiteSpace(addEnvKey.Value)) return;
                var next = (envList.Value ?? new List<EnvironmentVariable>()).ToList();
                next.Add(new EnvironmentVariable(addEnvKey.Value.Trim(), addEnvValue.Value ?? string.Empty, false));
                envList.Set(next);
                addEnvKey.Set(string.Empty);
                addEnvValue.Set(string.Empty);
                showAddEnvDialog.Set(false);
            }
            var envForm = Layout.Vertical()
                | addEnvKey.ToTextInput().Placeholder("Key")
                | addEnvValue.ToTextInput().Placeholder("Value");
            addEnvDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddEnvDialog.Set(false),
                header: new DialogHeader("Add environment variable"),
                body: new DialogBody(envForm),
                footer: new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).OnClick(_ => SaveEnv()),
                    new Button("Cancel").OnClick(_ => showAddEnvDialog.Set(false))
                )).Width(Size.Units(220));
        }

        if (!_isOpen.Value)
            return null;

        var sheetBody = new FooterLayout(footer, content);
        object sheetContent = addEnvDialog != null ? new Fragment(sheetBody, addEnvDialog) : sheetBody;
        return new Sheet(_ => _isOpen.Set(false), sheetContent, title: $"Edit: {_service.Name}").Width(Size.Fraction(1 / 3f));
    }
}

/// <summary>
/// Sheet to create a new service with all Sliplane API fields (deployment, network, cmd, healthcheck, env, volumes).
/// Uses FooterLayout and plain sections (no Cards).
/// </summary>
public class CreateServiceSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly List<SliplaneProject> _projects;

    public CreateServiceSheet(IState<bool> isOpen, string apiToken, List<SliplaneProject> projects)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projects = projects;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();
        var serverVolumes = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var selectedProjectId = this.UseState(string.Empty);
        var name = this.UseState(string.Empty);
        var serverId = this.UseState(string.Empty);
        var gitRepo = this.UseState(string.Empty);
        var branch = this.UseState("main");
        var dockerfilePath = this.UseState("Dockerfile");
        var dockerContext = this.UseState(".");
        var autoDeploy = this.UseState(true);
        var cmd = this.UseState(string.Empty);
        var healthcheck = this.UseState("/");
        var networkPublic = this.UseState(true);
        var networkProtocol = this.UseState("http");
        var busy = this.UseState(false);
        var error = this.UseState<string?>(() => (string?)null);
        // Dynamic env list + dialog to add
        var envList = this.UseState<List<EnvironmentVariable>>(() => new List<EnvironmentVariable>());
        var showAddEnvDialog = this.UseState(false);
        var addEnvKey = this.UseState(string.Empty);
        var addEnvValue = this.UseState(string.Empty);
        // Dynamic volume mounts list + dialog to add
        var volumeMountsList = this.UseState<List<(string VolumeId, string MountPath)>>(() => new List<(string, string)>());
        var showAddVolumeDialog = this.UseState(false);
        var addVolumeId = this.UseState(string.Empty);
        var addMountPath = this.UseState(string.Empty);

        QueryResult<Option<string>[]> QueryProjects(IViewContext ctx, string query)
        {
            return ctx.UseQuery<Option<string>[], (string, string, int)>(
                key: (nameof(QueryProjects), query, 0),
                fetcher: async ct =>
                {
                    var list = await client.GetProjectsAsync(_apiToken);
                    return list
                        .Where(p => string.IsNullOrEmpty(query) || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Take(20)
                        .Select(p => new Option<string>(p.Name, p.Id))
                        .ToArray();
                });
        }

        QueryResult<Option<string>?> LookupProject(IViewContext ctx, string? id)
        {
            return ctx.UseQuery<Option<string>?, (string, string?, int)>(
                key: (nameof(LookupProject), id, 0),
                fetcher: async ct =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var list = await client.GetProjectsAsync(_apiToken);
                    var p = list.FirstOrDefault(pj => pj.Id == id);
                    return p != null ? new Option<string>(p.Name, p.Id) : null;
                });
        }

        QueryResult<Option<string>[]> QueryServers(IViewContext ctx, string query)
        {
            return ctx.UseQuery<Option<string>[], (string, string, int)>(
                key: (nameof(QueryServers), query, 0),
                fetcher: async ct =>
                {
                    var list = await client.GetServersAsync(_apiToken);
                    return list
                        .Where(s => string.IsNullOrEmpty(query) || s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Take(20)
                        .Select(s => new Option<string>(s.Name, s.Id))
                        .ToArray();
                });
        }

        QueryResult<Option<string>?> LookupServer(IViewContext ctx, string? id)
        {
            return ctx.UseQuery<Option<string>?, (string, string?, int)>(
                key: (nameof(LookupServer), id, 0),
                fetcher: async ct =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var list = await client.GetServersAsync(_apiToken);
                    var server = list.FirstOrDefault(s => s.Id == id);
                    return server != null ? new Option<string>(server.Name, server.Id) : null;
                });
        }

        this.UseEffect(async () =>
        {
            if (string.IsNullOrWhiteSpace(serverId.Value))
            {
                serverVolumes.Set(new List<SliplaneVolume>());
                return;
            }
            try
            {
                var vols = await client.GetServerVolumesAsync(_apiToken, serverId.Value);
                serverVolumes.Set(vols ?? new List<SliplaneVolume>());
            }
            catch
            {
                serverVolumes.Set(new List<SliplaneVolume>());
            }
        }, serverId);

        var volumeOptions = (serverVolumes.Value ?? new List<SliplaneVolume>()).Select(v => new Option<string>($"{v.Name} ({v.MountPath})", v.Id)).ToArray();
        var protocolOptions = new[] { new Option<string>("HTTP", "http"), new Option<string>("HTTPS", "https") };

        async Task CreateAsync()
        {
            if (busy.Value) return;
            if (string.IsNullOrWhiteSpace(selectedProjectId.Value)) { error.Set("Select a project."); return; }
            if (string.IsNullOrWhiteSpace(name.Value)) { error.Set("Enter service name."); return; }
            if (string.IsNullOrWhiteSpace(gitRepo.Value)) { error.Set("Enter repository or image URL."); return; }
            if (string.IsNullOrWhiteSpace(serverId.Value)) { error.Set("Select a server."); return; }
            error.Set((string?)null);
            busy.Set(true);
            try
            {
                var request = ServiceRequestFactory.BuildCreateRequest(
                    name: name.Value,
                    serverId: serverId.Value,
                    gitRepo: gitRepo.Value,
                    branch: branch.Value,
                    dockerfilePath: dockerfilePath.Value,
                    dockerContext: dockerContext.Value,
                    autoDeploy: autoDeploy.Value,
                    networkPublic: networkPublic.Value,
                    networkProtocol: networkProtocol.Value,
                    cmd: cmd.Value,
                    healthcheck: healthcheck.Value,
                    env: envList.Value,
                    volumeMounts: volumeMountsList.Value
                );
                await client.CreateServiceAsync(_apiToken, selectedProjectId.Value, request);
                _isOpen.Set(false);
                await refreshSender.Send("services");
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

        var basicSection = Layout.Vertical()
            | Text.H4("Basic")
            | selectedProjectId.ToAsyncSelectInput(QueryProjects, LookupProject, placeholder: "Search project...")
            | name.ToTextInput().Placeholder("Service name")
            | serverId.ToAsyncSelectInput(QueryServers, LookupServer, placeholder: "Search server...");

        var deploymentSection = Layout.Vertical()
            | Text.H4("Deployment (source and build)")
            | gitRepo.ToTextInput().Placeholder("Repository URL (e.g. https://github.com/user/repo or docker.io/image)")
            | branch.ToTextInput().Placeholder("Branch (default: main)")
            | dockerfilePath.ToTextInput().Placeholder("Dockerfile path")
            | dockerContext.ToTextInput().Placeholder("Docker context")
            | autoDeploy.ToBoolInput().Label("Auto-deploy on push");

        var optionalSection = Layout.Vertical()
            | Text.H4("Optional (command, healthcheck)")
            | healthcheck.ToTextInput().Placeholder("Health check path (e.g. /health)");

        var networkSection = Layout.Vertical()
            | Text.H4("Network (public, protocol)")
            | networkPublic.ToBoolInput().Label("Public access")
            | networkProtocol.ToSelectInput(protocolOptions);

        // Env: Table widget + Add button; dialog to add one entry
        var envItems = envList.Value ?? new List<EnvironmentVariable>();
        var envHeaderRow = new TableRow(
            new TableCell("Key").IsHeader(),
            new TableCell("Value").IsHeader(),
            new TableCell("Actions").IsHeader().Width(Size.Fit()));
        var envDataRows = envItems
            .Select((e, idx) =>
            {
                var index = idx;
                return new TableRow(
                    new TableCell(e.Key),
                    new TableCell(e.Value ?? ""),
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).OnClick(_ =>
                    {
                        var next = envList.Value.Where((_, i) => i != index).ToList();
                        envList.Set(next);
                    })).Width(Size.Fit()));
            })
            .ToArray();
        object envTableContent = envDataRows.Length == 0
            ? (object)Text.Muted("No variables added.")
            : new Table(new[] { envHeaderRow }.Concat(envDataRows).ToArray()).Width(Size.Full());
        var envSection = Layout.Vertical()
            | Text.H4("Environment variables")
            | envTableContent
            | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).OnClick(_ => showAddEnvDialog.Set(true));

        // Volumes: Table widget + Add button; dialog to add one mount
        var vols = serverVolumes.Value ?? new List<SliplaneVolume>();
        var volItems = volumeMountsList.Value ?? new List<(string VolumeId, string MountPath)>();
        var volHeaderRow = new TableRow(
            new TableCell("Volume").IsHeader(),
            new TableCell("Mount path").IsHeader(),
            new TableCell("Actions").IsHeader().Width(Size.Fit()));
        var volDataRows = volItems
            .Select((v, idx) =>
            {
                var index = idx;
                var volName = vols.FirstOrDefault(vol => vol.Id == v.VolumeId)?.Name ?? v.VolumeId;
                return new TableRow(
                    new TableCell(volName),
                    new TableCell(v.MountPath),
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).OnClick(_ =>
                    {
                        var next = volumeMountsList.Value.Where((_, i) => i != index).ToList();
                        volumeMountsList.Set(next);
                    })));
            })
            .ToArray();
        object volTableContent = volDataRows.Length == 0
            ? (object)Text.Muted("No volume mounts. Select a server first, then add.")
            : new Table(new[] { volHeaderRow }.Concat(volDataRows).ToArray()).Width(Size.Full());
        var volumesSection = Layout.Vertical()
            | Text.H4("Volumes (attach server volumes)")
            | volTableContent
            | new Button("Add volume").Icon(Icons.Plus).Variant(ButtonVariant.Outline).OnClick(_ => showAddVolumeDialog.Set(true));

        var errorBlock = error.Value is { Length: > 0 } err
            ? (object)new Callout(err, variant: CalloutVariant.Error)
            : Layout.Vertical();

        var content = Layout.Vertical()
            | basicSection
            | deploymentSection
            | optionalSection
            | networkSection
            | envSection
            | volumesSection
            | errorBlock;

        var footer = Layout.Horizontal()
            | new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => _isOpen.Set(false))
            | new Button("Create").Icon(Icons.Plus).Variant(ButtonVariant.Primary).Loading(busy.Value).OnClick(async _ => await CreateAsync());

        // Dialog: Add environment variable
        Dialog? addEnvDialog = null;
        if (showAddEnvDialog.Value)
        {
            void SaveEnv()
            {
                if (string.IsNullOrWhiteSpace(addEnvKey.Value)) return;
                var next = (envList.Value ?? new List<EnvironmentVariable>()).ToList();
                next.Add(new EnvironmentVariable(addEnvKey.Value.Trim(), addEnvValue.Value ?? string.Empty, false));
                envList.Set(next);
                addEnvKey.Set(string.Empty);
                addEnvValue.Set(string.Empty);
                showAddEnvDialog.Set(false);
            }
            var envForm = Layout.Vertical()
                | addEnvKey.ToTextInput().Placeholder("Key (e.g. DATABASE_URL)")
                | addEnvValue.ToTextInput().Placeholder("Value");
            addEnvDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddEnvDialog.Set(false),
                header: new DialogHeader("Add environment variable"),
                body: new DialogBody(envForm),
                footer: new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).OnClick(_ => SaveEnv()),
                    new Button("Cancel").OnClick(_ => showAddEnvDialog.Set(false))
                )).Width(Size.Units(220));
        }

        // Dialog: Add volume mount
        Dialog? addVolumeDialog = null;
        if (showAddVolumeDialog.Value)
        {
            void SaveVolume()
            {
                if (string.IsNullOrWhiteSpace(addVolumeId.Value) || string.IsNullOrWhiteSpace(addMountPath.Value)) return;
                var next = (volumeMountsList.Value ?? new List<(string, string)>()).ToList();
                next.Add((addVolumeId.Value, addMountPath.Value.Trim()));
                volumeMountsList.Set(next);
                addVolumeId.Set(string.Empty);
                addMountPath.Set(string.Empty);
                showAddVolumeDialog.Set(false);
            }
            var volForm = Layout.Vertical()
                | addVolumeId.ToSelectInput(volumeOptions)
                | addMountPath.ToTextInput().Placeholder("Mount path (e.g. /data)");
            addVolumeDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddVolumeDialog.Set(false),
                header: new DialogHeader("Add volume mount"),
                body: new DialogBody(volForm),
                footer: new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).OnClick(_ => SaveVolume()),
                    new Button("Cancel").OnClick(_ => showAddVolumeDialog.Set(false))
                )).Width(Size.Units(220));
        }

        if (!_isOpen.Value)
            return null;

        var sheetBody = new FooterLayout(footer, content);
        object sheetContent;
        if (addEnvDialog != null && addVolumeDialog != null)
            sheetContent = new Fragment(sheetBody, addEnvDialog, addVolumeDialog);
        else if (addEnvDialog != null)
            sheetContent = new Fragment(sheetBody, addEnvDialog);
        else if (addVolumeDialog != null)
            sheetContent = new Fragment(sheetBody, addVolumeDialog);
        else
            sheetContent = sheetBody;

        return new Sheet(
            _ => _isOpen.Set(false),
            sheetContent,
            title: "Create service",
            description: "Git repository or Docker image.")
            .Width(Size.Fraction(1 / 3f));
    }
}
