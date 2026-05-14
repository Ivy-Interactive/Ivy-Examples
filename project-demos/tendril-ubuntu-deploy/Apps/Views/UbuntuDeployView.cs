namespace TendrilUbuntuDeploy.Apps.Views;

using TendrilUbuntuDeploy.Apps;
using TendrilUbuntuDeploy.Models;
using TendrilUbuntuDeploy.Services;

// ─── Form models ──────────────────────────────────────────────────────────────

public class UbuntuDeployFormModel
{
    [Display(Name = "Server", Order = 1, Prompt = "Select a server")]
    [Required(ErrorMessage = "Select a server")]
    public string ServerId { get; set; } = "";

    public string ProjectId { get; set; } = "";

    [Display(Name = "Service name", Order = 2, Prompt = "e.g. ubuntu-dev")]
    [Required(ErrorMessage = "Enter a service name")]
    [MinLength(2, ErrorMessage = "Service name must be at least 2 characters")]
    public string Name { get; set; } = "ubuntu-dev";

    // Hidden git settings (pre-filled from defaults)
    public string GitRepo { get; set; } = UbuntuDeployDefaults.DefaultRepoUrl;
    public string Branch { get; set; } = UbuntuDeployDefaults.DefaultBranch;
    public string DockerfilePath { get; set; } = UbuntuDeployDefaults.DefaultDockerfilePath;
    public string DockerContext { get; set; } = UbuntuDeployDefaults.DefaultDockerContext;

    [Display(Name = "noVNC port (PORT)", Order = 3)]
    public string NoVncPort { get; set; } = UbuntuDeployDefaults.DefaultNoVncPort;

    [Display(Name = "Volume ID (optional)", Order = 4,
        Prompt = "Sliplane persistent volume — mounted at /home/<user>")]
    public string? VolumeId { get; set; }
}

public class UbuntuRdpCredentialsModel
{
    [Display(Name = "Username", Prompt = "e.g. developer")]
    [Required(ErrorMessage = "Username is required")]
    [RegularExpression(@"^[a-z_][a-z0-9_-]{0,30}$",
        ErrorMessage = "Lowercase letters, digits, - and _ only; must start with a letter or _")]
    public string Username { get; set; } = UbuntuDeployDefaults.DefaultRdpUser;

    [Display(Name = "Password")]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = "";
}

// ─── View ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Two-step deploy wizard:
/// Step 0 — server + service name
/// Step 1 — RDP credentials
/// → Deploy button → status view
/// </summary>
public class UbuntuDeployView : ViewBase
{
    private readonly string _apiToken;
    private readonly string _defaultServerId;
    private readonly string _defaultProjectId;

    public UbuntuDeployView(string apiToken, string defaultServerId = "", string defaultProjectId = "")
    {
        _apiToken = apiToken;
        _defaultServerId = defaultServerId;
        _defaultProjectId = defaultProjectId;
    }

    public override object? Build()
    {
        var client = UseService<SliplaneApiClient>();

        var model = UseState(() => new UbuntuDeployFormModel
        {
            ServerId = _defaultServerId,
            ProjectId = _defaultProjectId,
        });
        var credModel = UseState(() => new UbuntuRdpCredentialsModel());

        var stepIndex = UseState(0);
        var validationFailed = UseState(false);
        var credValidationFailed = UseState(false);
        var isDeploying = UseState(false);
        var deployError = UseState<string?>(() => null);
        var deployedService = UseState<(string ProjectId, SliplaneService Service, string ServerId)?>(() => null);
        var reloadCounter = UseState(0);

        // ── Forms (hooks must precede other Build() statements; see IVYHOOK005) ─
        var (onServerSubmit, serverFormView, serverValidationView, serverLoading) = UseForm(() =>
            model.ToForm("Deploy")
                .Place(m => m.ServerId, m => m.Name)
                .Builder(m => m.ServerId,
                    s => s.ToAsyncSelectInput(QueryServers, LookupServer, placeholder: "Search server…"))
                .Builder(m => m.Name, s => s.ToTextInput().Placeholder("e.g. ubuntu-dev"))
                .Remove(m => m.ProjectId, m => m.GitRepo, m => m.Branch, m => m.DockerfilePath,
                    m => m.DockerContext, m => m.NoVncPort, m => m.VolumeId)
                .Required(m => m.ServerId, m => m.Name));

        var (onCredSubmit, credFormView, credValidationView, credLoading) = UseForm(() =>
            credModel.ToForm("Credentials")
                .Builder(m => m.Password, s => s.ToPasswordInput(placeholder: "At least 8 characters"))
                .Required(m => m.Username, m => m.Password));

        // ── Server lookup for async select ────────────────────────────────────
        QueryResult<Option<string>[]> QueryServers(IViewContext ctx, string q) =>
            ctx.UseQuery<Option<string>[], (string, string, int)>(
                key: ("ubuntu-servers", q, reloadCounter.Value),
                fetcher: async _ =>
                    (await client.GetServersAsync(_apiToken))
                        .Where(s => string.IsNullOrEmpty(q) || s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                        .Take(20)
                        .Select(s => new Option<string>(s.Name, s.Id))
                        .ToArray());

        QueryResult<Option<string>?> LookupServer(IViewContext ctx, string? id) =>
            ctx.UseQuery<Option<string>?, (string, string?, int)>(
                key: ("ubuntu-server-lookup", id, reloadCounter.Value),
                fetcher: async _ =>
                {
                    if (string.IsNullOrEmpty(id)) return null;
                    var s = (await client.GetServersAsync(_apiToken)).FirstOrDefault(x => x.Id == id);
                    return s is null ? null : new Option<string>(s.Name, s.Id);
                });

        _ = QueryServers(Context, "");
        _ = LookupServer(Context, model.Value.ServerId);

        // ── Step handlers ─────────────────────────────────────────────────────
        async ValueTask AdvanceToStep1()
        {
            validationFailed.Set(false);
            if (!await onServerSubmit())
            {
                validationFailed.Set(true);
                return;
            }
            stepIndex.Set(1);
        }

        async ValueTask HandleDeploy()
        {
            credValidationFailed.Set(false);
            if (!await onCredSubmit())
            {
                credValidationFailed.Set(true);
                return;
            }

            deployError.Set(null);
            isDeploying.Set(true);
            try
            {
                var m = model.Value;
                var creds = credModel.Value;

                // Ensure project exists or create one
                var projects = await client.GetProjectsAsync(_apiToken);
                var project = projects.FirstOrDefault(p => p.Name.Equals("Ivy", StringComparison.OrdinalIgnoreCase))
                              ?? await client.CreateProjectAsync(_apiToken, "Ivy");
                if (project == null)
                    throw new InvalidOperationException("Could not find or create an Ivy project on Sliplane.");

                // Auto-create volume if not provided
                var volumeId = m.VolumeId?.Trim();
                if (string.IsNullOrWhiteSpace(volumeId))
                {
                    var vol = await client.CreateVolumeAsync(_apiToken, m.ServerId, ServiceRequestFactory.AutoVolumeName(m.Name));
                    volumeId = vol.Id;
                }

                var port = string.IsNullOrWhiteSpace(m.NoVncPort) ? "8080" : m.NoVncPort.Trim();
                var homeMount = $"/home/{creds.Username}";

                var envVars = new List<EnvironmentVariable>
                {
                    new("PORT",         port,             Secret: false),
                    new("RDP_USER",     creds.Username,   Secret: false),
                    new("RDP_PASSWORD", creds.Password,   Secret: true),
                };

                List<(string, string)>? volumes = null;
                if (!string.IsNullOrWhiteSpace(volumeId))
                    volumes = [(volumeId, homeMount)];

                var service = await client.CreateServiceAsync(_apiToken, project.Id,
                    ServiceRequestFactory.BuildCreateRequest(
                        name: m.Name,
                        serverId: m.ServerId,
                        gitRepo: m.GitRepo,
                        branch: m.Branch,
                        dockerfilePath: m.DockerfilePath,
                        dockerContext: m.DockerContext,
                        autoDeploy: true,
                        networkPublic: true,
                        networkProtocol: "http",
                        healthcheck: "/",
                        env: envVars,
                        volumeMounts: volumes));

                if (service == null)
                    throw new InvalidOperationException("Sliplane returned an empty response after service creation.");

                deployedService.Set((project.Id, service, m.ServerId));
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

        // ── Stepper items ─────────────────────────────────────────────────────
        var stepperItems = new[]
        {
            new StepperItem("1", stepIndex.Value > 0 ? Icons.Check : null, "Welcome",     "Server & name"),
            new StepperItem("2", deployedService.Value != null ? Icons.Check : null, "Credentials", "RDP login"),
        };

        ValueTask OnStepperSelect(Event<Stepper, int> e)
        {
            if (e.Value < stepIndex.Value)
                stepIndex.Set(e.Value);
            return ValueTask.CompletedTask;
        }

        // ── Step content ──────────────────────────────────────────────────────
        var welcomeCallout = new Callout(
            Layout.Vertical().Gap(3)
                | Text.Block("This wizard deploys a full Ubuntu 22.04 Desktop environment to Sliplane. "
                    + "You will get an XFCE4 desktop with JetBrains Rider and Ivy Tendril pre-installed, "
                    + "accessible via your browser (noVNC) or any RDP client.")
                | Text.Block("The Docker image build takes 5–10 minutes on first deploy.").Bold(),
            "One-click Ubuntu Desktop",
            CalloutVariant.Info);

        object titleBlock = stepIndex.Value == 0
            ? (Layout.Vertical().Gap(2).AlignContent(Align.Center)
                | Text.H1("Ubuntu Desktop on Sliplane").Align(TextAlignment.Center))
            : (Layout.Vertical().Gap(2).AlignContent(Align.Center)
                | Text.H1("Set your RDP credentials").Align(TextAlignment.Center));

        var credHintCallout = new Callout(
            Text.Markdown(
                "These become the **Linux user account** inside the Ubuntu container. "
                + "Use them to log in via noVNC (browser) or any RDP client. "
                + "**RDP_PASSWORD** is stored as a Sliplane secret."),
            "Credentials",
            CalloutVariant.Info);

        object stepBody = stepIndex.Value == 0
            ? (Layout.Vertical().Gap(4).Width(Size.Full())
                | welcomeCallout
                | serverFormView
                | (validationFailed.Value
                    ? (object)new Callout(serverValidationView, "Please fix the following", CalloutVariant.Error)
                    : new Empty()))
            : (Layout.Vertical().Gap(4).Width(Size.Full())
                | credHintCallout
                | credFormView
                | (credValidationFailed.Value
                    ? (object)new Callout(credValidationView, "Please fix the following", CalloutVariant.Error)
                    : new Empty()));

        object footerRow = stepIndex.Value == 0
            ? (object)(Layout.Vertical().Width(Size.Full()).AlignContent(Align.Center)
                | new Button("Next")
                    .Icon(Icons.ChevronRight, Align.Right)
                    .Primary().Large().BorderRadius(BorderRadius.Full)
                    .Width(Size.Full())
                    .Loading(serverLoading)
                    .Disabled(serverLoading)
                    .OnClick(async _ => await AdvanceToStep1()))
            : (Layout.Horizontal().Width(Size.Full()).Gap(4)
                | new Button("Back")
                    .Icon(Icons.ChevronLeft)
                    .Variant(ButtonVariant.Outline).Large().BorderRadius(BorderRadius.Full)
                    .Width(Size.Fraction(0.31f))
                    .OnClick(_ => { stepIndex.Set(0); return ValueTask.CompletedTask; })
                | new Spacer()
                | new Button("Deploy")
                    .Icon(Icons.Rocket, Align.Right)
                    .Primary().Large().BorderRadius(BorderRadius.Full)
                    .Width(Size.Fraction(0.31f))
                    .Loading(credLoading || isDeploying.Value)
                    .Disabled(credLoading || isDeploying.Value)
                    .OnClick(async _ => await HandleDeploy()));

        var mainFlow = Layout.Vertical().Width(Size.Full()).Gap(4).AlignContent(Align.Stretch)
            | new Stepper(OnStepperSelect, stepIndex.Value, stepperItems).Width(Size.Full())
            | titleBlock
            | stepBody
            | footerRow;

        var pageBody = mainFlow;

        if (isDeploying.Value && deployedService.Value == null)
        {
            pageBody = pageBody
                | new Callout(
                    Layout.Vertical().Gap(3)
                        | Text.Block("Creating the Ubuntu Desktop service on Sliplane…").Bold()
                        | new Progress().Indeterminate().Goal("Please wait…"),
                    "Deploying",
                    CalloutVariant.Info);
        }
        else if (deployedService.Value is { } deployed)
        {
            pageBody = pageBody
                | new UbuntuDeployStatusView(
                    _apiToken,
                    deployed.ProjectId,
                    deployed.Service,
                    credModel.Value.Username,
                    credModel.Value.Password,
                    deployed.ServerId);
        }

        if (deployError.Value != null)
            pageBody = pageBody | new Callout(deployError.Value, variant: CalloutVariant.Error);

        var manageServicesUrl = "https://ivy-sliplane-management.sliplane.app/";
        var manageFloat = new FloatingPanel(
            new Button("Manage services").Link().Url(manageServicesUrl).Outline().Large().BorderRadius(BorderRadius.Full),
            Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        return new Fragment(
            Layout.TopCenter()
                .Padding(new Thickness(0, 12, 0, 0))
                | (Layout.Vertical()
                    .Width(Size.Fraction(0.52f))
                    .Gap(2)
                    .Padding(new Thickness(16, 16, 16, 16))
                    .AlignContent(Align.Stretch)
                    | pageBody),
            manageFloat);
    }
}
