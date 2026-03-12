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

    public string ProjectId { get; set; } = "";

    [Display(Name = "Service name", Order = 3, Prompt = "my-ivy-service")]
    [Required(ErrorMessage = "Enter a service name")]
    [MinLength(2, ErrorMessage = "Service name must be at least 2 characters")]
    public string Name { get; set; } = "";

    // Hidden — filled from draft, not shown in UI
    public string GitRepo { get; set; } = "";
    public string Branch { get; set; } = "main";
    public string DockerfilePath { get; set; } = "Dockerfile";
    public string DockerContext { get; set; } = ".";
    public bool AutoDeploy { get; set; } = true;
    public bool NetworkPublic { get; set; } = true;
    public string NetworkProtocol { get; set; } = "http";
    public string Healthcheck { get; set; } = "/";
    public string? Cmd { get; set; }
}

public class DeployView : ViewBase
{
    private readonly string _apiToken;
    private readonly DeployDraft _draft;
    private readonly string _defaultServerId;
    private readonly string _defaultProjectId;

    public DeployView(string apiToken, DeployDraft draft, string defaultServerId = "", string defaultProjectId = "")
    {
        _apiToken         = apiToken;
        _draft            = draft;
        _defaultServerId  = defaultServerId;
        _defaultProjectId = defaultProjectId;
    }

    public override object? Build()
    {
        var client        = this.UseService<SliplaneApiClient>();
        var refreshSender = this.CreateSignal<SliplaneRefreshSignal, string, Unit>();

        var model = this.UseState(() => new DeployFormModel
        {
            ServerId        = _defaultServerId,
            ProjectId       = _defaultProjectId,
            GitRepo         = _draft.RepoUrl,
            Branch          = string.IsNullOrWhiteSpace(_draft.Branch) ? "main" : _draft.Branch,
            DockerContext   = string.IsNullOrWhiteSpace(_draft.DockerContext) ? "." : _draft.DockerContext,
            DockerfilePath  = string.IsNullOrWhiteSpace(_draft.DockerfilePath) ? "Dockerfile" : _draft.DockerfilePath,
            Name            = DeriveServiceName(_draft.RepoUrl, _draft.DockerContext),
            AutoDeploy      = true,
            NetworkPublic   = true,
            NetworkProtocol = "http",
            Healthcheck     = "/",
        });

        var reloadCounter  = this.UseState(0);

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

        // Warm up the query cache so AsyncSelect has data immediately on first open.
        _ = QueryServers(this.Context, "");
        _ = LookupServer(this.Context, model.Value.ServerId);

        var navigator = this.Context.UseNavigation();
        var (onSubmit, formView, validationView, loading) = this.UseForm(() => model.ToForm("Deploy")
            .Place(m => m.ServerId, m => m.Name)
            .Builder(m => m.ServerId, s => s.ToAsyncSelectInput(QueryServers, LookupServer, placeholder: "Search server..."))
            .Builder(m => m.Name,     s => s.ToTextInput().Placeholder("e.g. yamldotnet"))
            .Remove(m => m.ProjectId, m => m.GitRepo, m => m.Branch, m => m.DockerfilePath, m => m.DockerContext,
                m => m.AutoDeploy, m => m.NetworkPublic, m => m.NetworkProtocol, m => m.Healthcheck, m => m.Cmd)
            .Required(m => m.ProjectId, m => m.Name, m => m.ServerId, m => m.GitRepo));

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
                    env: [], volumeMounts: []));
            await refreshSender.Send("services");
            navigator.Navigate(typeof(SliplaneServicesApp));
        }

        var headerSection = Layout.Vertical().Align(Align.Center).Gap(4)
            | Icons.Rocket.ToIcon()
            | Text.H1("Deploy to Sliplane")
            | Text.Lead("Configure and deploy your Ivy app in seconds.");

        var actionsRow = (Layout.Vertical()
            | (Layout.Horizontal().Align(Align.Center)
                | new Button("Deploy").Icon(Icons.Rocket).Primary().Large().Loading(loading)
                    .OnClick(async _ => await HandleDeploy())
                | validationView));

        var card = new Card(
            Layout.Vertical()
                | headerSection
                | new Separator()
                | formView
                | new Separator()
                | actionsRow)
            .Width(Size.Fraction(0.4f));

        return Layout.Center() | card;
    }

    // Prefer the last segment of dockerContext (e.g. "packages-demos/yamldotnet" → "yamldotnet"),
    // falling back to the last segment of the repo URL.
    private static string DeriveServiceName(string repoUrl, string dockerContext = ".")
    {
        var source = (!string.IsNullOrWhiteSpace(dockerContext) && dockerContext != ".")
            ? dockerContext
            : repoUrl;

        if (string.IsNullOrWhiteSpace(source)) return string.Empty;
        var seg = source.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
        if (seg.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) seg = seg[..^4];
        return string.IsNullOrWhiteSpace(seg) ? string.Empty : seg.ToLowerInvariant();
    }
}
