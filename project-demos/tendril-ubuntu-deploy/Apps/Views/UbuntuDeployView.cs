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
/// Two-step deploy wizard.
/// Step 0 — server + service name  (UseForm lives here)
/// Step 1 — RDP credentials        (UseForm lives in UbuntuCredentialsStepView child component)
///
/// The credentials form is intentionally separated into a child ViewBase so that its
/// UseForm hook is fully isolated from this component's UseForm, preventing Ivy from
/// conflating fields by their hook-call index (IVYHOOK005).
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

        var stepIndex = UseState(0);
        var validationFailed = UseState(false);
        var reloadCounter = UseState(0);

        // deploy result — set by the UbuntuCredentialsStepView child via callback
        var deployedService = UseState<(string ProjectId, SliplaneService Service, string ServerId, string RdpUser, string RdpPassword)?>(() => null);

        // ── Step 0 form (only UseForm in this component) ──────────────────────
        var (onServerSubmit, serverFormView, serverValidationView, serverLoading) = UseForm(() =>
            model.ToForm("Server")
                .Place(m => m.ServerId, m => m.Name)
                .Builder(m => m.ServerId,
                    s => s.ToAsyncSelectInput(QueryServers, LookupServer, placeholder: "Search server…"))
                .Builder(m => m.Name, s => s.ToTextInput().Placeholder("e.g. ubuntu-dev"))
                .Remove(m => m.ProjectId, m => m.GitRepo, m => m.Branch, m => m.DockerfilePath,
                    m => m.DockerContext, m => m.NoVncPort, m => m.VolumeId)
                .Required(m => m.ServerId, m => m.Name));

        // ── Server lookup helpers ─────────────────────────────────────────────
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

        // ── Step 0 advance ────────────────────────────────────────────────────
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

        // ── Stepper ───────────────────────────────────────────────────────────
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

        // ── Step 0 body ───────────────────────────────────────────────────────
        var welcomeCallout = new Callout(
            Layout.Vertical().Gap(3)
                | Text.Block("This wizard deploys a full Ubuntu 22.04 Desktop environment to Sliplane. "
                    + "You will get an XFCE4 desktop with JetBrains Rider and Ivy Tendril pre-installed, "
                    + "accessible via your browser (noVNC) or any RDP client.")
                | Text.Block("The Docker image build takes 5–10 minutes on first deploy.").Bold(),
            "One-click Ubuntu Desktop",
            CalloutVariant.Info);

        var step0Body = Layout.Vertical().Gap(4).Width(Size.Full())
            | welcomeCallout
            | serverFormView
            | (validationFailed.Value
                ? (object)new Callout(serverValidationView, "Please fix the following", CalloutVariant.Error)
                : new Empty());

        var step0Footer = Layout.Vertical().Width(Size.Full()).AlignContent(Align.Center)
            | new Button("Next")
                .Icon(Icons.ChevronRight, Align.Right)
                .Primary().Large().BorderRadius(BorderRadius.Full)
                .Width(Size.Full())
                .Loading(serverLoading)
                .Disabled(serverLoading)
                .OnClick(async _ => await AdvanceToStep1());

        // ── Step 1: credentials child component ───────────────────────────────
        // Rendered as a child ViewBase → owns its own UseForm hooks independently.
        var step1Component = new UbuntuCredentialsStepView(
            apiToken: _apiToken,
            serverModel: model.Value,
            onBack: () => { stepIndex.Set(0); return ValueTask.CompletedTask; },
            onDeployed: (projectId, serverId, rdpUser, rdpPassword, service) =>
            {
                deployedService.Set((projectId, service, serverId, rdpUser, rdpPassword));
                return ValueTask.CompletedTask;
            });

        // ── Assemble page ─────────────────────────────────────────────────────
        object titleBlock = stepIndex.Value == 0
            ? (Layout.Vertical().Gap(2).AlignContent(Align.Center)
                | Text.H1("Ubuntu Desktop on Sliplane").Align(TextAlignment.Center))
            : (Layout.Vertical().Gap(2).AlignContent(Align.Center)
                | Text.H1("Set your RDP credentials").Align(TextAlignment.Center));

        object pageContent;
        if (deployedService.Value is { } deployed)
        {
            // Show status view below the (frozen) stepper
            pageContent = Layout.Vertical().Width(Size.Full()).Gap(4)
                | new Stepper(OnStepperSelect, 1, stepperItems).Width(Size.Full())
                | titleBlock
                | new UbuntuDeployStatusView(
                    _apiToken,
                    deployed.ProjectId,
                    deployed.Service,
                    deployed.RdpUser,
                    deployed.RdpPassword,
                    deployed.ServerId);
        }
        else if (stepIndex.Value == 0)
        {
            pageContent = Layout.Vertical().Width(Size.Full()).Gap(4).AlignContent(Align.Stretch)
                | new Stepper(OnStepperSelect, 0, stepperItems).Width(Size.Full())
                | titleBlock
                | step0Body
                | step0Footer;
        }
        else
        {
            pageContent = Layout.Vertical().Width(Size.Full()).Gap(4).AlignContent(Align.Stretch)
                | new Stepper(OnStepperSelect, 1, stepperItems).Width(Size.Full())
                | titleBlock
                | step1Component;
        }

        var manageFloat = new FloatingPanel(
            new Button("Manage services")
                .Link().Url("https://ivy-sliplane-management.sliplane.app/")
                .Outline().Large().BorderRadius(BorderRadius.Full),
            Align.BottomRight).Offset(new Thickness(0, 0, 20, 10));

        return new Fragment(
            Layout.TopCenter()
                .Padding(new Thickness(0, 12, 0, 0))
                | (Layout.Vertical()
                    .Width(Size.Fraction(0.52f))
                    .Gap(2)
                    .Padding(new Thickness(16, 16, 16, 16))
                    .AlignContent(Align.Stretch)
                    | pageContent),
            manageFloat);
    }
}
