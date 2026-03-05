namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Services;

/// <summary>
/// Route: /sliplane-deploy-app
/// Opened via the "Host your Ivy app on Sliplane" button.
/// ?repo= is captured by RepoCaptureFilter, parsed into a DeployDraft, and pre-fills the form.
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

        var draftStore = this.UseService<DeploymentDraftStore>();
        var args       = this.UseArgs<DeployArgs>();

        // Args from internal navigation take priority over the stored draft
        var draft = args is not null
            ? DeploymentDraftStore.ParseGitHubUrl(args.Repo)
            : draftStore.LastDraft;

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                | (Layout.Vertical().Align(Align.Center).Gap(6)
                    | Icons.Rocket.ToIcon()
                    | Text.H2("Deploy to Sliplane")
                    | (draft is not null
                        ? Text.Muted($"Repository: {draft.RepoUrl}")
                        : Text.Muted("Sign in with Sliplane to deploy your Ivy app."))
                    | Text.Muted("No API token. Please sign in or configure Sliplane:ApiToken."));
        }

        return new DeployView(apiToken, draft ?? new DeployDraft(string.Empty));
    }
}

/// <summary>Arguments for internal Ivy navigation to SliplaneDeployApp.</summary>
public record DeployArgs(string Repo);
