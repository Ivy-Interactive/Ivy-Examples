namespace SliplaneDeploy.Apps;

using SliplaneDeploy.Apps.Views;
using SliplaneDeploy.Models;
using SliplaneDeploy.Services;

/// <summary>
/// Route: /sliplane-deploy-app
/// One-click deploy: Server + Service name, Deploy button, status check at the end.
/// ?repo= is captured by RepoCaptureFilter, parsed into a DeployDraft, and pre-fills the form.
/// </summary>
[App(
    id: "sliplane-deploy-app",
    icon: Icons.Server,
    title: "Deploy on Sliplane",
    isVisible: true)]
public class SliplaneDeployApp : ViewBase
{
    public override object? Build()
    {
        var config = this.UseService<IConfiguration>();
        var auth = this.UseService<IAuthService>();
        var draftStore = this.UseService<DeploymentDraftStore>();
        var args = this.UseArgs<DeployArgs>();
        var client = this.UseService<SliplaneApiClient>();
        var draftState = this.UseState<DeployDraft?>(() =>
            args is not null
                ? DeploymentDraftStore.ParseGitHubUrl(args.Repo)
                : draftStore.LastDraft);

        var firstServerQuery = this.UseQuery<SliplaneServer?, (string, string)>(
            key: ("deploy-default-server", config["Sliplane:ApiToken"] ?? auth.GetAuthSession().AuthToken?.AccessToken ?? string.Empty),
            fetcher: async (key, ct) => (await client.GetServersAsync(key.Item2)).FirstOrDefault());

        var ivyProjectQuery = this.UseQuery<SliplaneProject?, (string, bool)>(
            key: (config["Sliplane:ApiToken"] ?? auth.GetAuthSession().AuthToken?.AccessToken ?? string.Empty, draftState.Value is not null),
            fetcher: async (key, ct) =>
            {
                var (token, needIvy) = key;
                if (!needIvy) return null;
                var projects = await client.GetProjectsAsync(token);
                var ivy = projects.FirstOrDefault(p => p.Name.Equals("Ivy", StringComparison.OrdinalIgnoreCase));
                return ivy ?? await client.CreateProjectAsync(token, "Ivy");
            });

        var firstProjectQuery = this.UseQuery<SliplaneProject?, (string, string)>(
            key: ("deploy-default-project", config["Sliplane:ApiToken"] ?? auth.GetAuthSession().AuthToken?.AccessToken ?? string.Empty),
            fetcher: async (key, ct) => (await client.GetProjectsAsync(key.Item2)).FirstOrDefault());

        var serverLookupPreload = this.UseQuery<Option<string>?, (string, string?, int)>(
            key: ("deploy-server-lookup", string.IsNullOrEmpty(firstServerQuery.Value?.Id ?? "") ? null : firstServerQuery.Value!.Id, 0),
            fetcher: async _ =>
            {
                var currentServerId = firstServerQuery.Value?.Id ?? "";
                return string.IsNullOrEmpty(currentServerId)
                ? null
                : new Option<string>(firstServerQuery.Value!.Name, currentServerId);
            });

        var projectLookupPreload = this.UseQuery<Option<string>?, (string, string?, int)>(
            key: ("deploy-project-lookup", string.IsNullOrEmpty((draftState.Value is not null ? ivyProjectQuery.Value?.Id : firstProjectQuery.Value?.Id) ?? "") ? null : (draftState.Value is not null ? ivyProjectQuery.Value?.Id : firstProjectQuery.Value?.Id), 0),
            fetcher: async _ =>
            {
                var currentNeedIvyProject = draftState.Value is not null;
                var currentProjectId = (currentNeedIvyProject ? ivyProjectQuery.Value?.Id : firstProjectQuery.Value?.Id) ?? "";
                return string.IsNullOrEmpty(currentProjectId)
                ? null
                : new Option<string>((currentNeedIvyProject ? ivyProjectQuery.Value?.Name : firstProjectQuery.Value?.Name) ?? "Ivy", currentProjectId);
            });

        var session = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;
        var draft = draftState.Value;
        var needIvyProject = draft is not null;
        var ivyProject = needIvyProject ? ivyProjectQuery.Value : null;
        var preServerId = firstServerQuery.Value?.Id ?? "";
        var preProjectId = (needIvyProject ? ivyProject?.Id : firstProjectQuery.Value?.Id) ?? "";

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                | (Layout.Vertical().AlignContent(Align.Center).Gap(6)
                    | Text.H2("Deploy to Sliplane")
                    | (draft is not null
                        ? Text.Muted($"Repository: {draft.RepoUrl}")
                        : Text.Muted("Sign in with Sliplane to deploy your Ivy app."))
                    | Text.Muted("No API token. Please sign in or configure Sliplane:ApiToken."));
        }

        var needDefaults = draft is not null;
        var serversReady = !firstServerQuery.Loading || firstServerQuery.Value != null;
        var projectsReady = needIvyProject
            ? (!ivyProjectQuery.Loading || ivyProjectQuery.Value != null)
            : (!firstProjectQuery.Loading || firstProjectQuery.Value != null);
        var serverLkpReady = string.IsNullOrEmpty(preServerId) || !serverLookupPreload.Loading || serverLookupPreload.Value != null;
        var projectLkpReady = string.IsNullOrEmpty(preProjectId) || !projectLookupPreload.Loading || projectLookupPreload.Value != null;

        if (!serversReady || !projectsReady || !serverLkpReady || !projectLkpReady)
            return Layout.Center() | Text.Muted("Loading…");

        var defaultServerId = needDefaults ? preServerId : (firstServerQuery.Value?.Id ?? "");
        var defaultProjectId = needDefaults ? preProjectId : ((needIvyProject ? ivyProject?.Id : firstProjectQuery.Value?.Id) ?? "");

        return new DeployView(apiToken, draft ?? new DeployDraft(string.Empty), defaultServerId, defaultProjectId);
    }
}

/// <summary>Arguments for navigation to SliplaneDeployApp with ?repo=.</summary>
public record DeployArgs(string Repo);
