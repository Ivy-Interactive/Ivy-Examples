namespace SliplaneDeploy.Apps.Views;

using SliplaneDeploy.Models;
using SliplaneDeploy.Services;

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
        _apiToken = apiToken;
        _draft = draft;
        _defaultServerId = defaultServerId;
        _defaultProjectId = defaultProjectId;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var config = this.UseService<IConfiguration>();
        var dockerfileResolver = this.UseService<GitHubDockerfilePathResolver>();
        var model = this.UseState(() => new DeployFormModel
        {
            ServerId = _defaultServerId,
            ProjectId = _defaultProjectId,
            GitRepo = _draft.RepoUrl,
            Branch = string.IsNullOrWhiteSpace(_draft.Branch) ? "main" : _draft.Branch,
            DockerContext = string.IsNullOrWhiteSpace(_draft.DockerContext) ? "." : _draft.DockerContext,
            DockerfilePath = string.IsNullOrWhiteSpace(_draft.DockerfilePath) ? "Dockerfile" : _draft.DockerfilePath,
            Name = DeriveServiceName(_draft.RepoUrl, _draft.DockerContext),
            AutoDeploy = true,
            NetworkPublic = true,
            NetworkProtocol = "http",
            Healthcheck = "/",
        });

        var reloadCounter = this.UseState(0);
        var deployedService = this.UseState<(string ProjectId, SliplaneService Service)?>(() => null);
        var deployError = this.UseState<string?>(() => null);
        var validationFailed = this.UseState(false);
        var isDeploying = this.UseState(false);

        var (onSubmit, formView, validationView, loading) = this.UseForm(() => model.ToForm("Deploy")
            .Place(m => m.ServerId, m => m.Name)
            .Builder(m => m.ServerId, s => s.ToAsyncSelectInput(QueryServers, LookupServer, placeholder: "Search server..."))
            .Builder(m => m.Name, s => s.ToTextInput().Placeholder("e.g. yamldotnet"))
            .Remove(m => m.ProjectId, m => m.GitRepo, m => m.Branch, m => m.DockerfilePath, m => m.DockerContext,
                m => m.AutoDeploy, m => m.NetworkPublic, m => m.NetworkProtocol, m => m.Healthcheck, m => m.Cmd)
            .Required(m => m.ProjectId, m => m.Name, m => m.ServerId, m => m.GitRepo));

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

        _ = QueryServers(this.Context, "");
        _ = LookupServer(this.Context, model.Value.ServerId);

        async ValueTask HandleDeploy()
        {
            string? noErr = null;
            (string ProjectId, SliplaneService Service)? noSvc = null;
            deployError.Set(noErr);
            deployedService.Set(noSvc);
            validationFailed.Set(false);
            if (!await onSubmit())
            {
                validationFailed.Set(true);
                return;
            }

            var m = model.Value;
            isDeploying.Set(true);
            try
            {
                var resolution = await dockerfileResolver.ResolveAsync(
                    m.GitRepo, m.Branch, m.DockerfilePath, m.DockerContext);
                var envVars = resolution.AdditionalEnv?.ToList() ?? [];
                var service = await client.CreateServiceAsync(_apiToken, m.ProjectId,
                    ServiceRequestFactory.BuildCreateRequest(
                        name: m.Name, serverId: m.ServerId, gitRepo: m.GitRepo,
                        branch: m.Branch, dockerfilePath: resolution.DockerfilePath,
                        dockerContext: resolution.DockerContext, autoDeploy: m.AutoDeploy,
                        networkPublic: m.NetworkPublic, networkProtocol: m.NetworkProtocol,
                        cmd: m.Cmd ?? string.Empty, healthcheck: m.Healthcheck,
                        env: envVars, volumeMounts: []));

                if (service != null)
                    deployedService.Set((m.ProjectId, service));
            }
            catch (Exception ex)
            {
                deployError.Set(ex.Message);
            }
            finally
            {
                isDeploying.Set(false);
            }
        }

        var headerSection = Layout.Vertical().AlignContent(Align.Center).Gap(4)
            | Text.H1("Deploy to Sliplane")
            | Text.Lead("Configure and deploy your Ivy app in seconds.");

        var actionsRow = Layout.Vertical()
            | (Layout.Horizontal().AlignContent(Align.Center)
                | new Button("Deploy").Primary().Large()
                    .Loading(loading || isDeploying.Value).Disabled(loading || isDeploying.Value)
                    .Width(Size.Fraction(0.5f))
                    .OnClick(async _ => await HandleDeploy()))
            | (validationFailed.Value
                ? new Callout(validationView, "Please fix the following", CalloutVariant.Error)
                : validationView);

        var cardContent = Layout.Vertical()
            | headerSection
            | new Separator()
            | formView
            | new Spacer()
            | actionsRow;

        if (isDeploying.Value && deployedService.Value == null)
        {
            cardContent = cardContent
                | new Separator()
                | new Callout(
                    Layout.Vertical()
                        | Text.Block("Creating service on Sliplane…").Bold()
                        | new Progress().Indeterminate().Goal("Please wait…"),
                    "Deploying",
                    CalloutVariant.Info);
        }
        else if (deployedService.Value is { } deployed)
        {
            cardContent = cardContent
                | new Separator()
                | new DeployStatusView(_apiToken, deployed.ProjectId, deployed.Service);
        }

        if (deployError.Value != null)
        {
            cardContent = cardContent
                | new Callout(deployError.Value, variant: CalloutVariant.Error);
        }

        var card = new Card(cardContent).Width(Size.Fraction(0.35f));
        var manageServicesUrl = config["Sliplane:ManageServicesUrl"]?.Trim();
        if (string.IsNullOrEmpty(manageServicesUrl))
            manageServicesUrl = "https://ivy-sliplane-management.sliplane.app/";
        var manageBtn = new Button("Manage services")
            .Link().Url(manageServicesUrl)
            .Outline().Large().BorderRadius(BorderRadius.Full);
        var manageFloat = new FloatingPanel(manageBtn, Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        return new Fragment(Layout.Center() | card, manageFloat);
    }

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
