namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;
using Ivy.Helpers;

// ProjectsView: Blades, left = project list, right = details.

/// <summary>Projects view — Blades master-detail navigation.</summary>
public class ProjectsView : ViewBase
{
    private readonly string _apiToken;

    public ProjectsView(string apiToken) => _apiToken = apiToken;

    public override object? Build() =>
        this.UseBlades(() => new ProjectListBlade(_apiToken), "Projects");
}


/// <summary>Project list with service-count badges, add-project dialog.</summary>
public class ProjectListBlade : ViewBase
{
    private readonly string _apiToken;
    public ProjectListBlade(string apiToken) => _apiToken = apiToken;

    public override object? Build()
    {
        var client       = this.UseService<SliplaneApiClient>();
        var blades       = this.UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();

        var filter = this.UseState(string.Empty);
        var version = this.UseState(0);
        var overviewQuery = this.UseQuery<SliplaneOverview?, (string, int)>(
            key: ("projects-overview", version.Value),
            fetcher: async ct => await client.GetOverviewAsync(_apiToken),
            options: new QueryOptions
            {
                RefreshInterval   = TimeSpan.FromSeconds(15),
                RevalidateOnMount = true,
                KeepPrevious      = true
            });

        // Reload on child blade change
        this.UseEffect(() =>
        {
            if (refreshToken.IsRefreshed)
                version.Set(version.Value + 1);
        }, [refreshToken]);

        var showAddDialog  = this.UseState(false);
        var newProjectName = this.UseState(string.Empty);
        var addBusy        = this.UseState(false);
        var addError       = this.UseState<string?>(() => null);

        async Task CreateProjectAsync()
        {
            if (addBusy.Value) return;
            if (string.IsNullOrWhiteSpace(newProjectName.Value)) { addError.Set("Enter project name."); return; }
            addError.Set((string?)null);
            addBusy.Set(true);
            try
            {
                await client.CreateProjectAsync(_apiToken, newProjectName.Value.Trim());
                newProjectName.Set(string.Empty);
                showAddDialog.Set(false);
                version.Set(version.Value + 1);
            }
            catch (Exception ex) { addError.Set(ex.Message); }
            finally { addBusy.Set(false); }
        }

        var headerBar = Layout.Horizontal().Gap(1)
            | filter.ToSearchInput().Placeholder("Search projects…").Width(Size.Grow())
            | new Button().Icon(Icons.Plus).Ghost()
                .OnClick(_ =>
                {
                    newProjectName.Set(string.Empty);
                    addError.Set((string?)null);
                    showAddDialog.Set(true);
                });

        object listContent;
        if (overviewQuery.Loading && overviewQuery.Value == null)
        {
            listContent = Layout.Center() | Text.Muted("Loading projects…");
        }
        else if (overviewQuery.Error is { } qErr)
        {
            listContent = new Callout($"Error: {qErr.Message}", variant: CalloutVariant.Error);
        }
        else
        {
            var overview = overviewQuery.Value;
            var projects = overview?.Projects ?? new List<SliplaneProject>();
            var servers  = overview?.Servers  ?? new List<SliplaneServer>();
            var servicesByProject = overview?.ServicesByProject
                                    ?? new Dictionary<string, List<SliplaneService>>();

            if (!string.IsNullOrWhiteSpace(filter.Value))
            {
                var term = filter.Value.Trim();
                projects = projects
                    .Where(p => p.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            if (projects.Count == 0)
            {
                listContent = new Callout("No projects yet. Click \"Add project\" to create one.", variant: CalloutVariant.Info);
            }
            else
            {
                var items = projects.Select(p =>
                {
                    servicesByProject.TryGetValue(p.Id, out var svcList);
                    svcList ??= new List<SliplaneService>();
                    var count = svcList.Count;

                    string? serverLabel = null;
                    var firstServerId = svcList.FirstOrDefault()?.ServerId;
                    if (!string.IsNullOrWhiteSpace(firstServerId))
                    {
                        serverLabel = servers.FirstOrDefault(s => s.Id == firstServerId)?.Name
                                      ?? firstServerId;
                    }

                    var subtitle = $"{count} service" + (count == 1 ? string.Empty : "s");
                    var icon = count > 0 ? Icons.FolderOpen : Icons.Folder;

                    return new ListItem(
                        title:    p.Name,
                        subtitle: subtitle,
                        icon:     icon,
                        onClick:  _ => blades.Push(this, new ProjectDetailsBlade(_apiToken, p, refreshToken), p.Name, width: Size.Units(260)));
                });

                listContent = new List(items);
            }
        }

                Dialog? addDialog = null;
        if (showAddDialog.Value)
        {
            addDialog = new Dialog(
                onClose: (Event<Dialog> _) =>
                {
                    showAddDialog.Set(false);
                    addError.Set((string?)null);
                },
                header: new DialogHeader("Add project"),
                body: new DialogBody(
                    Layout.Vertical()
                    | newProjectName.ToTextInput().Placeholder("Project name")
                    | (addError.Value is { Length: > 0 } e
                        ? (object)new Callout(e, variant: CalloutVariant.Error)
                        : Layout.Vertical())),
                footer: new DialogFooter(
                    new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => showAddDialog.Set(false)),
                    new Button("Create").Icon(Icons.Plus).Primary().Loading(addBusy.Value)
                        .OnClick(async _ => await CreateProjectAsync()))
            ).Width(Size.Units(120));
        }

        return new Fragment()
               | new BladeHeader(headerBar)
               | listContent
               | addDialog;
    }
}
/// <summary>Project details: services DataTable, edit/create sheets, logs/events.</summary>
public class ProjectDetailsBlade : ViewBase
{
    private readonly string         _apiToken;
    private readonly SliplaneProject _project;
    private readonly RefreshToken   _parentRefreshToken;

    public ProjectDetailsBlade(string apiToken, SliplaneProject project, RefreshToken parentRefreshToken)
    {
        _apiToken           = apiToken;
        _project            = project;
        _parentRefreshToken = parentRefreshToken;
    }

    public override object? Build()
    {
        var client    = this.UseService<SliplaneApiClient>();
        var blades    = this.UseContext<IBladeService>();
        var (alertView, showAlert) = this.UseAlert();
        var refreshToken = this.UseRefreshToken();

                var overviewQuery = this.UseQuery<SliplaneOverview?, (string, string)>(
            key: ("proj-detail-overview", _project.Id),
            fetcher: async ct =>
            {
                var result = await client.GetOverviewAsync(_apiToken);
                // Refresh DataTable rows
                refreshToken.Refresh();
                return result;
            },
            options: new QueryOptions
            {
                RefreshInterval   = TimeSpan.FromSeconds(3),
                RevalidateOnMount = true,
                KeepPrevious      = true
            });

        var overview = overviewQuery.Value;
        List<SliplaneService>? rawServices = null;
        overview?.ServicesByProject.TryGetValue(_project.Id, out rawServices);
        var services        = rawServices ?? new List<SliplaneService>();
        var servers         = overview?.Servers ?? new List<SliplaneServer>();
        var eventsByService = overview?.EventsByService ?? new Dictionary<string, List<SliplaneServiceEvent>>();

        void Reload()
        {
            overviewQuery.Mutator.Revalidate();
            _parentRefreshToken.Refresh();
        }

                var createSheetOpen  = this.UseState(false);
        var editSheetOpen    = this.UseState(false);
        var selectedForEdit  = this.UseState<SliplaneService?>(() => null);
        var logsSheetOpen    = this.UseState(false);
        var logsSelection    = this.UseState<(string ServiceName, string ServiceId)?>(() => null);
        var eventsSheetOpen  = this.UseState(false);
        var eventsSelection  = this.UseState<(string ServiceName, List<SliplaneServiceEvent> Events)?>(() => null);
        var deleteDialogOpen = this.UseState(false);
        var deleteSelection  = this.UseState<SliplaneService?>(() => null);
        var deleteInput      = this.UseState(string.Empty);
        var deleteInputError = this.UseState<string?>(() => null);
        var deleteBusy       = this.UseState(false);
        var deleteProjectDialogOpen = this.UseState(false);
        var deleteProjectInput      = this.UseState(string.Empty);
        var deleteProjectInputError = this.UseState<string?>(() => null);

        object? createSheet = createSheetOpen.Value
            ? new CreateServiceSheet(createSheetOpen, _apiToken, [], _project.Id)
            : null;

        var selectionState = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
        this.UseEffect(() =>
        {
            if (selectedForEdit.Value is { } svc)
                selectionState.Set((_project.Id, _project.Name ?? string.Empty, svc));
            else
                selectionState.Set(((string, string, SliplaneService)?)null);
        }, [selectedForEdit]);
        this.UseEffect(() =>
        {
            if (!editSheetOpen.Value)
            {
                selectionState.Set(((string, string, SliplaneService)?)null);
                selectedForEdit.Set((SliplaneService?)null);
            }
        }, [editSheetOpen]);

        var signalReceiver = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        this.UseEffect(() => signalReceiver.Receive(_ =>
        {
            Reload();
            return new Unit();
        }));

        object? editSheet = editSheetOpen.Value && selectedForEdit.Value is { } svcEdit
            ? new EditServiceSheet(
                editSheetOpen, _apiToken, _project.Id, _project.Name ?? string.Empty, svcEdit,
                selectionState, servers)
            : null;

        object? logsSheet = logsSelection.Value is { } logsSel && logsSheetOpen.Value
            ? new ServiceLogsSheet(logsSheetOpen, _apiToken, _project.Id, logsSel.ServiceId, logsSel.ServiceName)
            : null;

        object? eventsSheet = eventsSelection.Value is { } evtSel && eventsSheetOpen.Value
            ? new ServiceEventsSheet(eventsSheetOpen, evtSel.ServiceName, evtSel.Events)
            : null;

                async Task ConfirmDeleteProjectAsync()
        {
            if (!string.Equals(deleteProjectInput.Value?.Trim(), _project.Name, StringComparison.Ordinal))
            {
                deleteProjectInputError.Set("Project name does not match. Please type it exactly.");
                return;
            }
            deleteProjectInputError.Set((string?)null);
            deleteBusy.Set(true);
            try
            {
                await client.DeleteProjectAsync(_apiToken, _project.Id);
                deleteProjectDialogOpen.Set(false);
                deleteProjectInput.Set(string.Empty);
                _parentRefreshToken.Refresh();
                blades.Pop(this);
            }
            finally { deleteBusy.Set(false); }
        }

        void CloseDeleteProjectDialog()
        {
            deleteProjectDialogOpen.Set(false);
            deleteProjectInput.Set(string.Empty);
            deleteProjectInputError.Set((string?)null);
        }

                bool IsPaused(string? s) =>
            string.Equals(s, "paused", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, "suspended", StringComparison.OrdinalIgnoreCase);

        async Task PauseAsync(SliplaneService svc)
        {
            if (IsPaused(svc.Status)) { showAlert($"Service \"{svc.Name}\" is already paused.", async _ => { }, "Pause"); return; }
            await client.PauseServiceAsync(_apiToken, _project.Id, svc.Id);
            Reload();
        }

        async Task ResumeAsync(SliplaneService svc)
        {
            if (!IsPaused(svc.Status)) { showAlert($"Service \"{svc.Name}\" is already running.", async _ => { }, "Resume"); return; }
            await client.UnpauseServiceAsync(_apiToken, _project.Id, svc.Id);
            Reload();
        }

                var headerBar = Layout.Horizontal()
            | new Button("Add service").Icon(Icons.Plus).Secondary()
                .OnClick(_ => createSheetOpen.Set(true))
            | new Button("Delete project")
                .Icon(Icons.Trash2)
                .Variant(ButtonVariant.Destructive)
                .OnClick(_ =>
                {
                    deleteProjectInput.Set(string.Empty);
                    deleteProjectInputError.Set((string?)null);
                    deleteProjectDialogOpen.Set(true);
                });

                var rows = BuildServiceRows(services, servers, eventsByService);

                object mainContent;
        if (overviewQuery.Loading && overview == null)
        {
            mainContent = Layout.Center() | Text.Muted("Loading services…");
        }
        else if (overviewQuery.Error is { } svcErr)
        {
            mainContent = new Callout($"Error: {svcErr.Message}", variant: CalloutVariant.Error);
        }
        else if (rows.Length == 0)
        {
            mainContent = new Callout("No services yet. Click \"Add service\" to create one.", variant: CalloutVariant.Info);
        }
        else
        {
            mainContent = rows
                .AsQueryable()
                .ToDataTable(r => r.ServiceId)
                .RefreshToken(refreshToken)
                .Height(Size.Full())
                .Hidden(r => r.ServiceId)
                .Header(r => r.Name,        "Service")
                .Header(r => r.Server,       "Server")
                .Header(r => r.StatusIcon,   "Icon")
                .Header(r => r.Status,       "Status")
                .Header(r => r.DeployStatus, "Deploy log")
                .Header(r => r.Url,          "URL")
                .Width(r => r.StatusIcon,   Size.Px(50))
                .Width(r => r.Status,       Size.Px(120))
                .Width(r => r.DeployStatus, Size.Px(200))
                .Renderer(r => r.Url, new LinkDisplayRenderer { Type = LinkDisplayType.Url })
                .Config(c =>
                {
                    c.AllowSorting    = true;
                    c.AllowFiltering  = true;
                    c.ShowSearch      = true;
                    c.SelectionMode   = SelectionModes.Rows;
                    c.ShowIndexColumn = false;
                })
                .RowActions(
                    MenuItem.Default(Icons.Pencil, "edit").Tag("edit"),
                    MenuItem.Default(Icons.Trash2, "delete").Tag("delete"),
                    MenuItem.Default(Icons.EllipsisVertical, "more")
                        .Children([
                            MenuItem.Default(Icons.Pause,    "pause").Label("Pause").Tag("pause"),
                            MenuItem.Default(Icons.Play,     "resume").Label("Resume").Tag("resume"),
                            MenuItem.Default(Icons.FileText, "logs").Label("Logs").Tag("logs"),
                            MenuItem.Default(Icons.Calendar, "events").Label("Events").Tag("events")
                        ]))
                .OnRowAction(e =>
                {
                    var args = e.Value;
                    if (args is null) return ValueTask.CompletedTask;
                    var tag = args.Tag?.ToString();
                    var id  = args.Id?.ToString() ?? string.Empty;
                    var svc = services.FirstOrDefault(s => s.Id == id);
                    if (svc == null) return ValueTask.CompletedTask;

                    if (tag == "edit")
                    {
                        selectedForEdit.Set(svc);
                        editSheetOpen.Set(true);
                    }
                    else if (tag == "delete")
                    {
                        deleteSelection.Set(svc);
                        deleteInput.Set(string.Empty);
                        deleteInputError.Set((string?)null);
                        deleteDialogOpen.Set(true);
                    }
                    else if (tag == "pause")
                    {
                        _ = PauseAsync(svc);
                    }
                    else if (tag == "resume")
                    {
                        _ = ResumeAsync(svc);
                    }
                    else if (tag == "logs")
                    {
                        logsSelection.Set((svc.Name ?? svc.Id, svc.Id));
                        logsSheetOpen.Set(true);
                    }
                    else if (tag == "events")
                    {
                        var evts = eventsByService.TryGetValue(svc.Id, out var ev) ? ev : new List<SliplaneServiceEvent>();
                        eventsSelection.Set((svc.Name ?? svc.Id, evts));
                        eventsSheetOpen.Set(true);
                    }

                    return ValueTask.CompletedTask;
                });
        }

                Dialog? deleteDialog = null;
        if (deleteDialogOpen.Value && deleteSelection.Value is { } del)
        {
            async Task ConfirmDeleteAsync()
            {
                if (!string.Equals(deleteInput.Value?.Trim(), del.Name, StringComparison.Ordinal))
                {
                    deleteInputError.Set("Service name does not match. Please type it exactly.");
                    return;
                }
                deleteInputError.Set((string?)null);
                await client.DeleteServiceAsync(_apiToken, _project.Id, del.Id);
                deleteDialogOpen.Set(false);
                deleteSelection.Set((SliplaneService?)null);
                Reload();
            }

            void CloseDeleteDialog()
            {
                deleteDialogOpen.Set(false);
                deleteSelection.Set((SliplaneService?)null);
                deleteInput.Set(string.Empty);
                deleteInputError.Set((string?)null);
            }

            deleteDialog = new Dialog(
                onClose: (Event<Dialog> _) => CloseDeleteDialog(),
                header: new DialogHeader($"Delete service \"{del.Name}\"?"),
                body: new DialogBody(
                    Layout.Vertical()
                    | Text.Markdown($"Type the service name to confirm: **`{del.Name}`**")
                    | deleteInput.ToTextInput().Placeholder("Service name")
                    | (deleteInputError.Value is { Length: > 0 } errMsg
                        ? (object)new Callout(errMsg, variant: CalloutVariant.Error)
                        : Layout.Vertical())),
                footer: new DialogFooter(
                    new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => CloseDeleteDialog()),
                    new Button("Delete").Destructive().Icon(Icons.Trash2)
                        .OnClick(async _ => await ConfirmDeleteAsync()))
            );
        }

                Dialog? deleteProjectDialog = null;
        if (deleteProjectDialogOpen.Value)
        {
            deleteProjectDialog = new Dialog(
                onClose: (Event<Dialog> _) => CloseDeleteProjectDialog(),
                header: new DialogHeader($"Delete project \"{_project.Name}\"?"),
                body: new DialogBody(
                    Layout.Vertical()
                    | Text.Markdown($"Type the project name to confirm: **`{_project.Name}`**")
                    | deleteProjectInput.ToTextInput().Placeholder("Project name")
                    | (deleteProjectInputError.Value is { Length: > 0 } errMsg
                        ? (object)new Callout(errMsg, variant: CalloutVariant.Error)
                        : Layout.Vertical())),
                footer: new DialogFooter(
                    new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => CloseDeleteProjectDialog()),
                    new Button("Delete").Destructive().Icon(Icons.Trash2).Loading(deleteBusy.Value)
                        .OnClick(async _ => await ConfirmDeleteProjectAsync()))
            );
        }

        return new Fragment(
            new BladeHeader(headerBar),
            mainContent,
            createSheet,
            editSheet,
            logsSheet,
            eventsSheet,
            alertView,
            deleteDialog,
            deleteProjectDialog);
    }

        private sealed record ServiceRow(
        string ServiceId,
        string Name,
        string Server,
        string Status,
        Icons  StatusIcon,
        string DeployStatus,
        string Url);

        private static ServiceRow[] BuildServiceRows(
        List<SliplaneService> services,
        List<SliplaneServer> servers,
        Dictionary<string, List<SliplaneServiceEvent>> eventsByService)
    {
        return services.Select(svc =>
        {
            var serverLabel = string.IsNullOrWhiteSpace(svc.ServerId) ? "—"
                : servers.FirstOrDefault(s => s.Id == svc.ServerId)?.Name ?? svc.ServerId!;

            var events = eventsByService.TryGetValue(svc.Id, out var ev) ? ev : new List<SliplaneServiceEvent>();
            var (statusLabel, statusIcon, _) = ServicesView.GetServiceStatus(svc, events);

            var lastUpdatedInstant = svc.UpdatedAt ?? svc.CreatedAt;

            string deployStatus = "—";
            if (events.Count > 0)
            {
                deployStatus = string.Join("\n\n",
                    events.OrderByDescending(e => e.CreatedAt).Take(10)
                        .Select(e =>
                        {
                            var label = string.IsNullOrWhiteSpace(e.Message)
                                ? FormatEventType(e.Type)
                                : e.Message;
                            var dateStr = e.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy, HH:mm:ss");
                            return $"{label}\n{dateStr}";
                        }));
            }

            var rawDomain = svc.Network?.CustomDomains?.FirstOrDefault()?.Domain
                            ?? svc.Network?.ManagedDomain ?? string.Empty;
            var url = string.IsNullOrWhiteSpace(rawDomain) ? string.Empty
                : (rawDomain.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? rawDomain : "https://" + rawDomain);

            return new ServiceRow(
                ServiceId:    svc.Id,
                Name:         svc.Name,
                Server:       serverLabel,
                Status:       statusLabel,
                StatusIcon:   statusIcon,
                DeployStatus: deployStatus,
                Url:          url);
        }).ToArray();
    }

    private static string FormatEventType(string? type) => type switch
    {
        "service_resume_success"  => "Service resumed",
        "service_resume"          => "Resume requested",
        "service_suspend_success" => "Service suspended",
        "service_suspend"         => "Suspend requested",
        "service_deploy_success"  => "Deployed successfully",
        "service_deploy"          => "Deploy started",
        "service_deploy_failed"   => "Deploy failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };
}


/// <summary>Service logs in CodeBlock, polls every 5s.</summary>
public class ServiceLogsBlade : ViewBase
{
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _serviceId;
    private readonly string _serviceName;

    public ServiceLogsBlade(string apiToken, string projectId, string serviceId, string serviceName)
    {
        _apiToken    = apiToken;
        _projectId   = projectId;
        _serviceId   = serviceId;
        _serviceName = serviceName;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();

        var logsQuery = this.UseQuery<List<SliplaneServiceLog>, string>(
            key: $"logs:{_serviceId}",
            fetcher: async ct =>
                await client.GetServiceLogsAsync(_apiToken, _projectId, _serviceId)
                ?? new List<SliplaneServiceLog>(),
            options: new QueryOptions
            {
                RefreshInterval   = TimeSpan.FromSeconds(5),
                RevalidateOnMount = true
            });

        string logsText;
        if (logsQuery.Loading && logsQuery.Value == null)
        {
            logsText = "Loading logs…";
        }
        else if (logsQuery.Error is { } err)
        {
            logsText = $"Error: {err.Message}";
        }
        else
        {
            var logs = logsQuery.Value ?? new List<SliplaneServiceLog>();
            logsText = logs.Count == 0
                ? "No logs available."
                : string.Join("\n", logs
                    .OrderBy(l => l.Timestamp)
                    .Select(l => $"[{l.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}] {l.Line}"));
        }

        return Layout.Vertical().Height(Size.Full())
            | new CodeBlock(logsText)
                .Language(Languages.Text)
                .ShowCopyButton()
                .Width(Size.Full())
                .Height(Size.Full());
    }
}


/// <summary>Service events in CodeBlock.</summary>
public class ServiceEventsBlade : ViewBase
{
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _serviceId;
    private readonly string _serviceName;

    public ServiceEventsBlade(string apiToken, string projectId, string serviceId, string serviceName)
    {
        _apiToken    = apiToken;
        _projectId   = projectId;
        _serviceId   = serviceId;
        _serviceName = serviceName;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();

        var eventsQuery = this.UseQuery<List<SliplaneServiceEvent>, string>(
            key: $"events:{_serviceId}",
            fetcher: async ct =>
                await client.GetServiceEventsAsync(_apiToken, _projectId, _serviceId)
                ?? new List<SliplaneServiceEvent>(),
            options: new QueryOptions
            {
                RefreshInterval   = TimeSpan.FromSeconds(10),
                RevalidateOnMount = true
            });

        string eventsText;
        if (eventsQuery.Loading && eventsQuery.Value == null)
        {
            eventsText = "Loading events…";
        }
        else if (eventsQuery.Error is { } err)
        {
            eventsText = $"Error loading events: {err.Message}";
        }
        else
        {
            var events = eventsQuery.Value ?? new List<SliplaneServiceEvent>();
            eventsText = events.Count == 0
                ? "No events recorded for this service."
                : string.Join("\n\n", events
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e =>
                    {
                        var date  = e.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                        var label = FormatEventType(e.Type);
                        var msg   = string.IsNullOrWhiteSpace(e.Message)
                            ? string.Empty
                            : $"\n  Message: {e.Message}";
                        return $"[{date}]  {label}{msg}";
                    }));
        }

        return Layout.Vertical().Height(Size.Full())
            | new CodeBlock(eventsText)
                .Language(Languages.Text)
                .ShowCopyButton()
                .Width(Size.Full())
                .Height(Size.Full());
    }

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
}

// ServiceRequestFactory

internal static class ServiceRequestFactory
{
    public static UpdateServiceRequest BuildUpdateRequest(
        string name,
        string deployUrl,
        string branch,
        string dockerfilePath,
        string dockerContext,
        bool autoDeploy,
        string? cmd,
        string? healthcheck,
        IReadOnlyCollection<EnvironmentVariable>? env)
    {
        return new UpdateServiceRequest(
            Name: name.Trim(),
            Cmd: string.IsNullOrWhiteSpace(cmd) ? null : cmd.Trim(),
            Healthcheck: string.IsNullOrWhiteSpace(healthcheck) ? null : healthcheck.Trim(),
            Deployment: new UpdateServiceDeployment(
                Url: deployUrl.Trim(),
                Branch: string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim(),
                AutoDeploy: autoDeploy,
                DockerfilePath: string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim(),
                DockerContext: string.IsNullOrWhiteSpace(dockerContext) ? "." : dockerContext.Trim()
            ),
            Env: env is { Count: > 0 } ? env.ToList() : null
        );
    }

    public static CreateServiceRequest BuildCreateRequest(
        string name,
        string serverId,
        string gitRepo,
        string branch,
        string dockerfilePath,
        string dockerContext,
        bool autoDeploy,
        bool networkPublic,
        string networkProtocol,
        string? cmd,
        string? healthcheck,
        IReadOnlyCollection<EnvironmentVariable>? env,
        IReadOnlyCollection<(string VolumeId, string MountPath)>? volumeMounts)
    {
        var envList = env is { Count: > 0 } ? env.ToList() : null;
        var volumes = volumeMounts is { Count: > 0 }
            ? volumeMounts.Select(v => new ServiceVolumeMount(v.VolumeId, v.MountPath)).ToList()
            : null;

        return new CreateServiceRequest(
            Name: name.Trim(),
            ServerId: serverId,
            Network: new ServiceNetworkRequest(Public: networkPublic, Protocol: networkProtocol),
            Deployment: new RepositoryDeployment(
                Url: gitRepo.Trim(),
                Branch: string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim(),
                AutoDeploy: autoDeploy,
                DockerfilePath: string.IsNullOrWhiteSpace(dockerfilePath) ? "Dockerfile" : dockerfilePath.Trim(),
                DockerContext: string.IsNullOrWhiteSpace(dockerContext) ? "." : dockerContext.Trim()
            ),
            Cmd: string.IsNullOrWhiteSpace(cmd) ? null : cmd.Trim(),
            Healthcheck: string.IsNullOrWhiteSpace(healthcheck) ? null : healthcheck.Trim(),
            Env: envList,
            Volumes: volumes
        );
    }
}
