namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;
using Ivy.Helpers;

/// <summary>
/// Full projects management view: list, create, rename, delete.
/// </summary>
public class ProjectsView : ViewBase
{
    private readonly string _apiToken;

    public ProjectsView(string apiToken)
    {
        _apiToken = apiToken;
    }

    public override object? Build()
    {
        var client   = this.UseService<SliplaneApiClient>();
        var refreshSender   = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();
        var refreshReceiver = this.UseSignal<SliplaneRefreshSignal, string, Unit>();
        var projects = this.UseState<List<SliplaneProject>>();
        var loading  = this.UseState(true);
        var error    = this.UseState<string?>();
        var creating = this.UseState(false);
        var newName  = this.UseState(string.Empty);
        var busy     = this.UseState(false);
        var serviceCounts = this.UseState<Dictionary<string, int>>(() => new Dictionary<string, int>());
        var servers = this.UseState<List<SliplaneServer>>();
        var selectedProject = this.UseState<SliplaneProject?>(() => null);
        var services = this.UseState<List<SliplaneService>?>(() => null);
        var servicesLoading = this.UseState(false);
        var servicesError = this.UseState<string?>();
        var selectedServiceForEdit = this.UseState<SliplaneService?>(() => null);
        var selectedServiceForLogs = this.UseState<SliplaneService?>(() => null);
        var selectedServiceForView = this.UseState<SliplaneService?>(() => null);
        var selectedServiceForEvents = this.UseState<SliplaneService?>(() => null);
        var editSheetOpen = this.UseState(false);
        var logsSheetOpen = this.UseState(false);
        var viewSheetOpen = this.UseState(false);
        var eventsSheetOpen = this.UseState(false);
        var serviceLogs = this.UseState<List<SliplaneServiceLog>?>(() => null);
        var serviceLogsLoading = this.UseState(false);
        var serviceLogsError = this.UseState<string?>();
        var serviceDetailsForView = this.UseState<SliplaneService?>(() => null);
        var serviceDetailsForViewLoading = this.UseState(false);
        var serviceDetailsForViewError = this.UseState<string?>();
        var viewSheetBusy = this.UseState(false);
        var serviceEvents = this.UseState<List<SliplaneServiceEvent>?>(() => null);
        var serviceEventsLoading = this.UseState(false);
        var serviceEventsError = this.UseState<string?>();
        var createServiceSheetOpen = this.UseState(false);
        var refreshTick = this.UseState(0);
        var showAddProjectDialog = this.UseState(false);
        var newProjectName = this.UseState(string.Empty);
        var addProjectBusy = this.UseState(false);
        var addProjectError = this.UseState<string?>(() => (string?)null);

        async Task LoadProjectsAsync()
        {
            try
            {
                var list = await client.GetProjectsAsync(_apiToken);
                projects.Set(list);
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

        this.UseEffect(async () => await LoadProjectsAsync());
        this.UseEffect(async () => await LoadProjectsAsync(), refreshTick);

        this.UseEffect(() => refreshReceiver.Receive(_ =>
        {
            refreshTick.Set(refreshTick.Value + 1);
            return new Unit();
        }));

        // Load all servers once so we can resolve server names for services AND populate the create-service sheet
        this.UseEffect(async () =>
        {
            try
            {
                var list = await client.GetServersAsync(_apiToken);
                servers.Set(list);
            }
            catch
            {
                servers.Set(new List<SliplaneServer>());
            }
        });

        // Preload service counts per project so they are visible on cards
        this.UseEffect(async () =>
        {
            var current = projects.Value;
            if (current == null || current.Count == 0) return;

            var map = new Dictionary<string, int>();

            foreach (var p in current)
            {
                try
                {
                    var svcs = await client.GetServicesAsync(_apiToken, p.Id);
                    map[p.Id] = svcs?.Count ?? 0;
                }
                catch
                {
                    map[p.Id] = 0;
                }
            }

            serviceCounts.Set(map);
        }, [projects]);

        // Load services for the currently selected project (for the dialog)
        this.UseEffect(async () =>
        {
            var project = selectedProject.Value;

            if (project is null)
            {
                servicesLoading.Set(false);
                servicesError.Set((string?)null);
                services.Set((List<SliplaneService>?)null);
                return;
            }

            servicesLoading.Set(true);
            servicesError.Set((string?)null);
            services.Set((List<SliplaneService>?)null);

            try
            {
                var list = await client.GetServicesAsync(_apiToken, project.Id);
                services.Set(list);
            }
            catch (Exception ex)
            {
                servicesError.Set(ex.Message);
            }
            finally
            {
                servicesLoading.Set(false);
            }
        }, [selectedProject]);

        // Load logs for the currently selected service (within the selected project)
        this.UseEffect(async () =>
        {
            var project = selectedProject.Value;
            var svc = selectedServiceForLogs.Value;

            if (project is null || svc is null)
            {
                serviceLogsLoading.Set(false);
                serviceLogsError.Set((string?)null);
                serviceLogs.Set((List<SliplaneServiceLog>?)null);
                return;
            }

            serviceLogsLoading.Set(true);
            serviceLogsError.Set((string?)null);
            serviceLogs.Set((List<SliplaneServiceLog>?)null);

            try
            {
                var logs = await client.GetServiceLogsAsync(_apiToken, project.Id, svc.Id);
                serviceLogs.Set(logs);
            }
            catch (Exception ex)
            {
                serviceLogsError.Set(ex.Message);
            }
            finally
            {
                serviceLogsLoading.Set(false);
            }
        }, [selectedProject, selectedServiceForLogs]);

        // Load full service details for View sheet
        this.UseEffect(async () =>
        {
            var project = selectedProject.Value;
            var svc = selectedServiceForView.Value;

            if (project is null || svc is null)
            {
                serviceDetailsForViewLoading.Set(false);
                serviceDetailsForViewError.Set((string?)null);
                serviceDetailsForView.Set(default(SliplaneService?));
                return;
            }

            serviceDetailsForViewLoading.Set(true);
            serviceDetailsForViewError.Set((string?)null);
            serviceDetailsForView.Set(default(SliplaneService?));

            try
            {
                var full = await client.GetServiceAsync(_apiToken, project.Id, svc.Id);
                serviceDetailsForView.Set(full);
            }
            catch (Exception ex)
            {
                serviceDetailsForViewError.Set(ex.Message);
            }
            finally
            {
                serviceDetailsForViewLoading.Set(false);
            }
        }, [selectedProject, selectedServiceForView]);

        // Load events for Events sheet
        this.UseEffect(async () =>
        {
            var project = selectedProject.Value;
            var svc = selectedServiceForEvents.Value;

            if (project is null || svc is null)
            {
                serviceEventsLoading.Set(false);
                serviceEventsError.Set((string?)null);
                serviceEvents.Set((List<SliplaneServiceEvent>?)null);
                return;
            }

            serviceEventsLoading.Set(true);
            serviceEventsError.Set((string?)null);
            serviceEvents.Set((List<SliplaneServiceEvent>?)null);

            try
            {
                var list = await client.GetServiceEventsAsync(_apiToken, project.Id, svc.Id);
                serviceEvents.Set(list);
            }
            catch (Exception ex)
            {
                serviceEventsError.Set(ex.Message);
            }
            finally
            {
                serviceEventsLoading.Set(false);
            }
        }, [selectedProject, selectedServiceForEvents]);

        if (loading.Value)
            return Layout.Center() | Text.Muted("Loading projects...");

        if (error.Value is { Length: > 0 })
            return new Callout($"Error: {error.Value}", variant: CalloutVariant.Error);

        var list = projects.Value ?? new List<SliplaneProject>();

        object projectsBlock;
        if (list.Count == 0)
        {
            projectsBlock = new Callout("No projects yet. Create one above.", variant: CalloutVariant.Info);
        }
        else
        {
            var cards = list
                .Select(p =>
                {
                    var hasCount = serviceCounts.Value.TryGetValue(p.Id, out var svcCount);
                    var svcCountNum = hasCount ? svcCount : 0;
                    var label = hasCount
                        ? $"{svcCount} Service" + (svcCount == 1 ? string.Empty : "s")
                        : "0 Services";

                    var header = Layout.Horizontal().Align(Align.Center)
                        | (Layout.Vertical().Align(Align.Left)
                            | Text.H3(p.Name))
                        | (Layout.Vertical().Align(Align.Right).Width(Size.Fit())
                            | (svcCountNum > 0 ? Icons.FolderOpen.ToIcon() : Icons.Folder.ToIcon()));

                    var servicesRow = Layout.Horizontal()
                        | Icons.Box.ToIcon()
                        | Text.Block(label);

                    var body = Layout.Vertical()
                        | header
                        | servicesRow;

                    return new Card(body)
                        .HandleClick(_ => selectedProject.Set(p));
                })
                .ToArray();

            projectsBlock = Layout.Grid().Columns(3) | cards;
        }

        Dialog? projectDetailDialog = null;
        object? createServiceSheetView = null;
        object? editSheetView = null;
        object? logsSheetView = null;
        object? viewSheetView = null;
        object? eventsSheetView = null;
        if (selectedProject.Value is { } project)
        {
            object body;
            if (servicesLoading.Value)
            {
                body = Text.Muted("Loading project services...");
            }
            else if (servicesError.Value is { Length: > 0 })
            {
                body = new Callout($"Error loading services: {servicesError.Value}", variant: CalloutVariant.Error);
            }
            else if (services.Value == null || services.Value.Count == 0)
            {
                body = new Callout("No services in this project yet.", variant: CalloutVariant.Info);
            }
            else
            {
                var tableItems = services.Value
                    .Select(s =>
                    {
                        var customDomain = s.Network?.CustomDomains?.FirstOrDefault()?.Domain
                                           ?? s.Network?.ManagedDomain
                                           ?? "—";
                        var internalDomain = string.IsNullOrWhiteSpace(s.Network?.InternalDomain)
                            ? "—"
                            : s.Network!.InternalDomain;
                        var repoUrl = s.Deployment?.Url ?? s.GitRepo ?? "—";

                        string serverLabel;
                        if (!string.IsNullOrWhiteSpace(s.ServerId))
                        {
                            var server = servers.Value?.FirstOrDefault(sv => sv.Id == s.ServerId);
                            serverLabel = server != null ? server.Name : s.ServerId!;
                        }
                        else
                        {
                            serverLabel = "—";
                        }

                        return new
                        {
                            s.Name,
                            Status = string.IsNullOrWhiteSpace(s.Status) ? "—" : s.Status,
                            Server = serverLabel,
                            Domain = customDomain,
                            InternalDomain = internalDomain,
                            RepoUrl = repoUrl
                        };
                    })
                    .ToList();

                body = tableItems
                    .AsQueryable()
                    .ToDataTable(idSelector: x => x.Name)
                    .Height(Size.Units(100))
                    .Header(x => x.Name, "Service")
                    .Header(x => x.Status, "Status")
                    .Header(x => x.Server, "Server")
                    .Header(x => x.Domain, "Domain")
                    .Header(x => x.InternalDomain, "Internal domain")
                    .Header(x => x.RepoUrl, "Repo URL")
                    .Width(x => x.Status, Size.Px(60))
                    .Width(x => x.Domain, Size.Px(210))
                    .Width(x => x.InternalDomain, Size.Px(170))
                    .Width(x => x.Server, Size.Px(60))
                    .RowActions(
                        MenuItem.Default(Icons.Eye, "View").Tag("view"),
                        MenuItem.Default(Icons.Pencil, "Edit").Tag("edit"),
                        MenuItem.Default(Icons.FileText, "Logs").Tag("logs"),
                        MenuItem.Default(Icons.Calendar, "Events").Tag("events")
                    )
                    .HandleRowAction(e =>
                    {
                        var args = e.Value;
                        var tag = args.Tag?.ToString();
                        var name = args.Id?.ToString();
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(tag))
                            return ValueTask.CompletedTask;

                        var svc = services.Value?.FirstOrDefault(s => s.Name == name);
                        if (svc == null)
                            return ValueTask.CompletedTask;

                        if (tag == "edit")
                        {
                            selectedServiceForEdit.Set(svc);
                            editSheetOpen.Set(true);
                        }
                        else if (tag == "logs")
                        {
                            selectedServiceForLogs.Set(svc);
                            logsSheetOpen.Set(true);
                        }
                        else if (tag == "view")
                        {
                            selectedServiceForView.Set(svc);
                            viewSheetOpen.Set(true);
                        }
                        else if (tag == "events")
                        {
                            selectedServiceForEvents.Set(svc);
                            eventsSheetOpen.Set(true);
                        }

                        return ValueTask.CompletedTask;
                    })
                    .Config(c =>
                    {
                        c.AllowSorting = true;
                        c.AllowFiltering = true;
                        c.ShowSearch = true;
                    });
            }

            projectDetailDialog = new Dialog(
                onClose: (Event<Dialog> _) => selectedProject.Set((SliplaneProject?)null),
                header: new DialogHeader(project.Name),
                body: new DialogBody(body),
                footer: new DialogFooter(
                    new Button("Create service")
                        .Icon(Icons.Plus)
                        .Variant(ButtonVariant.Outline)
                        .HandleClick(_ => createServiceSheetOpen.Set(true)),
                    new Button("Close").HandleClick(_ => selectedProject.Set((SliplaneProject?)null)))
            ).Width(Size.Units(220));

            if (createServiceSheetOpen.Value)
            {
                createServiceSheetView = new ProjectCreateServiceSheet(
                    createServiceSheetOpen,
                    _apiToken,
                    project.Id,
                    project.Name,
                    servers.Value ?? new List<SliplaneServer>(),
                    onCreated: async () =>
                    {
                        var list = await client.GetServicesAsync(_apiToken, project.Id);
                        services.Set(list);
                    },
                    onClose: () => { });
            }

            if (editSheetOpen.Value && selectedServiceForEdit.Value is { } svcEdit)
            {
                editSheetView = new ProjectServiceEditSheet(
                    editSheetOpen,
                    _apiToken,
                    project.Id,
                    project.Name,
                    svcEdit,
                    onSaved: async () =>
                    {
                        var list = await client.GetServicesAsync(_apiToken, project.Id);
                        services.Set(list);
                    },
                    onClose: () => selectedServiceForEdit.Set(default(SliplaneService?)));
            }

            if (logsSheetOpen.Value && selectedServiceForLogs.Value is { } svcLogs)
            {
                object logsBody;
                if (serviceLogsLoading.Value)
                    logsBody = Text.Muted("Loading logs...");
                else if (serviceLogsError.Value is { Length: > 0 })
                    logsBody = new Callout($"Error: {serviceLogsError.Value}", variant: CalloutVariant.Error);
                else if (serviceLogs.Value == null || serviceLogs.Value.Count == 0)
                    logsBody = Text.Muted("No logs found.");
                else
                    logsBody = new CodeBlock(
                        string.Join(Environment.NewLine,
                            serviceLogs.Value.TakeLast(200).Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm:ss}  {l.Line}")),
                        Languages.Text).ShowLineNumbers().ShowCopyButton().Width(Size.Full()).Height(Size.Units(200));

                var logsFooter = Layout.Horizontal()
                    | new Button("Close", onClick: _ => { logsSheetOpen.Set(false); selectedServiceForLogs.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                logsSheetView = new Sheet(
                    _ => { logsSheetOpen.Set(false); selectedServiceForLogs.Set(default(SliplaneService?)); },
                    new FooterLayout(logsFooter, logsBody),
                    title: $"Logs: {svcLogs.Name}").Width(Size.Fraction(1 / 2f));
            }

            if (viewSheetOpen.Value && selectedServiceForView.Value is { } svcView)
            {
                object viewBody;
                object viewFooter;
                bool isPausedStatus(string? s) =>
                    string.Equals(s, "paused", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "suspended", StringComparison.OrdinalIgnoreCase);

                if (serviceDetailsForViewLoading.Value)
                {
                    viewBody = Text.Muted("Loading service details...");
                    viewFooter = Layout.Horizontal()
                        | new Button("Close", onClick: _ => { viewSheetOpen.Set(false); selectedServiceForView.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                }
                else if (serviceDetailsForViewError.Value is { Length: > 0 })
                {
                    viewBody = new Callout($"Error: {serviceDetailsForViewError.Value}", variant: CalloutVariant.Error);
                    viewFooter = Layout.Horizontal()
                        | new Button("Close", onClick: _ => { viewSheetOpen.Set(false); selectedServiceForView.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                }
                else if (serviceDetailsForView.Value is not { } full)
                {
                    viewBody = Text.Muted("No details.");
                    viewFooter = Layout.Horizontal()
                        | new Button("Close", onClick: _ => { viewSheetOpen.Set(false); selectedServiceForView.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                }
                else
                {
                    var dep = full.Deployment;
                    var net = full.Network;
                    var basicModel = new
                    {
                        Project = project.Name,
                        Name = full.Name,
                        Status = full.Status,
                        ServerId = full.ServerId,
                        Image = full.Image ?? "—",
                        Port = full.Port?.ToString() ?? "—",
                        Created = full.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        Updated = full.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—"
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
                    viewBody = Layout.Vertical()
                        | basicModel.ToDetails()
                        | Text.H4("Deployment")
                        | deploymentModel.ToDetails()
                        | Text.H4("Network")
                        | networkModel.ToDetails()
                        | (full.Domains?.Count > 0 == true
                            ? Layout.Vertical()
                                | Text.H4("Domains")
                                | (Layout.Vertical() | full.Domains.Select(d => Text.Block($"{d.Domain} (custom: {d.IsCustom})")).ToArray<object>())
                            : Layout.Vertical());

                    async Task PauseUnpauseAsync()
                    {
                        if (viewSheetBusy.Value) return;
                        viewSheetBusy.Set(true);
                        try
                        {
                            if (isPausedStatus(full.Status))
                                await client.UnpauseServiceAsync(_apiToken, project.Id, full.Id);
                            else
                                await client.PauseServiceAsync(_apiToken, project.Id, full.Id);
                            var updated = await client.GetServiceAsync(_apiToken, project.Id, full.Id);
                            if (updated != null)
                                serviceDetailsForView.Set(updated);
                            var list = await client.GetServicesAsync(_apiToken, project.Id);
                            services.Set(list);
                        }
                        finally { viewSheetBusy.Set(false); }
                    }

                    async Task DeleteAsync()
                    {
                        if (viewSheetBusy.Value) return;
                        viewSheetBusy.Set(true);
                        try
                        {
                            await client.DeleteServiceAsync(_apiToken, project.Id, full.Id);
                            viewSheetOpen.Set(false);
                            selectedServiceForView.Set(default(SliplaneService?));
                            var list = await client.GetServicesAsync(_apiToken, project.Id);
                            services.Set(list);
                        }
                        finally { viewSheetBusy.Set(false); }
                    }

                    var pauseLabel = isPausedStatus(full.Status) ? "Resume" : "Pause";
                    viewFooter = Layout.Horizontal()
                        | new Button("Edit").Icon(Icons.Pencil).Variant(ButtonVariant.Outline).HandleClick(_ => { selectedServiceForEdit.Set(full); editSheetOpen.Set(true); })
                        | new Button(pauseLabel).Icon(isPausedStatus(full.Status) ? Icons.Play : Icons.Pause).Variant(ButtonVariant.Outline).Loading(viewSheetBusy.Value).HandleClick(async _ => await PauseUnpauseAsync())
                        | new Button("Delete", onClick: async _ => await DeleteAsync())
                            .Icon(Icons.Trash).Variant(ButtonVariant.Destructive).Loading(viewSheetBusy.Value)
                            .WithConfirm("Are you sure you want to delete this service?", "Delete service")
                        | new Button("Close", onClick: _ => { viewSheetOpen.Set(false); selectedServiceForView.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                }

                viewSheetView = new Sheet(
                    _ => { viewSheetOpen.Set(false); selectedServiceForView.Set(default(SliplaneService?)); },
                    new FooterLayout(viewFooter, viewBody),
                    title: $"View: {svcView.Name}").Width(Size.Fraction(1 / 3f));
            }

            if (eventsSheetOpen.Value && selectedServiceForEvents.Value is { } svcEvents)
            {
                object eventsBody;
                if (serviceEventsLoading.Value)
                    eventsBody = Text.Muted("Loading events...");
                else if (serviceEventsError.Value is { Length: > 0 })
                    eventsBody = new Callout($"Error: {serviceEventsError.Value}", variant: CalloutVariant.Error);
                else if (serviceEvents.Value == null || serviceEvents.Value.Count == 0)
                    eventsBody = Text.Muted("No events.");
                else
                {
                    var rows = serviceEvents.Value
                        .OrderByDescending(ev => ev.CreatedAt)
                        .Take(200)
                        .Select(ev => new TableRow(
                            new TableCell(ev.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                            new TableCell(ev.Type),
                            new TableCell(ev.Message)))
                        .ToArray();
                    var headerRow = new TableRow(
                        new TableCell("Time").IsHeader(),
                        new TableCell("Type").IsHeader(),
                        new TableCell("Message").IsHeader());
                    eventsBody = new Table(new[] { headerRow }.Concat(rows).ToArray()).Width(Size.Full());
                }
                var eventsFooter = Layout.Horizontal()
                    | new Button("Close", onClick: _ => { eventsSheetOpen.Set(false); selectedServiceForEvents.Set(default(SliplaneService?)); }).Variant(ButtonVariant.Outline);
                eventsSheetView = new Sheet(
                    _ => { eventsSheetOpen.Set(false); selectedServiceForEvents.Set(default(SliplaneService?)); },
                    new FooterLayout(eventsFooter, eventsBody),
                    title: $"Events: {svcEvents.Name}").Width(Size.Fraction(1 / 2f));
            }
        }

        var addProjectBtn = new Button("Add project").Icon(Icons.Plus).Large().Secondary().BorderRadius(BorderRadius.Full).HandleClick(_ => showAddProjectDialog.Set(true));
        var addProjectFloat = new FloatingPanel(addProjectBtn, Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        Dialog? addProjectDialog = null;
        if (showAddProjectDialog.Value)
        {
            async Task CreateProjectAsync()
            {
                if (addProjectBusy.Value) return;
                if (string.IsNullOrWhiteSpace(newProjectName.Value))
                {
                    addProjectError.Set("Enter project name.");
                    return;
                }
                addProjectError.Set((string?)null);
                addProjectBusy.Set(true);
                try
                {
                    await client.CreateProjectAsync(_apiToken, newProjectName.Value.Trim());
                    await refreshSender.Send("projects");
                    newProjectName.Set(string.Empty);
                    showAddProjectDialog.Set(false);
                    var list = await client.GetProjectsAsync(_apiToken);
                    projects.Set(list);
                }
                catch (Exception ex)
                {
                    addProjectError.Set(ex.Message);
                }
                finally
                {
                    addProjectBusy.Set(false);
                }
            }

            var addProjectForm = Layout.Vertical()
                | newProjectName.ToTextInput().Placeholder("Project name")
                | (addProjectError.Value is { Length: > 0 } err
                    ? (object)new Callout(err, variant: CalloutVariant.Error)
                    : Layout.Vertical());

            addProjectDialog = new Dialog(
                onClose: (Event<Dialog> _) => { showAddProjectDialog.Set(false); addProjectError.Set((string?)null); },
                header: new DialogHeader("Add project"),
                body: new DialogBody(addProjectForm),
                footer: new DialogFooter(
                    new Button("Create").Icon(Icons.Plus).Variant(ButtonVariant.Primary).Loading(addProjectBusy.Value).HandleClick(async _ => await CreateProjectAsync()),
                    new Button("Cancel").HandleClick(_ => showAddProjectDialog.Set(false)))
            ).Width(Size.Units(220));
        }

        return new Fragment(
            Layout.Vertical()
                | Text.H2("Projects")
                | projectsBlock
                | projectDetailDialog
                | addProjectFloat
                | addProjectDialog,
            editSheetView,
            logsSheetView,
            viewSheetView,
            eventsSheetView,
            createServiceSheetView
        );
    }
}

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

/// <summary>
/// Sheet to edit a service from the Projects view (PATCH). Refreshes the services list via onSaved.
/// </summary>
public class ProjectServiceEditSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _projectName;
    private readonly SliplaneService _service;
    private readonly Func<Task> _onSaved;
    private readonly Action _onClose;

    public ProjectServiceEditSheet(
        IState<bool> isOpen,
        string apiToken,
        string projectId,
        string projectName,
        SliplaneService service,
        Func<Task> onSaved,
        Action onClose)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projectId = projectId;
        _projectName = projectName;
        _service = service;
        _onSaved = onSaved;
        _onClose = onClose;
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
                await _onSaved();
                _isOpen.Set(false);
                _onClose();
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

        void CloseSheet()
        {
            _isOpen.Set(false);
            _onClose();
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
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).HandleClick(_ =>
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
            | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => showAddEnvDialog.Set(true))
            | (error.Value is { Length: > 0 } err ? (object)new Callout(err, variant: CalloutVariant.Error) : Layout.Vertical());

        var footer = Layout.Horizontal()
            | new Button("Cancel", onClick: _ => CloseSheet()).Variant(ButtonVariant.Outline)
            | new Button("Save").Icon(Icons.Check).Variant(ButtonVariant.Primary).Loading(busy.Value).HandleClick(async _ => await SaveAsync());

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
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveEnv()),
                    new Button("Cancel").HandleClick(_ => showAddEnvDialog.Set(false))
                )).Width(Size.Units(220));
        }

        if (!_isOpen.Value)
            return null;

        var sheetBody = new FooterLayout(footer, content);
        object sheetContent = addEnvDialog != null ? new Fragment(sheetBody, addEnvDialog) : sheetBody;
        return new Sheet(_ => CloseSheet(), sheetContent, title: $"Edit: {_service.Name}").Width(Size.Fraction(1 / 3f));
    }
}

/// <summary>
/// Sheet to create a new service from the Projects view. Full CreateServiceRequest: Basic, Deployment, Network, Optional (cmd, healthcheck), Env, Volumes.
/// </summary>
public class ProjectCreateServiceSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly string _projectName;
    private readonly List<SliplaneServer> _servers;
    private readonly Func<Task> _onCreated;
    private readonly Action _onClose;

    public ProjectCreateServiceSheet(
        IState<bool> isOpen,
        string apiToken,
        string projectId,
        string projectName,
        List<SliplaneServer> servers,
        Func<Task> onCreated,
        Action onClose)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projectId = projectId;
        _projectName = projectName;
        _servers = servers;
        _onCreated = onCreated;
        _onClose = onClose;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();
        var serverVolumes = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var name = this.UseState(string.Empty);
        var serverId = this.UseState(string.Empty);
        var gitRepo = this.UseState(string.Empty);
        var branch = this.UseState("main");
        var dockerfilePath = this.UseState("Dockerfile");
        var dockerContext = this.UseState(".");
        var autoDeploy = this.UseState(true);
        var cmd = this.UseState(string.Empty);
        var healthcheck = this.UseState(string.Empty);
        var networkPublic = this.UseState(true);
        var networkProtocol = this.UseState("http");
        var busy = this.UseState(false);
        var error = this.UseState<string?>(() => (string?)null);
        var envList = this.UseState<List<EnvironmentVariable>>(() => new List<EnvironmentVariable>());
        var showAddEnvDialog = this.UseState(false);
        var addEnvKey = this.UseState(string.Empty);
        var addEnvValue = this.UseState(string.Empty);
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

        var serverOptions = _servers.Select(s => new Option<string>(s.Name, s.Id)).ToArray();
        var protocolOptions = new[] { new Option<string>("HTTP", "http"), new Option<string>("HTTPS", "https") };
        var volumeOptions = (serverVolumes.Value ?? new List<SliplaneVolume>()).Select(v => new Option<string>($"{v.Name} ({v.MountPath})", v.Id)).ToArray();

        async Task CreateAsync()
        {
            if (busy.Value) return;
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
                await client.CreateServiceAsync(_apiToken, _projectId, request);
                await _onCreated();
                _isOpen.Set(false);
                _onClose();
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

        void CloseSheet()
        {
            _isOpen.Set(false);
            _onClose();
        }

        var basicSection = Layout.Vertical()
            | Text.H4("Basic")
            | Text.Block($"Project: {_projectName}").Muted()
            | name.ToTextInput().Placeholder("Service name")
            | serverId.ToSelectInput(serverOptions).Placeholder("Select server");

        var deploymentSection = Layout.Vertical()
            | Text.H4("Deployment (source and build)")
            | gitRepo.ToTextInput().Placeholder("Repository URL or Docker image")
            | branch.ToTextInput().Placeholder("Branch (default: main)")
            | dockerfilePath.ToTextInput().Placeholder("Dockerfile path")
            | dockerContext.ToTextInput().Placeholder("Docker context")
            | autoDeploy.ToBoolInput().Label("Auto-deploy on push");

        var optionalSection = Layout.Vertical()
            | Text.H4("Optional (command, healthcheck)")
            | cmd.ToTextInput().Placeholder("Start command (e.g. npm start)")
            | healthcheck.ToTextInput().Placeholder("Health check path (e.g. /health)");

        var networkSection = Layout.Vertical()
            | Text.H4("Network")
            | networkPublic.ToBoolInput().Label("Public access")
            | networkProtocol.ToSelectInput(protocolOptions);

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
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).HandleClick(_ =>
                    {
                        var next = envList.Value.Where((_, i) => i != index).ToList();
                        envList.Set(next);
                    })).Width(Size.Fit()));
            })
            .ToArray();
        object envTableContent = envDataRows.Length == 0
            ? (object)Text.Muted("No variables.")
            : new Table(new[] { envHeaderRow }.Concat(envDataRows).ToArray()).Width(Size.Full());
        var envSection = Layout.Vertical()
            | Text.H4("Environment variables")
            | envTableContent
            | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => showAddEnvDialog.Set(true));

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
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).HandleClick(_ =>
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
            | new Button("Add volume").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => showAddVolumeDialog.Set(true));

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
            | new Button("Cancel", onClick: _ => CloseSheet()).Variant(ButtonVariant.Outline)
            | new Button("Create").Icon(Icons.Plus).Variant(ButtonVariant.Primary).Loading(busy.Value).HandleClick(async _ => await CreateAsync());

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
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveEnv()),
                    new Button("Cancel").HandleClick(_ => showAddEnvDialog.Set(false))
                )).Width(Size.Units(220));
        }

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
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveVolume()),
                    new Button("Cancel").HandleClick(_ => showAddVolumeDialog.Set(false))
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

        return new Sheet(_ => CloseSheet(), sheetContent, title: "Create service", description: $"Project: {_projectName}").Width(Size.Fraction(1 / 3f));
    }
}
