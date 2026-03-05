namespace SliplaneManage.Apps.Views;

using System.ComponentModel.DataAnnotations;
using SliplaneManage.Models;
using SliplaneManage.Services;
using SliplaneManage.Apps;

public class DeployFormModel
{
    [Display(Name = "Server", Order = 1, Prompt = "Select a server")]
    [Required(ErrorMessage = "Select a server")]
    public string ServerId { get; set; } = "";

    [Display(Name = "Project", Order = 2, Prompt = "Select a project")]
    [Required(ErrorMessage = "Select a project")]
    public string ProjectId { get; set; } = "";

    [Display(Name = "Service name", Order = 3, Prompt = "my-ivy-service")]
    [Required(ErrorMessage = "Enter a service name")]
    [MinLength(2, ErrorMessage = "Service name must be at least 2 characters")]
    public string Name { get; set; } = "";

    [Display(Name = "Repository URL", Order = 4, Prompt = "https://github.com/user/repo")]
    [Required(ErrorMessage = "Enter a repository URL")]
    public string GitRepo { get; set; } = "";

    [Display(Name = "Branch", Order = 5, Prompt = "main")]
    public string Branch { get; set; } = "main";

    [Display(GroupName = "Build", Name = "Dockerfile path", Order = 6, Prompt = "Dockerfile")]
    public string DockerfilePath { get; set; } = "Dockerfile";

    [Display(GroupName = "Build", Name = "Docker context", Order = 7, Prompt = ".")]
    public string DockerContext { get; set; } = ".";

    [Display(GroupName = "Build", Name = "Auto-deploy on push", Order = 8, Prompt = "Enable auto-deploy on push")]
    public bool AutoDeploy { get; set; } = true;

    [Display(GroupName = "Network", Name = "Public access", Order = 9, Prompt = "Expose service publicly")]
    public bool NetworkPublic { get; set; } = true;

    [Display(GroupName = "Network", Name = "Protocol", Order = 10, Prompt = "http or https")]
    public string NetworkProtocol { get; set; } = "http";

    [Display(GroupName = "Optional", Name = "Health check path", Prompt = "/health", Order = 11)]
    public string Healthcheck { get; set; } = "/";

    [Display(GroupName = "Optional", Name = "Start command", Prompt = "e.g. npm start", Order = 12)]
    public string? Cmd { get; set; }
}

public class DeployView : ViewBase
{
    private readonly string _apiToken;
    private readonly string _repoUrl;

    public DeployView(string apiToken, string repoUrl)
    {
        _apiToken = apiToken;
        _repoUrl  = repoUrl;
    }

    public override object? Build()
    {
        var client        = this.UseService<SliplaneApiClient>();
        var draftStore    = this.UseService<DeploymentDraftStore>();
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();

        var initialName = DeriveServiceName(_repoUrl);

        var model = this.UseState(() => new DeployFormModel
        {
            GitRepo = _repoUrl,
            Name    = initialName,
        });

        // Keep DeploymentDraftStore in sync as the user edits the repo URL (per-user)
        this.UseEffect(() => draftStore.SaveRepoUrl(model.Value.GitRepo), model);

        var envList        = this.UseState<List<EnvironmentVariable>>(() => new List<EnvironmentVariable>());
        var showAddEnvDlg  = this.UseState(false);
        var addEnvKey      = this.UseState(string.Empty);
        var addEnvValue    = this.UseState(string.Empty);
        var reloadCounter  = this.UseState(0);
        var serverVolumes  = this.UseState<List<SliplaneVolume>>(() => new List<SliplaneVolume>());
        var volumeMountsList = this.UseState<List<(string VolumeId, string MountPath)>>(() => new List<(string, string)>());
        var showAddVolumeDlg = this.UseState(false);
        var addVolumeId    = this.UseState(string.Empty);
        var addMountPath   = this.UseState(string.Empty);

        QueryResult<Option<string>[]> QueryProjects(IViewContext ctx, string q) =>
            ctx.UseQuery<Option<string>[], (string, string, int)>(
                key: ("deploy-projects", q, reloadCounter.Value),
                fetcher: async _ =>
                    (await client.GetProjectsAsync(_apiToken))
                        .Where(p => string.IsNullOrEmpty(q) || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .Take(20).Select(p => new Option<string>(p.Name, p.Id)).ToArray());

        QueryResult<Option<string>?> LookupProject(IViewContext ctx, string? id) =>
            ctx.UseQuery<Option<string>?, (string, string?, int)>(
                key: ("deploy-project-lookup", id, reloadCounter.Value),
                fetcher: async _ =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var p = (await client.GetProjectsAsync(_apiToken)).FirstOrDefault(x => x.Id == id);
                    return p is null ? null : new Option<string>(p.Name, p.Id);
                });

        QueryResult<Option<string>[]> QueryServers(IViewContext ctx, string q) =>
            ctx.UseQuery<Option<string>[], (string, string, int)>(
                key: ("deploy-servers", q, reloadCounter.Value),
                fetcher: async _ =>
                    (await client.GetServersAsync(_apiToken))
                        .Where(s => string.IsNullOrEmpty(q) || s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .Take(20).Select(s => new Option<string>(s.Name, s.Id)).ToArray());

        QueryResult<Option<string>?> LookupServer(IViewContext ctx, string? id) =>
            ctx.UseQuery<Option<string>?, (string, string?, int)>(
                key: ("deploy-server-lookup", id, reloadCounter.Value),
                fetcher: async _ =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var s = (await client.GetServersAsync(_apiToken)).FirstOrDefault(x => x.Id == id);
                    return s is null ? null : new Option<string>(s.Name, s.Id);
                });

        var protocolOptions = new[] { new Option<string>("HTTP", "http"), new Option<string>("HTTPS", "https") };

        var navigator = this.Context.UseNavigation();
        var (onSubmit, formView, validationView, loading) = this.UseForm(() => model.ToForm("Deploy")
            .Builder(m => m.ProjectId,       s => s.ToAsyncSelectInput(QueryProjects, LookupProject, placeholder: "Search project..."))
            .Builder(m => m.ServerId,        s => s.ToAsyncSelectInput(QueryServers,  LookupServer,  placeholder: "Search server..."))
            .Builder(m => m.NetworkProtocol, s => s.ToSelectInput(protocolOptions))
            .Required(m => m.ProjectId, m => m.Name, m => m.ServerId, m => m.GitRepo));

        this.UseEffect(async () =>
        {
            var serverId = model.Value.ServerId;
            if (string.IsNullOrWhiteSpace(serverId))
            {
                serverVolumes.Set(new List<SliplaneVolume>());
                return;
            }
            try
            {
                var vols = await client.GetServerVolumesAsync(_apiToken, serverId);
                serverVolumes.Set(vols ?? new List<SliplaneVolume>());
            }
            catch
            {
                serverVolumes.Set(new List<SliplaneVolume>());
            }
        }, model);

        async ValueTask HandleDeploy()
        {
            if (!await onSubmit()) return;
            var m   = model.Value;
            await client.CreateServiceAsync(_apiToken, m.ProjectId,
                ServiceRequestFactory.BuildCreateRequest(
                    name: m.Name, serverId: m.ServerId, gitRepo: m.GitRepo,
                    branch: m.Branch, dockerfilePath: m.DockerfilePath,
                    dockerContext: m.DockerContext, autoDeploy: m.AutoDeploy,
                    networkPublic: m.NetworkPublic, networkProtocol: m.NetworkProtocol,
                    cmd: m.Cmd ?? string.Empty, healthcheck: m.Healthcheck,
                    env: envList.Value, volumeMounts: volumeMountsList.Value));
            await refreshSender.Send("services");
            navigator.Navigate(typeof(SliplaneServicesApp));
        }

        // Env variable table
        var envItems = envList.Value;
        var envHeaderRow = new TableRow(
            new TableCell("Key").IsHeader(),
            new TableCell("Value").IsHeader(),
            new TableCell("").IsHeader().Width(Size.Fit()));
        var envDataRows = envItems.Select((e, i) => new TableRow(
            new TableCell(e.Key),
            new TableCell(e.Value ?? ""),
            new TableCell(new Button("Remove").Variant(ButtonVariant.Outline)
                .HandleClick(_ => envList.Set(envList.Value.Where((_, j) => j != i).ToList())))
                .Width(Size.Fit()))).ToArray();

        object envTable = envDataRows.Length == 0
            ? Text.Muted("No variables added.")
            : new Table(new[] { envHeaderRow }.Concat(envDataRows).ToArray()).Width(Size.Full());

        Dialog? addEnvDialog = null;
        if (showAddEnvDlg.Value)
        {
            void SaveEnv()
            {
                if (string.IsNullOrWhiteSpace(addEnvKey.Value)) return;
                envList.Set(envList.Value
                    .Append(new EnvironmentVariable(addEnvKey.Value.Trim(), addEnvValue.Value ?? string.Empty, false))
                    .ToList());
                addEnvKey.Set(string.Empty);
                addEnvValue.Set(string.Empty);
                showAddEnvDlg.Set(false);
            }
            addEnvDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddEnvDlg.Set(false),
                header:  new DialogHeader("Add environment variable"),
                body:    new DialogBody(Layout.Vertical()
                    | addEnvKey.ToTextInput().Placeholder("Key (e.g. DATABASE_URL)")
                    | addEnvValue.ToTextInput().Placeholder("Value")),
                footer:  new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveEnv()),
                    new Button("Cancel").HandleClick(_ => showAddEnvDlg.Set(false))
                )).Width(Size.Units(220));
        }

        Dialog? addVolumeDialog = null;
        if (showAddVolumeDlg.Value)
        {
            void SaveVolume()
            {
                if (string.IsNullOrWhiteSpace(addVolumeId.Value) || string.IsNullOrWhiteSpace(addMountPath.Value)) return;
                volumeMountsList.Set(volumeMountsList.Value
                    .Append((addVolumeId.Value, addMountPath.Value.Trim()))
                    .ToList());
                addVolumeId.Set(string.Empty);
                addMountPath.Set(string.Empty);
                showAddVolumeDlg.Set(false);
            }
            var volumeOptionsForDialog = (serverVolumes.Value ?? new List<SliplaneVolume>())
                .Select(v => new Option<string>($"{v.Name} ({v.MountPath})", v.Id)).ToArray();
            addVolumeDialog = new Dialog(
                onClose: (Event<Dialog> _) => showAddVolumeDlg.Set(false),
                header:  new DialogHeader("Add volume mount"),
                body:    new DialogBody(Layout.Vertical()
                    | addVolumeId.ToSelectInput(volumeOptionsForDialog)
                    | addMountPath.ToTextInput().Placeholder("Mount path (e.g. /data)")),
                footer:  new DialogFooter(
                    new Button("Save").Variant(ButtonVariant.Primary).HandleClick(_ => SaveVolume()),
                    new Button("Cancel").HandleClick(_ => showAddVolumeDlg.Set(false))
                )).Width(Size.Units(220));
        }

        var headerSection = Layout.Vertical().Align(Align.Center).Gap(4)
            | Icons.Rocket.ToIcon()
            | Text.H1("Deploy to Sliplane")
            | Text.Lead("Configure and deploy your Ivy app in seconds.");

        var envSection = new Expandable(
            "Environment Variables",
            Layout.Vertical()
                | envTable
                | new Button("Add variable").Icon(Icons.Plus).Variant(ButtonVariant.Outline)
                    .HandleClick(_ => showAddEnvDlg.Set(true)));

        var vols = serverVolumes.Value ?? new List<SliplaneVolume>();
        var volItems = volumeMountsList.Value ?? new List<(string VolumeId, string MountPath)>();
        var volHeaderRow = new TableRow(
            new TableCell("Volume").IsHeader(),
            new TableCell("Mount path").IsHeader(),
            new TableCell("").IsHeader().Width(Size.Fit()));
        var volDataRows = volItems.Select((v, i) =>
        {
            var index = i;
            var volName = vols.FirstOrDefault(vol => vol.Id == v.VolumeId)?.Name ?? v.VolumeId;
            return new TableRow(
                new TableCell(volName),
                new TableCell(v.MountPath),
                new TableCell(new Button("Remove").Variant(ButtonVariant.Outline)
                    .HandleClick(_ => volumeMountsList.Set(volumeMountsList.Value.Where((_, j) => j != index).ToList())))
                    .Width(Size.Fit()));
        }).ToArray();
        object volTableContent = volDataRows.Length == 0
            ? (object)Text.Muted("No volume mounts. Select a server first, then add.")
            : new Table(new[] { volHeaderRow }.Concat(volDataRows).ToArray()).Width(Size.Full());

        var volumesSection = new Expandable(
            "Volumes",
            Layout.Vertical()
                | volTableContent
                | new Button("Add volume").Icon(Icons.Plus).Variant(ButtonVariant.Outline)
                    .HandleClick(_ => showAddVolumeDlg.Set(true)));

        var actionsRow = Layout.Horizontal()
            | new Button("Deploy").Icon(Icons.Rocket).Primary().Large().Loading(loading)
                .HandleClick(async _ => await HandleDeploy())
            | validationView;

        var card = new Card(
            Layout.Vertical()
                | headerSection
                | new Separator()
                | formView
                | envSection
                | volumesSection
                | actionsRow)
            .Width(Size.Fraction(0.5f));

        var page = Layout.Vertical().Align(Align.TopCenter)
            | card;

        if (addEnvDialog != null && addVolumeDialog != null) return new Fragment(page, addEnvDialog, addVolumeDialog);
        if (addEnvDialog != null) return new Fragment(page, addEnvDialog);
        if (addVolumeDialog != null) return new Fragment(page, addVolumeDialog);
        return page;
    }

    private static string DeriveServiceName(string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoUrl)) return string.Empty;
        var seg = repoUrl.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
        if (seg.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) seg = seg[..^4];
        return string.IsNullOrWhiteSpace(seg) ? string.Empty : seg.ToLowerInvariant();
    }
}
