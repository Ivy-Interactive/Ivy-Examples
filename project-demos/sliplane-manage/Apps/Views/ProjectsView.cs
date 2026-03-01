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
        var serviceLogs = this.UseState<List<SliplaneServiceLog>?>(() => null);
        var serviceLogsLoading = this.UseState(false);
        var serviceLogsError = this.UseState<string?>();
        var showCreateServiceDialog = this.UseState(false);
        var newServiceName = this.UseState(string.Empty);
        var newServiceGitRepo = this.UseState(string.Empty);
        var newServiceBranch = this.UseState("main");
        var newServiceServerId = this.UseState(string.Empty);
        var newServiceAutoDeploy = this.UseState(true);
        var createServiceBusy = this.UseState(false);
        var createServiceError = this.UseState<string?>();

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

        // Load all servers once so we can resolve server names for services AND populate the create-service dropdown
        this.UseEffect(async () =>
        {
            try
            {
                var list = await client.GetServersAsync(_apiToken);
                servers.Set(list);
                if (list.Count > 0 && string.IsNullOrEmpty(newServiceServerId.Value))
                    newServiceServerId.Set(list[0].Id);
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
                    var label = hasCount
                        ? $"{svcCount} Service" + (svcCount == 1 ? string.Empty : "s")
                        : "Services: —";

                    var icon = hasCount && svcCount > 0
                        ? Icons.FolderOpen
                        : Icons.Folder;

                    return new Card(
                            (Layout.Vertical().Align(Align.Center)
                            | Text.H2(p.Name)
                            | Text.Muted(label))
                        )
                        .Title("Project")
                        .Icon(icon)
                        .HandleClick(_ => selectedProject.Set(p));
                })
                .ToArray();

            projectsBlock = Layout.Grid().Columns(3).Gap(3) | cards;
        }

        Dialog? projectDetailDialog = null;
        Dialog? createServiceDialog = null;
        Dialog? serviceEditDialog = null;
        Dialog? serviceLogsDialog = null;
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
                        MenuItem.Default(Icons.Pencil, "Edit").Tag("edit"),
                        MenuItem.Default(Icons.FileText, "Logs").Tag("logs")
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
                        }
                        else if (tag == "logs")
                        {
                            selectedServiceForLogs.Set(svc);
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
                        .HandleClick(_ => showCreateServiceDialog.Set(true)),
                    new Button("Close").HandleClick(_ => selectedProject.Set((SliplaneProject?)null)))
            ).Width(Size.Units(220));

            if (showCreateServiceDialog.Value)
            {
                async Task CreateAndDeployServiceAsync()
                {
                    if (createServiceBusy.Value) return;
                    if (string.IsNullOrWhiteSpace(newServiceName.Value))
                    {
                        createServiceError.Set("Enter service name.");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(newServiceGitRepo.Value))
                    {
                        createServiceError.Set("Enter Git repository URL.");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(newServiceServerId.Value))
                    {
                        createServiceError.Set("Select a server.");
                        return;
                    }
                    createServiceError.Set((string?)null);
                    createServiceBusy.Set(true);
                    try
                    {
                        var request = new CreateServiceRequest(
                            Name: newServiceName.Value.Trim(),
                            ServerId: newServiceServerId.Value,
                            Network: new ServiceNetworkRequest(Public: true, Protocol: "http"),
                            Deployment: new RepositoryDeployment(
                                Url: newServiceGitRepo.Value.Trim(),
                                Branch: string.IsNullOrWhiteSpace(newServiceBranch.Value) ? "main" : newServiceBranch.Value.Trim(),
                                AutoDeploy: newServiceAutoDeploy.Value
                            )
                        );
                        await client.CreateServiceAsync(_apiToken, project.Id, request);
                        newServiceName.Set(string.Empty);
                        newServiceGitRepo.Set(string.Empty);
                        newServiceBranch.Set("main");
                        newServiceAutoDeploy.Set(true);
                        showCreateServiceDialog.Set(false);
                        var list = await client.GetServicesAsync(_apiToken, project.Id);
                        services.Set(list);
                    }
                    catch (Exception ex)
                    {
                        createServiceError.Set(ex.Message);
                    }
                    finally
                    {
                        createServiceBusy.Set(false);
                    }
                }

                var serverOptions = (servers.Value ?? new List<SliplaneServer>())
                    .Select(s => new Option<string>(s.Name, s.Id))
                    .ToArray();

                var createForm = Layout.Vertical().Gap(2)
                    | Text.H4("New service")
                    | Text.Block("Deploy a new app from a Git repository.").Muted()
                    | newServiceName.ToTextInput().Placeholder("Service name")
                    | newServiceGitRepo.ToTextInput().Placeholder("Git repository URL (e.g. https://github.com/user/repo)")
                    | newServiceBranch.ToTextInput().Placeholder("Branch (default: main)")
                    | (Layout.Horizontal().Gap(3).Align(Align.Center)
                       | Text.Block("Server:").Bold()
                       | newServiceServerId.ToSelectInput(serverOptions))
                    | (Layout.Horizontal().Gap(3).Align(Align.Center)
                       | Text.Block("Auto-deploy on push:").Bold()
                       | newServiceAutoDeploy.ToBoolInput())
                    | (createServiceError.Value is { Length: > 0 } err
                        ? (object)new Callout(err, variant: CalloutVariant.Error)
                        : Layout.Vertical());

                createServiceDialog = new Dialog(
                    onClose: (Event<Dialog> _) => { showCreateServiceDialog.Set(false); createServiceError.Set((string?)null); },
                    header: new DialogHeader("Create service"),
                    body: new DialogBody(createForm),
                    footer: new DialogFooter(
                        new Button("Create & Deploy")
                            .Icon(Icons.Rocket)
                            .Variant(ButtonVariant.Primary)
                            .Loading(createServiceBusy.Value)
                            .HandleClick(async _ => await CreateAndDeployServiceAsync()),
                        new Button("Cancel").HandleClick(_ => showCreateServiceDialog.Set(false)))
                ).Width(Size.Units(220));
            }

            if (selectedServiceForEdit.Value is { } svcEdit)
            {
                var detailsModel = new
                {
                    svcEdit.Name,
                    svcEdit.Status,
                    Domain = svcEdit.Network?.ManagedDomain,
                    InternalDomain = svcEdit.Network?.InternalDomain,
                    RepoUrl = svcEdit.Deployment?.Url ?? svcEdit.GitRepo,
                    Branch = svcEdit.Deployment?.Branch ?? svcEdit.GitBranch,
                    CreatedAt = svcEdit.CreatedAt.ToString("u")
                };

                serviceEditDialog = new Dialog(
                    onClose: (Event<Dialog> _) => selectedServiceForEdit.Set((SliplaneService?)null),
                    header: new DialogHeader($"Edit service: {svcEdit.Name}"),
                    body: new DialogBody(detailsModel.ToDetails().RemoveEmpty()),
                    footer: new DialogFooter(
                        new Button("Close").HandleClick(_ => selectedServiceForEdit.Set((SliplaneService?)null)))
                ).Width(Size.Units(220));
            }

            if (selectedServiceForLogs.Value is { } svcLogs)
            {
                object logsBody;
                if (serviceLogsLoading.Value)
                {
                    logsBody = Text.Muted("Loading logs...");
                }
                else if (serviceLogsError.Value is { Length: > 0 })
                {
                    logsBody = new Callout($"Error loading logs: {serviceLogsError.Value}", variant: CalloutVariant.Error);
                }
                else if (serviceLogs.Value == null || serviceLogs.Value.Count == 0)
                {
                    logsBody = Text.Muted("No logs found for this service.");
                }
                else
                {
                    var codeContent = string.Join(
                        Environment.NewLine,
                        serviceLogs.Value
                            .TakeLast(200)
                            .Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm:ss}  {l.Line}")
                    );

                    logsBody = new CodeBlock(codeContent, Languages.Text)
                        .ShowLineNumbers()
                        .ShowCopyButton()
                        .Width(Size.Full())
                        .Height(Size.Units(200));
                }

                serviceLogsDialog = new Dialog(
                    onClose: (Event<Dialog> _) => selectedServiceForLogs.Set((SliplaneService?)null),
                    header: new DialogHeader($"Logs: {svcLogs.Name}"),
                    body: new DialogBody(logsBody),
                    footer: new DialogFooter(
                        new Button("Close").HandleClick(_ => selectedServiceForLogs.Set((SliplaneService?)null)))
                ).Width(Size.Units(260));
            }
        }

        return Layout.Vertical()
            | Text.H2("Projects")
            | projectsBlock
            | projectDetailDialog
            | createServiceDialog
            | serviceEditDialog
            | serviceLogsDialog;
    }
}
