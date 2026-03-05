namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Services;

/// <summary>
/// Route: /sliplane-deploy-app
/// Opened via the "Host your Ivy app on Sliplane" button in GitHub READMEs.
/// The ?repo= query param is captured by RepoCaptureFilter before Ivy SPA loads,
/// stored in DeploymentDraftStore, and read here to pre-fill the deploy form.
/// </summary>
[App(
    id: "sliplane-deploy-app",
    icon: Icons.Rocket,
    title: "Deploy on Sliplane",
    searchHints: ["deploy", "host", "sliplane"],
    isVisible: true)]
public class SliplaneDeployApp : ViewBase
{
    public override object? Build()
    {
        var config   = this.UseService<IConfiguration>();
        var auth     = this.UseService<IAuthService>();
        var session  = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        // Repo from internal navigation args or last saved value (per-user)
        var draftStore = this.UseService<DeploymentDraftStore>();
        var args    = this.UseArgs<DeployArgs>();
        var repoUrl = args?.Repo ?? draftStore.LastRepoUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                | (Layout.Vertical().Align(Align.Center).Gap(6)
                    | Icons.Rocket.ToIcon()
                    | Text.H2("Deploy to Sliplane")
                    | (string.IsNullOrWhiteSpace(repoUrl)
                        ? Text.Muted("Sign in with Sliplane to deploy your Ivy app.")
                        : Text.Muted($"Repository: {repoUrl}"))
                    | Text.Muted("No API token. Please sign in or configure Sliplane:ApiToken."));
        }

        return new DeployView(apiToken, repoUrl);
    }
}

/// <summary>Arguments for internal Ivy navigation to SliplaneDeployApp.</summary>
public record DeployArgs(string Repo);
