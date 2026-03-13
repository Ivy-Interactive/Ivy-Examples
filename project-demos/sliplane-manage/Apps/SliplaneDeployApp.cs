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
    icon: Icons.Rocket,
    title: "Deploy on Sliplane",
    isVisible: false)]
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

        // UseState ensures we read the draft once per app instance; LastDraft never clears (data always saved).
        var draftState = this.UseState<DeployDraft?>(() =>
            args is not null
                ? DeploymentDraftStore.ParseGitHubUrl(args.Repo)
                : draftStore.LastDraft);
        var draft = draftState.Value;

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

        // When user came from deploy button (draft present), ensure project "Ivy" exists and use it.
        var needIvyProject = draft is not null;
        var ivyProjectQuery = this.UseQuery<SliplaneProject?, (string, bool)>(
            key: (apiToken, needIvyProject),
            fetcher: async (key, ct) =>
            {
                var (token, needIvy) = key;
                if (!needIvy) return null;
                var projects = await client.GetProjectsAsync(token);
                var ivy = projects.FirstOrDefault(p => p.Name.Equals("Ivy", StringComparison.OrdinalIgnoreCase));
                return ivy ?? await client.CreateProjectAsync(token, "Ivy");
            });

        var firstProjectQuery = this.UseQuery<SliplaneProject?, (string, string)>(
            key: ("deploy-default-project", apiToken),
            fetcher: async (_, ct) => (await client.GetProjectsAsync(apiToken)).FirstOrDefault());

        // When draft present: use Ivy project (created if missing). Otherwise use first project.
        var ivyProject   = needIvyProject ? ivyProjectQuery.Value : null;
        var preServerId  = firstServerQuery.Value?.Id ?? "";
        var preProjectId = (needIvyProject ? ivyProject?.Id : firstProjectQuery.Value?.Id) ?? "";

        var serverLookupPreload = this.UseQuery<Option<string>?, (string, string?, int)>(
            key: ("deploy-server-lookup", string.IsNullOrEmpty(preServerId) ? null : preServerId, 0),
            fetcher: async _ => string.IsNullOrEmpty(preServerId)
                ? null
                : new Option<string>(firstServerQuery.Value!.Name, preServerId));

        var projectLookupPreload = this.UseQuery<Option<string>?, (string, string?, int)>(
            key: ("deploy-project-lookup", string.IsNullOrEmpty(preProjectId) ? null : preProjectId, 0),
            fetcher: async _ => string.IsNullOrEmpty(preProjectId)
                ? null
                : new Option<string>((needIvyProject ? ivyProject?.Name : firstProjectQuery.Value?.Name) ?? "Ivy", preProjectId));

        var needDefaults     = draft is not null;
        var serversReady     = !firstServerQuery.Loading   || firstServerQuery.Value   != null;
        var projectsReady    = needIvyProject
            ? (!ivyProjectQuery.Loading || ivyProjectQuery.Value != null)
            : (!firstProjectQuery.Loading || firstProjectQuery.Value != null);
        var serverLkpReady   = string.IsNullOrEmpty(preServerId)  || !serverLookupPreload.Loading  || serverLookupPreload.Value  != null;
        var projectLkpReady  = string.IsNullOrEmpty(preProjectId) || !projectLookupPreload.Loading || projectLookupPreload.Value != null;

        if (needDefaults && (!serversReady || !projectsReady || !serverLkpReady || !projectLkpReady))
            return Layout.Center() | Text.Muted("Loading…");

        // Pre-fill server/project only when we came from the deploy button (draft present).
        // On refresh or opening Deploy from sidebar without repo → draft is empty → form starts blank.
        var defaultServerId  = needDefaults ? preServerId  : "";
        var defaultProjectId = needDefaults ? preProjectId : "";

        return new DeployView(apiToken, draft ?? new DeployDraft(string.Empty), defaultServerId, defaultProjectId);
    }
}

/// <summary>Arguments for internal Ivy navigation to SliplaneDeployApp.</summary>
public record DeployArgs(string Repo);
