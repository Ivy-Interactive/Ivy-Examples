namespace TendrilUbuntuDeploy.Apps;

using TendrilUbuntuDeploy.Apps.Views;
using TendrilUbuntuDeploy.Models;
using TendrilUbuntuDeploy.Services;

/// <summary>
/// One-click Ubuntu Desktop deployer: sign in with Sliplane, pick a server,
/// set RDP credentials → Sliplane builds and launches a full Ubuntu 22.04 desktop
/// with XFCE4 + xrdp + noVNC + JetBrains Rider + Ivy Tendril.
/// </summary>
[App(
    id: "ubuntu-deploy-app",
    icon: Icons.Monitor,
    title: "Ubuntu Desktop on Sliplane",
    isVisible: true)]
public class UbuntuDeployApp : ViewBase
{
    public override object? Build()
    {
        var config = UseService<IConfiguration>();
        var auth = UseService<IAuthService>();
        var client = UseService<SliplaneApiClient>();

        var firstServerQuery = UseQuery<SliplaneServer?, (string, string)>(
            key: ("ubuntu-default-server", config["Sliplane:ApiToken"] ?? auth.GetAuthSession().AuthToken?.AccessToken ?? string.Empty),
            fetcher: async (key, ct) => (await client.GetServersAsync(key.Item2)).FirstOrDefault());

        var firstProjectQuery = UseQuery<SliplaneProject?, (string, string)>(
            key: ("ubuntu-default-project", config["Sliplane:ApiToken"] ?? auth.GetAuthSession().AuthToken?.AccessToken ?? string.Empty),
            fetcher: async (key, ct) => (await client.GetProjectsAsync(key.Item2)).FirstOrDefault());

        var session = auth.GetAuthSession();
        var apiToken = config["Sliplane:ApiToken"]
                       ?? session.AuthToken?.AccessToken
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return Layout.Center()
                | (Layout.Vertical().AlignContent(Align.Center).Gap(6)
                    | Text.H2("Ubuntu Desktop on Sliplane").Align(TextAlignment.Center)
                    | Text.Muted("Sign in with Sliplane to deploy a full Ubuntu Desktop with JetBrains Rider and Ivy Tendril.")
                    | Text.Muted($"Docker image source: {UbuntuDeployDefaults.DefaultRepoUrl}"));
        }

        var serversReady = !firstServerQuery.Loading || firstServerQuery.Value != null;
        var projectsReady = !firstProjectQuery.Loading || firstProjectQuery.Value != null;

        if (!serversReady || !projectsReady)
            return Layout.Center() | Text.Muted("Loading…");

        var preServerId  = firstServerQuery.Value?.Id  ?? "";
        var preProjectId = firstProjectQuery.Value?.Id ?? "";

        return new UbuntuDeployView(apiToken, preServerId, preProjectId);
    }
}
