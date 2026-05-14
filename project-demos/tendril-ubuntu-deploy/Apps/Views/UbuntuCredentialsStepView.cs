namespace TendrilUbuntuDeploy.Apps.Views;

using TendrilUbuntuDeploy.Models;
using TendrilUbuntuDeploy.Services;

/// <summary>
/// Step 1 of the Ubuntu deploy wizard — collects RDP credentials and triggers the deploy.
/// Lives in its own ViewBase so its UseForm hook is completely isolated from the
/// server-selection form in UbuntuDeployView (fixes Ivy hook-index collision).
/// </summary>
public class UbuntuCredentialsStepView : ViewBase
{
    private readonly string _apiToken;
    private readonly UbuntuDeployFormModel _serverModel;
    private readonly Func<ValueTask> _onBack;
    private readonly Func<string, string, string, string, SliplaneService, ValueTask> _onDeployed;
    // _onDeployed(projectId, serverId, rdpUser, rdpPassword, service)

    public UbuntuCredentialsStepView(
        string apiToken,
        UbuntuDeployFormModel serverModel,
        Func<ValueTask> onBack,
        Func<string, string, string, string, SliplaneService, ValueTask> onDeployed)
    {
        _apiToken = apiToken;
        _serverModel = serverModel;
        _onBack = onBack;
        _onDeployed = onDeployed;
    }

    public override object? Build()
    {
        var client = UseService<SliplaneApiClient>();

        var credModel = UseState(() => new UbuntuRdpCredentialsModel());
        var isDeploying = UseState(false);
        var deployError = UseState<string?>(() => null);
        var validationFailed = UseState(false);

        var (onCredSubmit, credFormView, credValidationView, credLoading) = UseForm(() =>
            credModel.ToForm("RDP Credentials")
                .Builder(m => m.Password, s => s.ToPasswordInput(placeholder: "At least 8 characters"))
                .Required(m => m.Username, m => m.Password));

        async ValueTask HandleDeploy()
        {
            validationFailed.Set(false);
            if (!await onCredSubmit())
            {
                validationFailed.Set(true);
                return;
            }

            deployError.Set(null);
            isDeploying.Set(true);
            try
            {
                var m = _serverModel;
                var creds = credModel.Value;

                // Ensure "Ivy" project exists
                var projects = await client.GetProjectsAsync(_apiToken);
                var project = projects.FirstOrDefault(p => p.Name.Equals("Ivy", StringComparison.OrdinalIgnoreCase))
                              ?? await client.CreateProjectAsync(_apiToken, "Ivy");
                if (project == null)
                    throw new InvalidOperationException("Could not find or create an Ivy project on Sliplane.");

                // Auto-create volume if not supplied
                var volumeId = m.VolumeId?.Trim();
                if (string.IsNullOrWhiteSpace(volumeId))
                {
                    var vol = await client.CreateVolumeAsync(
                        _apiToken, m.ServerId, ServiceRequestFactory.AutoVolumeName(m.Name));
                    volumeId = vol.Id;
                }

                var port = string.IsNullOrWhiteSpace(m.NoVncPort) ? "8080" : m.NoVncPort.Trim();
                var homeMount = $"/home/{creds.Username}";

                var envVars = new List<EnvironmentVariable>
                {
                    new("PORT",         port,           Secret: false),
                    new("RDP_USER",     creds.Username, Secret: false),
                    new("RDP_PASSWORD", creds.Password, Secret: true),
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

                await _onDeployed(project.Id, m.ServerId, creds.Username, creds.Password, service);
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

        var hintCallout = new Callout(
            Text.Markdown(
                "These become the **Linux user account** inside the Ubuntu container. "
                + "Use them to log in via noVNC (browser) or any RDP client. "
                + "**RDP_PASSWORD** is stored as a Sliplane secret."),
            "Credentials",
            CalloutVariant.Info);

        var body = Layout.Vertical().Gap(4).Width(Size.Full())
            | hintCallout
            | credFormView
            | (validationFailed.Value
                ? new Callout(credValidationView, "Please fix the following", CalloutVariant.Error)
                : new Empty());

        if (isDeploying.Value)
            body = body | new Callout(
                Layout.Vertical().Gap(3)
                    | Text.Block("Creating the Ubuntu Desktop service on Sliplane…").Bold()
                    | new Progress().Indeterminate().Goal("Please wait…"),
                "Deploying", CalloutVariant.Info);

        if (deployError.Value != null)
            body = body | new Callout(deployError.Value, variant: CalloutVariant.Error);

        var footer = Layout.Horizontal().Width(Size.Full()).Gap(4)
            | new Button("Back")
                .Icon(Icons.ChevronLeft)
                .Variant(ButtonVariant.Outline).Large().BorderRadius(BorderRadius.Full)
                .Width(Size.Fraction(0.31f))
                .OnClick(async _ => await _onBack())
            | new Spacer()
            | new Button("Deploy")
                .Icon(Icons.Rocket, Align.Right)
                .Primary().Large().BorderRadius(BorderRadius.Full)
                .Width(Size.Fraction(0.31f))
                .Loading(credLoading || isDeploying.Value)
                .Disabled(credLoading || isDeploying.Value)
                .OnClick(async _ => await HandleDeploy());

        return Layout.Vertical().Width(Size.Full()).Gap(4) | body | footer;
    }
}
