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
        var allServices    = this.UseState<List<(string ProjectId, string ProjectName, SliplaneService Service)>>();
        var serverList     = this.UseState<List<SliplaneServer>>(() => new List<SliplaneServer>());
        var loading        = this.UseState(true);
        var error          = this.UseState<string?>();
        var reloadCounter  = this.UseState(0);
        var serviceDetailOpen = this.UseState(false);
        var serviceDetailSelection = this.UseState<(string ProjectId, string ProjectName, SliplaneService Service)?>(() => null);
        var (createSheetView, openCreateSheet) = this.UseTrigger(
            (IState<bool> isOpen) => new CreateServiceSheet(isOpen, _apiToken, _projects, reloadCounter));

        void ShowServiceSheet(string projectId, string projectName, SliplaneService svc)
        {
            serviceDetailSelection.Set((projectId, projectName, svc));
            serviceDetailOpen.Set(true);
        }

        async Task LoadAllAsync()
        {
            loading.Set(true);
            error.Set((string?)null);
            try
            {
                var overview = await client.GetOverviewAsync(_apiToken);
                if (overview == null)
                {
                    allServices.Set(new List<(string, string, SliplaneService)>());
                    serverList.Set(new List<SliplaneServer>());
                    return;
                }
                serverList.Set(overview.Servers);
                var flat = new List<(string ProjectId, string ProjectName, SliplaneService Service)>();
                foreach (var kv in overview.ServicesByProject)
                {
                    var projectName = overview.Projects.FirstOrDefault(p => p.Id == kv.Key)?.Name ?? kv.Key;
                    foreach (var svc in kv.Value)
                        flat.Add((kv.Key, projectName, svc));
                }
                allServices.Set(flat);
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

        this.UseEffect(async () => await LoadAllAsync(), EffectTrigger.OnMount(), reloadCounter);

        if (loading.Value)
            return Layout.Center() | Text.Muted("Loading all services...");

        if (error.Value is { Length: > 0 })
            return new Callout($"Error: {error.Value}", variant: CalloutVariant.Error);

        var currentServices = allServices.Value ?? new List<(string ProjectId, string ProjectName, SliplaneService Service)>();
        var servers = serverList.Value ?? new List<SliplaneServer>();

        object content;
        if (currentServices.Count == 0)
        {
            content = Layout.Vertical()
                | Text.H2("Services")
                | new Callout("No services found.", variant: CalloutVariant.Info)
                | Layout.Horizontal()
                    | new Button("Add service").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => openCreateSheet());
        }
        else
        {
            var cards = BuildServiceCards(currentServices, servers, ShowServiceSheet);
            content = Layout.Vertical()
                | Text.H2("Services")
                | (Layout.Horizontal()
                    | new Button("Add service").Icon(Icons.Plus).Variant(ButtonVariant.Outline).HandleClick(_ => openCreateSheet()))
                | (Layout.Grid().Columns(3) | cards);
        }

        return new Fragment(
            content,
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
                var siteUrl = svc.Network?.CustomDomains?.FirstOrDefault()?.Domain
                             ?? svc.Network?.ManagedDomain
                             ?? string.Empty;

                var header = Layout.Vertical()
                    | Text.H4(svc.Name)
                    | Text.Muted(projectName);

                var serverRow = Layout.Horizontal()
                    | Icons.Server.ToIcon()
                    | Text.Block(serverLabel);

                var statusBadge = new Badge(statusLabel);

                var openSiteBtn = !string.IsNullOrWhiteSpace(siteUrl)
                    ? new Button("Open site").Icon(Icons.ExternalLink).Variant(ButtonVariant.Outline)
                        .HandleClick(_ => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(siteUrl) { UseShellExecute = true }); } catch { } })
                    : null;

                var body = Layout.Vertical()
                    | header
                    | serverRow
                    | statusBadge
                    | (openSiteBtn != null ? (object)openSiteBtn : Layout.Vertical());

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
/// Sheet with form to create a new service (project, name, git repo, branch, server, auto-deploy).
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
        var servers = this.UseState<List<SliplaneServer>>(() => new List<SliplaneServer>());
        var selectedProjectId = this.UseState(_projects.Count > 0 ? _projects[0].Id : string.Empty);
        var name = this.UseState(string.Empty);
        var gitRepo = this.UseState(string.Empty);
        var branch = this.UseState("main");
        var serverId = this.UseState(string.Empty);
        var autoDeploy = this.UseState(true);
        var busy = this.UseState(false);
        var error = this.UseState<string?>(() => (string?)null);

        this.UseEffect(async () =>
        {
            try
            {
                var list = await client.GetServersAsync(_apiToken);
                servers.Set(list);
                if (list.Count > 0 && string.IsNullOrEmpty(serverId.Value))
                    serverId.Set(list[0].Id);
            }
            catch
            {
                servers.Set(new List<SliplaneServer>());
            }
        });

        var projectOptions = _projects.Select(p => new Option<string>(p.Name, p.Id)).ToArray();
        var serverOptions = (servers.Value ?? new List<SliplaneServer>()).Select(s => new Option<string>(s.Name, s.Id)).ToArray();

        async Task CreateAsync()
        {
            if (busy.Value) return;
            if (string.IsNullOrWhiteSpace(selectedProjectId.Value)) { error.Set("Select a project."); return; }
            if (string.IsNullOrWhiteSpace(name.Value)) { error.Set("Enter service name."); return; }
            if (string.IsNullOrWhiteSpace(gitRepo.Value)) { error.Set("Enter Git repository URL."); return; }
            if (string.IsNullOrWhiteSpace(serverId.Value)) { error.Set("Select a server."); return; }
            error.Set((string?)null);
            busy.Set(true);
            try
            {
                var request = new CreateServiceRequest(
                    Name: name.Value.Trim(),
                    ServerId: serverId.Value,
                    Network: new ServiceNetworkRequest(Public: true, Protocol: "http"),
                    Deployment: new RepositoryDeployment(
                        Url: gitRepo.Value.Trim(),
                        Branch: string.IsNullOrWhiteSpace(branch.Value) ? "main" : branch.Value.Trim(),
                        AutoDeploy: autoDeploy.Value
                    )
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

        var form = Layout.Vertical()
            | Text.H4("New service")
            | Text.Block("Deploy from a Git repository.").Muted()
            | selectedProjectId.ToSelectInput(projectOptions)
            | name.ToTextInput().Placeholder("Service name")
            | gitRepo.ToTextInput().Placeholder("Git repository URL")
            | branch.ToTextInput().Placeholder("Branch (default: main)")
            | serverId.ToSelectInput(serverOptions)
            | autoDeploy.ToBoolInput()
            | (error.Value is { Length: > 0 } err ? (object)new Callout(err, variant: CalloutVariant.Error) : Layout.Vertical());

        if (!_isOpen.Value)
            return null;

        return new Sheet(
            _ => _isOpen.Set(false),
            new Card(Layout.Vertical()
                | form
                | Layout.Horizontal()
                    | new Button("Create").Icon(Icons.Plus).Variant(ButtonVariant.Primary).Loading(busy.Value).HandleClick(async _ => await CreateAsync())
                    | new Button("Cancel").HandleClick(_ => _isOpen.Set(false))),
            title: "Create service");
    }
}
