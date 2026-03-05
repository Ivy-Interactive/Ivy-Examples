namespace SliplaneManage.Apps;

using SliplaneManage.Apps.Views;
using SliplaneManage.Services;
using SliplaneManage.Models;

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

        // Args from internal navigation take priority; draft is consumed once (one-shot pre-fill)
        var draft = args is not null
            ? DeploymentDraftStore.ParseGitHubUrl(args.Repo)
            : draftStore.ReadAndClearDraft();

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

        var client = this.UseService<SliplaneApiClient>();
        var firstServerQuery = this.UseQuery<SliplaneServer?, (string, string)>(
            key: ("deploy-default-server", apiToken),
            fetcher: async (_, ct) => (await client.GetServersAsync(apiToken)).FirstOrDefault());
        var firstProjectQuery = this.UseQuery<SliplaneProject?, (string, string)>(
            key: ("deploy-default-project", apiToken),
            fetcher: async (_, ct) => (await client.GetProjectsAsync(apiToken)).FirstOrDefault());

        // Pre-fill server/project only when we came from the deploy button (draft present).
        // On refresh or opening Deploy from sidebar without repo → draft is empty → form starts blank.
        var defaultServerId  = draft is not null ? (firstServerQuery.Value?.Id  ?? "") : "";
        var defaultProjectId = draft is not null ? (firstProjectQuery.Value?.Id ?? "") : "";

        return new DeployView(apiToken, draft ?? new DeployDraft(string.Empty), defaultServerId, defaultProjectId);
    }
}

/// <summary>Arguments for internal Ivy navigation to SliplaneDeployApp.</summary>
public record DeployArgs(string Repo);
