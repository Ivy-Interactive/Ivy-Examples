namespace SliplaneManage.Apps.Views;

using System.Text.Json;
using SliplaneManage.Models;
using SliplaneManage.Services;

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
        var client = this.UseService<SliplaneApiClient>();
        var refreshToken = this.UseRefreshToken();
        var serviceDetailOpen = this.UseState(false);
        var serviceDetailSelection = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
        var (createSheetView, openCreateSheet) = this.UseTrigger(
            (IState<bool> isOpen) => new CreateServiceSheet(isOpen, _apiToken, _projects));
        var (alertView, showAlert) = this.UseAlert();
        var deleteDialogOpen = this.UseState(false);
        var deleteSelection = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
        var deleteCommandInput = this.UseState(string.Empty);
        var deleteCommandError = this.UseState<string?>(() => (string?)null);
        // Services currently transitioning (pause/resume) — show pending optimistically.
        var transitioningServiceIds = this.UseState<HashSet<string>>(() => new HashSet<string>());
        // ── Logs / Events sheets ───────────────────────────────────────────────
        var logsSheetOpen = this.UseState(false);
        var logsSelection = this.UseState<(string ProjectId, string ServiceName, string ServiceId)?>(() => null);
        var eventsSheetOpen = this.UseState(false);
        var eventsSelection = this.UseState<(string ServiceName, List<SliplaneServiceEvent> Events)?>(() => null);
        var eventsStore = this.UseState<Dictionary<string, List<SliplaneServiceEvent>>>(() =>
            new Dictionary<string, List<SliplaneServiceEvent>>());

        var overviewQuery = this.UseQuery<SliplaneOverview?, string>(
            key: $"overview:{_apiToken}",
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

        // ── React to SliplaneRefreshSignal (sent by Create/Edit sheets) ──────────
        var signalReceiver = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        this.UseEffect(() => signalReceiver.Receive(_ =>
        {
            overviewQuery.Mutator.Revalidate();
            return new Unit();
        }));
        this.UseEffect(() =>
        {
            var ids = transitioningServiceIds.Value;
            if (ids.Count == 0) return;
            var evtsBySvc = eventsStore.Value ?? new Dictionary<string, List<SliplaneServiceEvent>>();
            var toRemove = new List<string>();
            foreach (var sid in ids)
            {
                if (!evtsBySvc.TryGetValue(sid, out var evts) || evts.Count == 0) continue;
                var last = evts.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
                if (last?.Type is "service_suspend_success" or "service_resume_success")
                    toRemove.Add(sid);
            }
            if (toRemove.Count > 0)
            {
                var next = new HashSet<string>(ids);
                foreach (var id in toRemove) next.Remove(id);
                transitioningServiceIds.Set(next);
            }
        }, EffectTrigger.OnBuild());

        // Current data for the table derived directly from overviewQuery
        var overview = overviewQuery.Value;
        if (overview?.EventsByService is { Count: > 0 } latestEvents)
        {
            var current = eventsStore.Value ?? new Dictionary<string, List<SliplaneServiceEvent>>();
            foreach (var kvp in latestEvents)
            {
                var serviceId = kvp.Key;
                var freshList = kvp.Value ?? new List<SliplaneServiceEvent>();
                if (!current.TryGetValue(serviceId, out var existing) || existing is null || existing.Count == 0)
                {
                    current[serviceId] = freshList
                        .OrderBy(e => e.CreatedAt)
                        .ToList();
                    continue;
                }

                var merged = existing
                    .Concat(freshList)
                    .GroupBy(e => new { e.CreatedAt, e.Type, e.Message })
                    .Select(g => g.First())
                    .OrderBy(e => e.CreatedAt)
                    .ToList();

                // Keep a reasonable history per service
                current[serviceId] = merged.TakeLast(200).ToList();
            }

            eventsStore.Set(current);
        }

        var currentServices = overview == null
            ? new List<(string ProjectId, string ProjectName, SliplaneService Service)>()
            : overview.ServicesByProject
                .SelectMany(kv =>
                {
                    var projectName = overview.Projects.FirstOrDefault(p => p.Id == kv.Key)?.Name ?? kv.Key;
                    return kv.Value.Select(svc => (ProjectId: kv.Key, ProjectName: projectName, Service: svc));
                })
                .ToList();
        var currentServers = overview?.Servers ?? new List<SliplaneServer>();
        var eventsByService = eventsStore.Value ?? new Dictionary<string, List<SliplaneServiceEvent>>();

        // ── build rows (pure – no hooks) ──────────────────────────────────────
        var rows = BuildServiceRows(currentServices, currentServers, eventsByService, transitioningServiceIds.Value);

        // ── UI ────────────────────────────────────────────────────────────────
        void ShowServiceSheet(string projectId, string projectName, SliplaneService svc)
        {
            serviceDetailSelection.Set((projectId, projectName, svc));
            serviceDetailOpen.Set(true);
        }

        async Task PauseServiceAsync(string projectId, SliplaneService svc)
        {
            bool IsPausedStatus(string? s) =>
                string.Equals(s, "paused", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "suspended", StringComparison.OrdinalIgnoreCase);

            if (IsPausedStatus(svc.Status))
            {
                showAlert(
                    $"Service \"{svc.Name}\" is already paused.",
                    async _ => { await Task.CompletedTask; },
                    "Pause service");
                return;
            }

            transitioningServiceIds.Set(new HashSet<string>(transitioningServiceIds.Value) { svc.Id });
            await client.PauseServiceAsync(_apiToken, projectId, svc.Id);
            overviewQuery.Mutator.Revalidate();
        }

        async Task ResumeServiceAsync(string projectId, SliplaneService svc)
        {
            bool IsPausedStatus(string? s) =>
                string.Equals(s, "paused", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "suspended", StringComparison.OrdinalIgnoreCase);

            if (!IsPausedStatus(svc.Status))
            {
                showAlert(
                    $"Service \"{svc.Name}\" is already running.",
                    async _ => { await Task.CompletedTask; },
                    "Resume service");
                return;
            }

            transitioningServiceIds.Set(new HashSet<string>(transitioningServiceIds.Value) { svc.Id });
            await client.UnpauseServiceAsync(_apiToken, projectId, svc.Id);
            overviewQuery.Mutator.Revalidate();
        }

        var selectedForEdit = serviceDetailSelection.Value;
        object? editSheetView = selectedForEdit is { } sel && serviceDetailOpen.Value
            ? new EditServiceSheet(serviceDetailOpen, _apiToken, sel.ProjectId, sel.ProjectName, sel.Service, serviceDetailSelection, currentServers)
            : null;

        // ── Logs sheet ────────────────────────────────────────────────────────────
        object? logsSheetView = logsSelection.Value is { } logsSel && logsSheetOpen.Value
            ? new ServiceLogsSheet(logsSheetOpen, _apiToken, logsSel.ProjectId, logsSel.ServiceId, logsSel.ServiceName)
            : null;

        // ── Events sheet ──────────────────────────────────────────────────────────
        object? eventsSheetView = eventsSelection.Value is { } evtSel && eventsSheetOpen.Value
            ? new ServiceEventsSheet(eventsSheetOpen, evtSel.ServiceName, evtSel.Events)
            : null;

        // Use overviewQuery directly for loading/error state to avoid stale local flags.
        if (overviewQuery.Loading && overviewQuery.Value == null && currentServices.Count == 0)
            return Layout.Center() | Text.Muted("Loading all services...");

        if (overviewQuery.Error is { } errEx)
            return new Callout($"Error: {errEx.Message}", variant: CalloutVariant.Error);

        var headerRow = Layout.Horizontal().Height(Size.Fit()) | Text.H2("Services");
        var addServiceBtn = new Button("Add service").Icon(Icons.Plus).OnClick(_ => openCreateSheet()).Large().Secondary().BorderRadius(BorderRadius.Full);
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
                config.ShowGroups = true;
                config.ShowIndexColumn = false;
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
                config.SelectionMode = SelectionModes.Rows;
            })
            .RowActions(
                MenuItem.Default(Icons.Pencil, "edit").Tag("edit"),
                MenuItem.Default(Icons.Trash2, "delete").Tag("delete"),
                MenuItem.Default(Icons.EllipsisVertical, "more")
                    .Children([
                        MenuItem.Default(Icons.Pause, "pause").Label("Pause").Tag("pause"),
                    MenuItem.Default(Icons.Play, "resume").Label("Resume").Tag("resume"),
                    MenuItem.Default(Icons.FileText, "logs").Label("Logs").Tag("logs"),
                    MenuItem.Default(Icons.Calendar, "events").Label("Events").Tag("events")
                    ]))
            .OnRowAction(e =>
            {
                var args = e.Value;
                if (args is null) return ValueTask.CompletedTask;
                var tag = args.Tag?.ToString();
                var id = args.Id?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) return ValueTask.CompletedTask;

                var match = currentServices.FirstOrDefault(cs => cs.Service.Id == id);
                if (match.Service == null || string.IsNullOrWhiteSpace(match.ProjectId)) return ValueTask.CompletedTask;

                if (tag == "edit")
                {
                    ShowServiceSheet(match.ProjectId, match.ProjectName, match.Service);
                    return ValueTask.CompletedTask;
                }

                if (tag == "delete")
                {
                    deleteSelection.Set((match.ProjectId, match.ProjectName, match.Service));
                    deleteCommandInput.Set(string.Empty);
                    deleteCommandError.Set((string?)null);
                    deleteDialogOpen.Set(true);
                    return ValueTask.CompletedTask;
                }

                if (tag == "pause")
                {
                    _ = PauseServiceAsync(match.ProjectId, match.Service);
                    return ValueTask.CompletedTask;
                }

                if (tag == "resume")
                {
                    _ = ResumeServiceAsync(match.ProjectId, match.Service);
                    return ValueTask.CompletedTask;
                }

                if (tag == "logs")
                {
                    logsSelection.Set((match.ProjectId, match.Service.Name ?? match.Service.Id, match.Service.Id));
                    logsSheetOpen.Set(true);
                    return ValueTask.CompletedTask;
                }

                if (tag == "events")
                {
                    var evts = eventsByService.TryGetValue(match.Service.Id, out var ev) ? ev : new List<SliplaneServiceEvent>();
                    eventsSelection.Set((match.Service.Name ?? match.Service.Id, evts));
                    eventsSheetOpen.Set(true);
                    return ValueTask.CompletedTask;
                }

                return ValueTask.CompletedTask;
            })
            .Renderer(e => e.Url, new LinkDisplayRenderer { Type = LinkDisplayType.Url });

        Dialog? deleteDialog = null;
        if (deleteDialogOpen.Value && deleteSelection.Value is { } del)
        {
            async Task ConfirmDeleteAsync()
            {
                var expectedName = del.Service.Name ?? string.Empty;
                if (!string.Equals(deleteCommandInput.Value?.Trim(), expectedName, StringComparison.Ordinal))
                {
                    deleteCommandError.Set("Service name does not match. Please type it exactly.");
                    return;
                }

                deleteCommandError.Set((string?)null);
                await client.DeleteServiceAsync(_apiToken, del.ProjectId, del.Service.Id);
                CloseDeleteDialog();
                overviewQuery.Mutator.Revalidate();
            }

            void CloseDeleteDialog()
            {
                deleteDialogOpen.Set(false);
                deleteSelection.Set(((string ProjectId, string ProjectName, SliplaneService Service)?)null);
                deleteCommandInput.Set(string.Empty);
                deleteCommandError.Set((string?)null);
            }

            var deleteBody = Layout.Vertical()
                | Text.Markdown($"If you want to delete this service, type the following service name: **`{del.Service.Name}`**")
                | deleteCommandInput.ToTextInput().Placeholder("Enter service name here")
                | (deleteCommandError.Value is { Length: > 0 } errMsg
                    ? (object)new Callout(errMsg, variant: CalloutVariant.Error)
                    : Layout.Vertical());

            var deleteFooter = new DialogFooter(
                new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => CloseDeleteDialog()),
                new Button("Delete")
                    .Destructive()
                    .Icon(Icons.Trash2)
                    .OnClick(async _ => await ConfirmDeleteAsync()));

            deleteDialog = new Dialog(
                onClose: (Event<Dialog> _) => CloseDeleteDialog(),
                header: new DialogHeader("Are you sure you want to delete this service?"),
                body: new DialogBody(deleteBody),
                footer: deleteFooter);
        }

        object content = currentServices.Count == 0
            ? (object)(Layout.Vertical() | headerRow | new Callout("No services found.", variant: CalloutVariant.Info) | table)
            : Layout.Vertical().Height(Size.Full()) | headerRow | table;

        return new Fragment(
            content,
            addServiceFloat,
            editSheetView,
            createSheetView,
            logsSheetView,
            eventsSheetView,
            alertView,
            deleteDialog);
    }

    // ── pure data types ────────────────────────────────────────────────────────

    private sealed record ServiceRow(
        string ServiceId,
        string Name,
        string Project,
        string Server,
        string Status,
        Icons StatusIcon,
        string LastUpdated,
        string DeployStatus,
        string Url);

    // ── static helpers (no hooks) ──────────────────────────────────────────────

    internal static (string Label, Icons Icon, string? EventType) GetServiceStatus(
        SliplaneService svc, List<SliplaneServiceEvent> events, HashSet<string>? transitioningIds = null)
    {
        if (transitioningIds?.Contains(svc.Id) == true)
            return ("pending", Icons.Clock, null);

        var rawStatus = svc.Status ?? string.Empty;

        // Only show error if the most recent deploy-related event is a failure.
        // A later service_deploy_success means the service recovered.
        var lastDeployEvent = events
            .Where(e => e.Type is "service_deploy_failed" or "service_build_failed" or "service_deploy_success" or "service_deploy")
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();
        if (lastDeployEvent?.Type is "service_deploy_failed" or "service_build_failed")
            return ("error", Icons.CircleX, lastDeployEvent.Type);

        var ev = events.OrderByDescending(e => e.CreatedAt).FirstOrDefault(e =>
            e.Type is "service_suspend" or "service_suspend_success"
                   or "service_resume" or "service_resume_success");

        if (ev != null)
        {
            var msg = ev.Message ?? string.Empty;
            return ev.Type switch
            {
                "service_suspend" => ("pending", Icons.Clock, ev.Type),
                "service_resume" => ("pending", Icons.Clock, ev.Type),
                "service_suspend_success" => ("suspended", Icons.Pause, ev.Type),
                "service_resume_success" => ("live", Icons.Play, ev.Type),
                _ => (rawStatus, Icons.MonitorStop, ev.Type)
            };
        }

        return rawStatus.ToLowerInvariant() switch
        {
            "live" => ("live", Icons.Play, null),
            "suspended" or "paused" => ("suspended", Icons.Pause, null),
            "error" or "failed" => ("error", Icons.CircleX, null),
            "pending" => ("pending", Icons.Clock, null),
            _ => (string.IsNullOrWhiteSpace(rawStatus) ? "—" : rawStatus, Icons.MonitorStop, null)
        };
    }

    /// <summary>
    /// Converts a raw Sliplane event type string to a human-readable log label.
    /// </summary>
    private static string FormatEventType(string? type) => type switch
    {
        "service_resume_success" => "Service resumed successfully",
        "service_resume" => "Service resume requested",
        "service_suspend_success" => "Service suspended successfully",
        "service_suspend" => "Service suspension requested",
        "service_deploy_success" => "Service deployed successfully",
        "service_deploy" => "Service deploy started",
        "service_deploy_failed" => "Service deploy failed",
        "service_build_failed" => "Build failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };

    /// <summary>
    /// Pure function – builds ServiceRow[] from already-loaded data.
    /// Must NOT call any Ivy hooks (UseQuery, UseState, UseEffect, …).
    /// </summary>
    private static ServiceRow[] BuildServiceRows(
        List<(string ProjectId, string ProjectName, SliplaneService Service)> currentServices,
        List<SliplaneServer> serverList,
        Dictionary<string, List<SliplaneServiceEvent>> eventsByService,
        HashSet<string>? transitioningIds = null)
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

                var (statusLabel, statusIcon, _) = GetServiceStatus(svc, events, transitioningIds);

                var lastUpdatedInstant = svc.UpdatedAt ?? svc.CreatedAt;

                string deployStatus = "—";
                if (events.Count > 0)
                {
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
                        ServiceId: svc.Id,
                        Name: svc.Name,
                        Project: projectName,
                        Server: serverLabel,
                        Status: statusLabel,
                        StatusIcon: statusIcon,
                        LastUpdated: lastUpdatedInstant.ToString("yyyy-MM-dd HH:mm"),
                        DeployStatus: deployStatus,
                        Url: siteUrlAbsolute)
                };
            })
            .OrderByDescending(x => x.SortKey)
            .Select(x => x.Row)
            .ToArray();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ServiceLogsSheet — shows service logs in a CodeBlock inside a Sheet.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Fetches and displays service logs in a Sheet using a CodeBlock widget.
/// Polls every 5 seconds for fresh data.
/// </summary>
public class ServiceLogsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _serviceId;
    private readonly string _serviceName;

    public ServiceLogsSheet(
        IState<bool> isOpen,
        string apiToken,
        string projectId,
        string serviceId,
        string serviceName)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projectId = projectId;
        _serviceId = serviceId;
        _serviceName = serviceName;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var logsQuery = this.UseQuery<List<SliplaneServiceLog>, string>(
            key: $"logs:{_serviceId}",
            fetcher: async ct =>
            {
                var logs = await client.GetServiceLogsAsync(_apiToken, _projectId, _serviceId);
                return logs ?? new List<SliplaneServiceLog>();
            },
            options: new QueryOptions
            {
                RefreshInterval = TimeSpan.FromSeconds(5),
                RevalidateOnMount = true
            });

        if (!_isOpen.Value) return null;

        string logsText;
        if (logsQuery.Loading && logsQuery.Value == null)
        {
            logsText = "Loading logs…";
        }
        else if (logsQuery.Error is { } err)
        {
            logsText = $"Error loading logs: {err.Message}";
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

        var codeBlock = new CodeBlock(logsText)
            .Language(Languages.Text)
            .ShowCopyButton()
            .Width(Size.Full())
            .Height(Size.Full());

        var body = Layout.Vertical().Height(Size.Full()) | codeBlock;

        return new Sheet(
            _ => { _isOpen.Set(false); },
            body,
            title: $"Logs — {_serviceName}",
            description: "Live logs (refreshes every 5 s)")
            .Width(Size.Fraction(1 / 2f));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ServiceEventsSheet — shows service events in a CodeBlock inside a Sheet.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Displays the service events (already loaded from the overview cache) in a
/// Sheet using a CodeBlock widget for easy reading and copy.
/// </summary>
public class ServiceEventsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _serviceName;
    private readonly List<SliplaneServiceEvent> _events;

    public ServiceEventsSheet(
        IState<bool> isOpen,
        string serviceName,
        List<SliplaneServiceEvent> events)
    {
        _isOpen = isOpen;
        _serviceName = serviceName;
        _events = events;
    }

    public override object? Build()
    {
        if (!_isOpen.Value) return null;

        string eventsText = _events.Count == 0
            ? "No events recorded for this service."
            : string.Join("\n\n", _events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e =>
                {
                    var date = e.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    var label = FormatEventType(e.Type);
                    var msg = string.IsNullOrWhiteSpace(e.Message)
                        ? string.Empty
                        : $"\n  Message: {e.Message}";
                    return $"[{date}]  {label}{msg}";
                }));

        var codeBlock = new CodeBlock(eventsText)
            .Language(Languages.Text)
            .ShowCopyButton()
            .Width(Size.Full())
            .Height(Size.Full());

        var body = Layout.Vertical().Height(Size.Full()) | codeBlock;

        return new Sheet(
            _ => { _isOpen.Set(false); },
            body,
            title: $"Events — {_serviceName}",
            description: $"{_events.Count} event(s) in total")
            .Width(Size.Fraction(1 / 2f));
    }

    private static string FormatEventType(string? type) => type switch
    {
        "service_resume_success" => "Service resumed successfully",
        "service_resume" => "Service resume requested",
        "service_suspend_success" => "Service suspended successfully",
        "service_suspend" => "Service suspension requested",
        "service_deploy_success" => "Service deployed successfully",
        "service_deploy" => "Service deploy started",
        "service_deploy_failed" => "Service deploy failed",
        "service_build_failed" => "Build failed",
        _ => string.IsNullOrWhiteSpace(type) ? "Event" : type
    };
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
    private readonly List<SliplaneServer> _servers;

    public EditServiceSheet(
        IState<bool> isOpen,
        string apiToken,
        string projectId,
        string projectName,
        SliplaneService service,
        IState<(string ProjectId, string ProjectName, SliplaneService Service)?> selection,
        List<SliplaneServer>? servers = null)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projectId = projectId;
        _projectName = projectName;
        _service = service;
        _selection = selection;
        _servers = servers ?? new List<SliplaneServer>();
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var refreshSender = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        var name = this.UseState(_service.Name ?? string.Empty);
        var deployUrl = this.UseState(_service.Deployment?.Url ?? string.Empty);
        var branch = this.UseState(_service.Deployment?.Branch ?? "main");
        var dockerfilePath = this.UseState(_service.Deployment?.DockerfilePath ?? "Dockerfile");
        var dockerContext = this.UseState(_service.Deployment?.DockerContext ?? ".");
        var autoDeploy = this.UseState(_service.Deployment?.AutoDeploy ?? true);
        var cmd = this.UseState(_service.Cmd ?? string.Empty);
        var healthcheck = this.UseState(_service.Healthcheck ?? string.Empty);
        var busy = this.UseState(false);
        var error = this.UseState<string?>(() => (string?)null);
        var envList = this.UseState<List<EnvironmentVariable>>(() =>
            _service.Env is { Count: > 0 }
                ? _service.Env.Select(e => new EnvironmentVariable(e.Key, e.Value, e.Secret)).ToList()
                : new List<EnvironmentVariable>());
        var showAddEnvDialog = this.UseState(false);
        var addEnvKey = this.UseState(string.Empty);
        var addEnvValue = this.UseState(string.Empty);
        var showEditEnvDialog = this.UseState(false);
        var editEnvIndex = this.UseState<int?>(() => null);
        var editEnvKey = this.UseState(string.Empty);
        var editEnvValue = this.UseState(string.Empty);
        // Track current volume mounts — user can remove or add
        var volumesList = this.UseState<List<SliplaneServiceVolumeInfo>>(() =>
            _service.Volumes is { Count: > 0 }
                ? _service.Volumes.ToList()
                : new List<SliplaneServiceVolumeInfo>());
        var volumesModified = this.UseState(false);
        // Server volumes (for the add-volume picker)
        var serverVolumes = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var showAddVolumeDialog = this.UseState(false);
        var addVolumeId = this.UseState(string.Empty);
        var addVolumeMountPath = this.UseState(string.Empty);

        // Load available volumes for this service's server once
        this.UseEffect(async () =>
        {
            if (string.IsNullOrWhiteSpace(_service.ServerId)) return;
            try
            {
                var vols = await client.GetServerVolumesAsync(_apiToken, _service.ServerId);
                serverVolumes.Set(vols ?? new List<SliplaneVolume>());
            }
            catch { serverVolumes.Set(new List<SliplaneVolume>()); }
        });

        if (!_isOpen.Value)
            return null;

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
            new TableCell(" ").IsHeader());
        var envDataRows = envItems
            .Select((e, idx) =>
            {
                var index = idx;
                var displayValue = e.Secret ? "•••••" : (e.Value ?? "");
                return new TableRow(
                    new TableCell(e.Key),
                    new TableCell(displayValue),
                    new TableCell(
                        Layout.Horizontal().AlignContent(Align.Right)
                            | new Button().Icon(Icons.Pencil)
                                .Variant(ButtonVariant.Outline)
                                .OnClick(_ =>
                                {
                                    editEnvIndex.Set(index);
                                    editEnvKey.Set(e.Key);
                                    editEnvValue.Set(e.Value ?? string.Empty);
                                    showEditEnvDialog.Set(true);
                                })
                            | new Button().Icon(Icons.Trash2)
                                .Destructive()
                                .OnClick(_ =>
                                {
                                    var next = envList.Value.Where((_, i) => i != index).ToList();
                                    envList.Set(next);
                                })));
            })
            .ToArray();
        object envTableContent = envDataRows.Length == 0
            ? (object)Text.Muted("No variables.")
            : new Table(new[] { envHeaderRow }.Concat(envDataRows).ToArray()).Width(Size.Full());

        bool IsPausedStatus(string? s) =>
            string.Equals(s, "paused", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "suspended", StringComparison.OrdinalIgnoreCase);

        async Task PauseUnpauseAsync()
        {
            if (busy.Value) return;
            busy.Set(true);
            try
            {
                if (IsPausedStatus(_service.Status))
                    await client.UnpauseServiceAsync(_apiToken, _projectId, _service.Id);
                else
                    await client.PauseServiceAsync(_apiToken, _projectId, _service.Id);

                var updated = await client.GetServiceAsync(_apiToken, _projectId, _service.Id);
                if (updated != null)
                {
                    _selection.Set((_projectId, _projectName, updated));
                    await refreshSender.Send("service-json:" + JsonSerializer.Serialize(updated));
                }
            }
            finally
            {
                busy.Set(false);
            }
        }

        var content = Layout.Vertical()
            | Text.H4("Basic")
            | name.ToTextInput().Placeholder("Service name").WithField().Label("Service name")
            | Text.H4("Deployment")
            | deployUrl.ToTextInput().Placeholder("Repository or image URL").WithField().Label("Repository or image URL")
            | branch.ToTextInput().Placeholder("Branch").WithField().Label("Branch")
            | dockerfilePath.ToTextInput().Placeholder("Dockerfile path").WithField().Label("Dockerfile path")
            | dockerContext.ToTextInput().Placeholder("Docker context").WithField().Label("Docker context")
            | autoDeploy.ToBoolInput().WithField().Label("Auto-deploy on push")
            | Text.H4("Optional")
            | cmd.ToTextInput().Placeholder("Start command (e.g. npm start)").WithField().Label("Start command (e.g. npm start)")
            | healthcheck.ToTextInput().Placeholder("Health check path (e.g. /health)").WithField().Label("Health check path (e.g. /health)")
            | (Layout.Horizontal().AlignContent(Align.Left)
                | Text.H4("Environment variables")
                | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).OnClick(_ => showAddEnvDialog.Set(true))
                )
            | envTableContent
            | BuildVolumesSection(volumesList, volumesModified, () => showAddVolumeDialog.Set(true))
            | BuildServiceInfoSection(
                _service,
                _projectName,
                _servers.FirstOrDefault(s => s.Id == _service.ServerId)?.Name ?? _service.ServerId ?? "—")
            | (error.Value is { Length: > 0 } err ? (object)new Callout(err, variant: CalloutVariant.Error) : Layout.Vertical());

        var pauseLabel = IsPausedStatus(_service.Status) ? "Resume" : "Pause";
        var pauseIcon = IsPausedStatus(_service.Status) ? Icons.Play : Icons.Pause;

        var footer = Layout.Horizontal()
            | new Button("Cancel").Variant(ButtonVariant.Outline).OnClick(_ => _isOpen.Set(false))
            | new Button(pauseLabel).Icon(pauseIcon).Variant(ButtonVariant.Outline).Loading(busy.Value).OnClick(async _ => await PauseUnpauseAsync())
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

        Dialog? editEnvDialog = null;
        if (showEditEnvDialog.Value && editEnvIndex.Value is int envIdx && envIdx >= 0 && envIdx < envItems.Count)
        {
            void SaveEditedEnv()
            {
                if (string.IsNullOrWhiteSpace(editEnvKey.Value)) return;
                var next = (envList.Value ?? new List<EnvironmentVariable>()).ToList();
                var existing = next[envIdx];
                next[envIdx] = new EnvironmentVariable(editEnvKey.Value.Trim(), editEnvValue.Value ?? string.Empty, existing.Secret);
                envList.Set(next);
                showEditEnvDialog.Set(false);
            }

            var editForm = Layout.Vertical()
                | editEnvKey.ToTextInput().Placeholder("Key")
                | editEnvValue.ToTextInput().Placeholder("Value");

            editEnvDialog = new Dialog(
                onClose: (Event<Dialog> _) => showEditEnvDialog.Set(false),
                header: new DialogHeader("Edit environment variable"),
                body: new DialogBody(editForm),
                footer: new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).OnClick(_ => SaveEditedEnv()),
                    new Button("Cancel").OnClick(_ => showEditEnvDialog.Set(false))
                )).Width(Size.Units(220));
        }

        // Add-volume dialog
        Dialog? addVolumeDialog = null;
        if (showAddVolumeDialog.Value)
        {
            var volumeOptions = (serverVolumes.Value ?? new List<SliplaneVolume>())
                .Select(v => new Option<string>($"{v.Name} ({v.MountPath})", v.Id))
                .ToArray();

            void SaveVolume()
            {
                if (string.IsNullOrWhiteSpace(addVolumeId.Value)) return;
                var chosen = serverVolumes.Value?.FirstOrDefault(v => v.Id == addVolumeId.Value);
                if (chosen == null) return;
                var mountPath = string.IsNullOrWhiteSpace(addVolumeMountPath.Value)
                    ? chosen.MountPath
                    : addVolumeMountPath.Value.Trim();
                var next = (volumesList.Value ?? new List<SliplaneServiceVolumeInfo>()).ToList();
                next.Add(new SliplaneServiceVolumeInfo(chosen.Id, chosen.Name, mountPath));
                volumesList.Set(next);
                volumesModified.Set(true);
                addVolumeId.Set(string.Empty);
                addVolumeMountPath.Set(string.Empty);
                showAddVolumeDialog.Set(false);
            }

            var addVolumeForm = Layout.Vertical()
                | Text.Muted("Select a volume from this service's server")
                | addVolumeId.ToSelectInput(volumeOptions)
                | addVolumeMountPath.ToTextInput().Placeholder("Mount path (leave blank for default)");

            addVolumeDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddVolumeDialog.Set(false),
                header: new DialogHeader("Add volume"),
                body: new DialogBody(addVolumeForm),
                footer: new DialogFooter(
                    new Button("Add").Variant(ButtonVariant.Primary).OnClick(_ => SaveVolume()),
                    new Button("Cancel").OnClick(_ => showAddVolumeDialog.Set(false))
                )).Width(Size.Units(220));
        }

        var sheetBody = new FooterLayout(footer, content);
        var dialogs = new[] { addEnvDialog, editEnvDialog, addVolumeDialog }
            .Where(d => d != null).Cast<object>().ToArray();
        object sheetContent = dialogs.Length > 0
            ? new Fragment(new[] { (object)sheetBody }.Concat(dialogs).ToArray())
            : sheetBody;
        return new Sheet(_ => _isOpen.Set(false), sheetContent, title: $"{_service.Name}").Width(Size.Fraction(1 / 3f));
    }

    /// <summary>
    /// Builds a read-only volumes section with Remove buttons.
    /// Always shows the "Volumes" header; if there are no volumes, shows a hint text.
    /// </summary>
    private static object BuildVolumesSection(
        IState<List<SliplaneServiceVolumeInfo>> volumesList,
        IState<bool> volumesModified,
        Action onAdd)
    {
        var items = volumesList.Value;

        var headerRow = new TableRow(
            new TableCell("Name").IsHeader(),
            new TableCell("Mount path").IsHeader(),
            new TableCell(" ").IsHeader());

        var dataRows = items
            .Select((v, idx) =>
            {
                var index = idx;
                return new TableRow(
                    new TableCell(v.Name),
                    new TableCell(v.MountPath),
                    new TableCell(
                        Layout.Horizontal().AlignContent(Align.Right)
                        | new Button().Icon(Icons.Trash2)
                            .Destructive()
                            .OnClick(_ =>
                            {
                                var next = volumesList.Value.Where((_, i) => i != index).ToList();
                                volumesList.Set(next);
                                volumesModified.Set(true);
                            })));
            })
            .ToArray();

        object tableContent = dataRows.Length == 0
            ? (object)Text.Muted("No volumes attached.")
            : new Table(new[] { headerRow }.Concat(dataRows).ToArray()).Width(Size.Full());

        return Layout.Vertical()
            | (Layout.Horizontal().AlignContent(Align.Left)
                | Text.H4("Volumes")
                | new Button("Add volume").Icon(Icons.Plus).Variant(ButtonVariant.Outline).OnClick(_ => onAdd())
              )
            | tableContent;
    }

    private static object BuildServiceInfoSection(SliplaneService svc, string projectName, string serverName)
    {
        var publicEndpoint = svc.Network?.ManagedDomain
            ?? svc.Network?.CustomDomains?.FirstOrDefault()?.Domain;
        var publicEndpointUrl = publicEndpoint != null
            ? (publicEndpoint.StartsWith("http") ? publicEndpoint : $"https://{publicEndpoint}")
            : null;

        var repoUrl = svc.Deployment?.Url;

        var model = new
        {
            DeployHook = svc.Webhook,
            InternalEndpoint = svc.Network?.InternalDomain,
            PublicEndpoint = publicEndpointUrl,
            Repository = repoUrl,
            Project = projectName,
            Server = serverName,
            CreatedAt = svc.CreatedAt.ToString("dd.MM.yyyy HH:mm")
        };

        return Layout.Vertical()
            | Text.H4("Service Info")
            | model
                .ToDetails()
                .RemoveEmpty()
                .Builder(x => x.DeployHook, b => b.CopyToClipboard())
                .Builder(x => x.InternalEndpoint, b => b.CopyToClipboard())
                .Builder(x => x.PublicEndpoint, b => b.Link())
                .Builder(x => x.Repository, b => b.Link());
    }
}

/// <summary>
/// Sheet to create a new service with all Sliplane API fields (deployment, network, cmd, healthcheck, env, volumes).
/// Uses FooterLayout and plain sections (no Cards).
/// When fixedProjectId is set, project selector is disabled (used from Projects view).
/// </summary>
public class CreateServiceSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly List<SliplaneProject> _projects;
    private readonly string? _fixedProjectId;

    public CreateServiceSheet(IState<bool> isOpen, string apiToken, List<SliplaneProject> projects, string? fixedProjectId = null)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projects = projects;
        _fixedProjectId = fixedProjectId;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var refreshSender = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        var serverVolumes = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var selectedProjectId = this.UseState(_fixedProjectId ?? string.Empty);
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

        if (!_isOpen.Value)
            return null;

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

        var projectInput = selectedProjectId.ToAsyncSelectInput(QueryProjects, LookupProject, placeholder: "Search project...");
        var basicSection = Layout.Vertical()
            | Text.H4("Basic")
            | (_fixedProjectId != null ? projectInput.Disabled() : projectInput)
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
