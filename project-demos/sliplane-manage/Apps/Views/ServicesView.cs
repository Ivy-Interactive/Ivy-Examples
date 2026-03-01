namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;

/// <summary>
/// Services view: all services as clickable cards; details and create in sheets (like Servers/Projects).
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
        var client         = this.UseService<SliplaneApiClient>();
        var reloadCounter  = this.UseState(0);
        var serviceDetailOpen = this.UseState(false);
        var serviceDetailSelection = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
        var (createSheetView, openCreateSheet) = this.UseTrigger(
            (IState<bool> isOpen) => new CreateServiceSheet(isOpen, _apiToken, _projects, reloadCounter));

        var overviewQuery = this.UseQuery(
            key: ("services-overview", _apiToken, reloadCounter.Value),
            fetcher: async ct => await client.GetOverviewAsync(_apiToken),
            options: new QueryOptions
            {
                RefreshInterval = TimeSpan.FromSeconds(30),
                KeepPrevious = true
            });

        void ShowServiceSheet(string projectId, string projectName, SliplaneService svc)
        {
            serviceDetailSelection.Set((projectId, projectName, svc));
            serviceDetailOpen.Set(true);
        }

        if (overviewQuery.Loading && overviewQuery.Value == null)
            return Layout.Center() | Text.Muted("Loading all services...");

        if (overviewQuery.Error is { } ex)
            return new Callout($"Error: {ex.Message}", variant: CalloutVariant.Error);

        var overview = overviewQuery.Value;
        var servers = overview?.Servers ?? new List<SliplaneServer>();
        var flat = new List<(string ProjectId, string ProjectName, SliplaneService Service)>();
        if (overview != null)
        {
            foreach (var kv in overview.ServicesByProject)
            {
                var projectName = overview.Projects.FirstOrDefault(p => p.Id == kv.Key)?.Name ?? kv.Key;
                foreach (var svc in kv.Value)
                    flat.Add((kv.Key, projectName, svc));
            }
        }

        var currentServices = flat;

        var headerRow = Layout.Horizontal()
            | Text.H2("Services")
            | (overviewQuery.Validating ? Text.Muted("Updating...") : null!);

        var addServiceBtn = new Button("Add service").Icon(Icons.Plus).HandleClick(_ => openCreateSheet()).Large().Secondary().BorderRadius(BorderRadius.Full);
        var addServiceFloat = new FloatingPanel(addServiceBtn, Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        object content;
        if (currentServices.Count == 0)
        {
            content = Layout.Vertical()
                | headerRow
                | new Callout("No services found.", variant: CalloutVariant.Info);
        }
        else
        {
            var cards = BuildServiceCards(currentServices, servers, ShowServiceSheet);
            content = Layout.Vertical()
                | headerRow
                | (Layout.Grid().Columns(3) | cards);
        }

        return new Fragment(
            content,
            addServiceFloat,
            serviceDetailOpen.Value ? new ServiceDetailsSheet(serviceDetailOpen, _apiToken, serviceDetailSelection, reloadCounter) : null,
            createSheetView
        );
    }

    private static object[] BuildServiceCards(
        List<(string ProjectId, string ProjectName, SliplaneService Service)> currentServices,
        List<SliplaneServer> serverList,
        Action<string, string, SliplaneService> showSheet)
    {
        return currentServices
            .Select(t => (t.ProjectId, t.ProjectName, t.Service))
            .Select(t =>
            {
                var (projectId, projectName, svc) = t;
                var serverLabel = string.IsNullOrWhiteSpace(svc.ServerId)
                    ? "—"
                    : (serverList.FirstOrDefault(s => s.Id == svc.ServerId)?.Name ?? svc.ServerId);
                var statusLabel = string.IsNullOrWhiteSpace(svc.Status) ? "—" : svc.Status;
                var statusIcon = string.Equals(svc.Status, "pending", StringComparison.OrdinalIgnoreCase)
                    ? Icons.Clock.ToIcon()
                    : string.Equals(svc.Status, "live", StringComparison.OrdinalIgnoreCase)
                        ? Icons.Play.ToIcon()
                        : string.Equals(svc.Status, "suspended", StringComparison.OrdinalIgnoreCase)
                            ? Icons.Pause.ToIcon()
                            : Icons.MonitorStop.ToIcon();
                var siteUrl = svc.Network?.CustomDomains?.FirstOrDefault()?.Domain
                             ?? svc.Network?.ManagedDomain
                             ?? string.Empty;
                var siteUrlAbsolute = string.IsNullOrWhiteSpace(siteUrl) ? string.Empty
                    : (siteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || siteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        ? siteUrl
                        : "https://" + siteUrl);

                var isDbService = (svc.Image?.Contains("docker.io", StringComparison.OrdinalIgnoreCase) == true)
                    || (svc.Deployment?.Url?.Contains("docker.io", StringComparison.OrdinalIgnoreCase) == true)
                    || (svc.GitRepo?.Contains("docker.io", StringComparison.OrdinalIgnoreCase) == true);
                var serviceIcon = isDbService ? Icons.Database.ToIcon() : Icons.Box.ToIcon();

                var header = Layout.Horizontal().Align(Align.Center)
                    | (Layout.Vertical().Align(Align.Left)
                        | Text.H3(svc.Name))
                    | (Layout.Vertical().Align(Align.Right).Width(Size.Fit())
                        | serviceIcon);
                    

                var serverRow = Layout.Horizontal()
                    | Icons.Server.ToIcon()
                    | Text.Block(serverLabel);
                
                var projectRow = Layout.Horizontal()
                    | Icons.FolderOpen.ToIcon()
                    | Text.Block(projectName);

                var statusRow = Layout.Horizontal()
                    | statusIcon
                    | Text.Block(statusLabel);

                var openLinkRow = string.IsNullOrWhiteSpace(siteUrlAbsolute)
                    ? null
                    : (object)(Layout.Horizontal().Gap(0).Align(Align.Left)
                        | Icons.ExternalLink.ToIcon()
                        | new Button(siteUrl).Link().Url(siteUrlAbsolute));

                var body = Layout.Vertical()
                    | header
                    | serverRow
                    | projectRow
                    | statusRow
                    | (openLinkRow ?? Layout.Vertical());

                return new Card(body).HandleClick(_ => showSheet(projectId, projectName, svc));
            })
            .ToArray();
    }
}

/// <summary>
/// Sheet showing service details: Logs, Events, Metrics tabs and Deploy / Pause / Delete actions.
/// </summary>
public class ServiceDetailsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly IState<(string ProjectId, string ProjectName, SliplaneService Service)?> _selection;
    private readonly IState<int> _reloadCounter;

    public ServiceDetailsSheet(
        IState<bool> isOpen,
        string apiToken,
        IState<(string ProjectId, string ProjectName, SliplaneService Service)?> selection,
        IState<int> reloadCounter)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _selection = selection;
        _reloadCounter = reloadCounter;
    }

    public override object? Build()
    {
        var sel = _selection.Value;
        if (sel == null || !_isOpen.Value)
            return null;

        var (projectId, projectName, service) = sel.Value;
        var client = this.UseService<SliplaneApiClient>();
        var tab = this.UseState(0); // 0=Logs, 1=Events, 2=Metrics
        var logs = this.UseState<List<SliplaneServiceLog>?>();
        var events = this.UseState<List<SliplaneServiceEvent>?>();
        var metrics = this.UseState<SliplaneServiceMetrics?>();
        var busy = this.UseState(false);

        this.UseEffect(async () =>
        {
            try
            {
                var logsTask = client.GetServiceLogsAsync(_apiToken, projectId, service.Id);
                var eventsTask = client.GetServiceEventsAsync(_apiToken, projectId, service.Id);
                var metricsTask = client.GetServiceMetricsAsync(_apiToken, projectId, service.Id);
                await Task.WhenAll(logsTask, eventsTask, metricsTask);
                logs.Set(await logsTask);
                events.Set(await eventsTask);
                metrics.Set(await metricsTask);
            }
            catch
            {
                logs.Set((List<SliplaneServiceLog>?)null);
                events.Set((List<SliplaneServiceEvent>?)null);
                metrics.Set((SliplaneServiceMetrics?)null);
            }
        });

        object tabContent;
        if (tab.Value == 0)
        {
            if (logs.Value == null)
                tabContent = Text.Muted("Loading logs...");
            else if (logs.Value.Count == 0)
                tabContent = Text.Muted("No logs.");
            else
            {
                var codeContent = string.Join(
                    Environment.NewLine,
                    logs.Value.TakeLast(200).Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm:ss}  {l.Line}"));
                tabContent = new CodeBlock(codeContent, Languages.Text)
                    .ShowLineNumbers().ShowCopyButton().Width(Size.Full()).Height(Size.Units(200));
            }
        }
        else if (tab.Value == 1)
        {
            if (events.Value == null)
                tabContent = Text.Muted("Loading events...");
            else if (events.Value.Count == 0)
                tabContent = Text.Muted("No events.");
            else
                tabContent = Layout.Vertical() | events.Value.Take(100).Select(e =>
                    Layout.Horizontal()
                    | Text.InlineCode(e.Type)
                    | Text.Block(e.Message)
                    | Text.Muted(e.CreatedAt.ToString("yyyy-MM-dd HH:mm")))
                    .ToArray<object>();
        }
        else
        {
            if (metrics.Value == null)
                tabContent = Text.Muted("Loading metrics...");
            else
                tabContent = Layout.Vertical()
                    | Text.Block($"CPU: {metrics.Value.CpuUsagePercent:F1}%")
                    | Text.Block($"Memory: {metrics.Value.MemoryUsagePercent:F1}% ({metrics.Value.MemoryUsageMb:F0} / {metrics.Value.MemoryTotalMb:F0} MB)");
        }

        var tabButtons = Layout.Horizontal()
            | new Button("Logs").Variant(tab.Value == 0 ? ButtonVariant.Primary : ButtonVariant.Outline).HandleClick(_ => tab.Set(0))
            | new Button("Events").Variant(tab.Value == 1 ? ButtonVariant.Primary : ButtonVariant.Outline).HandleClick(_ => tab.Set(1))
            | new Button("Metrics").Variant(tab.Value == 2 ? ButtonVariant.Primary : ButtonVariant.Outline).HandleClick(_ => tab.Set(2));

        async Task DeployAsync()
        {
            if (busy.Value) return;
            busy.Set(true);
            try
            {
                await client.DeployServiceAsync(_apiToken, projectId, service.Id);
                _reloadCounter.Set(_reloadCounter.Value + 1);
            }
            finally { busy.Set(false); }
        }

        async Task PauseUnpauseAsync()
        {
            if (busy.Value) return;
            busy.Set(true);
            try
            {
                if (string.Equals(service.Status, "paused", StringComparison.OrdinalIgnoreCase))
                    await client.UnpauseServiceAsync(_apiToken, projectId, service.Id);
                else
                    await client.PauseServiceAsync(_apiToken, projectId, service.Id);
                _reloadCounter.Set(_reloadCounter.Value + 1);
            }
            finally { busy.Set(false); }
        }

        async Task DeleteAsync()
        {
            if (busy.Value) return;
            busy.Set(true);
            try
            {
                await client.DeleteServiceAsync(_apiToken, projectId, service.Id);
                _isOpen.Set(false);
                _reloadCounter.Set(_reloadCounter.Value + 1);
            }
            finally { busy.Set(false); }
        }

        var isPaused = string.Equals(service.Status, "paused", StringComparison.OrdinalIgnoreCase);
        var pauseLabel = isPaused ? "Unpause" : "Pause";

        var actions = Layout.Horizontal()
            | new Button("Deploy").Icon(Icons.Rocket).Variant(ButtonVariant.Outline).Loading(busy.Value).HandleClick(async _ => await DeployAsync())
            | new Button(pauseLabel).Icon(Icons.Pause).Variant(ButtonVariant.Outline).Loading(busy.Value).HandleClick(async _ => await PauseUnpauseAsync())
            | new Button("Delete").Icon(Icons.Trash).Variant(ButtonVariant.Destructive).Loading(busy.Value).HandleClick(async _ => await DeleteAsync());

        var cardContent = Layout.Vertical()
            | Text.Muted(projectName)
                | tabButtons
            | tabContent
            | actions;

        if (!_isOpen.Value)
            return null;

        return new Sheet(_ => _isOpen.Set(false), new Card(cardContent), title: $"Service: {service.Name}");
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
    private readonly IState<int> _reloadCounter;

    public CreateServiceSheet(IState<bool> isOpen, string apiToken, List<SliplaneProject> projects, IState<int> reloadCounter)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _projects = projects;
        _reloadCounter = reloadCounter;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
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
            return ctx.UseQuery<Option<string>[], (string, string)>(
                key: (nameof(QueryProjects), query),
                fetcher: _ => Task.FromResult(_projects
                    .Where(p => string.IsNullOrEmpty(query) || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(p => new Option<string>(p.Name, p.Id))
                    .ToArray()));
        }

        QueryResult<Option<string>?> LookupProject(IViewContext ctx, string? id)
        {
            return ctx.UseQuery<Option<string>?, (string, string?)>(
                key: (nameof(LookupProject), id),
                fetcher: _ => Task.FromResult<Option<string>?>(
                    string.IsNullOrEmpty(id) ? null : _projects.FirstOrDefault(p => p.Id == id) is { } p ? new Option<string>(p.Name, p.Id) : null));
        }

        QueryResult<Option<string>[]> QueryServers(IViewContext ctx, string query)
        {
            return ctx.UseQuery<Option<string>[], (string, string)>(
                key: (nameof(QueryServers), query),
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
            return ctx.UseQuery<Option<string>?, (string, string?)>(
                key: (nameof(LookupServer), id),
                fetcher: async ct =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var s = await client.GetServerAsync(_apiToken, id);
                    return s != null ? new Option<string>(s.Name, s.Id) : null;
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
                var env = envList.Value?.Count > 0 ? envList.Value : null;
                var volumes = volumeMountsList.Value?.Count > 0
                    ? volumeMountsList.Value.Select(v => new ServiceVolumeMount(v.VolumeId, v.MountPath)).ToList()
                    : null;

                var request = new CreateServiceRequest(
                    Name: name.Value.Trim(),
                    ServerId: serverId.Value,
                    Network: new ServiceNetworkRequest(Public: networkPublic.Value, Protocol: networkProtocol.Value),
                    Deployment: new RepositoryDeployment(
                        Url: gitRepo.Value.Trim(),
                        Branch: string.IsNullOrWhiteSpace(branch.Value) ? "main" : branch.Value.Trim(),
                        AutoDeploy: autoDeploy.Value,
                        DockerfilePath: string.IsNullOrWhiteSpace(dockerfilePath.Value) ? "Dockerfile" : dockerfilePath.Value.Trim(),
                        DockerContext: string.IsNullOrWhiteSpace(dockerContext.Value) ? "." : dockerContext.Value.Trim()
                    ),
                    Cmd: string.IsNullOrWhiteSpace(cmd.Value) ? null : cmd.Value.Trim(),
                    Healthcheck: string.IsNullOrWhiteSpace(healthcheck.Value) ? null : healthcheck.Value.Trim(),
                    Env: env,
                    Volumes: volumes
                );
                await client.CreateServiceAsync(_apiToken, selectedProjectId.Value, request);
                _isOpen.Set(false);
                _reloadCounter.Set(_reloadCounter.Value + 1);
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
                    new TableCell(new Button("Remove").Variant(ButtonVariant.Outline).HandleClick(_ =>
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
            | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => showAddEnvDialog.Set(true));

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
            | new Button("Cancel").Variant(ButtonVariant.Outline).HandleClick(_ => _isOpen.Set(false))
            | new Button("Create").Icon(Icons.Plus).Variant(ButtonVariant.Primary).Loading(busy.Value).HandleClick(async _ => await CreateAsync());

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
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveEnv()),
                    new Button("Cancel").HandleClick(_ => showAddEnvDialog.Set(false))
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

        return new Sheet(
            _ => _isOpen.Set(false),
            sheetContent,
            title: "Create service",
            description: "Git repository or Docker image.")
            .Width(Size.Fraction(1 / 3f));
    }
}
