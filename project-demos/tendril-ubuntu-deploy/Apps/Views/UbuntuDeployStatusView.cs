namespace TendrilUbuntuDeploy.Apps.Views;

using TendrilUbuntuDeploy.Models;
using TendrilUbuntuDeploy.Services;

/// <summary>
/// Shown after a successful Sliplane service creation. Polls events and service details,
/// then shows noVNC URL + RDP connection info + credentials.
/// </summary>
public class UbuntuDeployStatusView : ViewBase
{
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly SliplaneService _service;
    private readonly string _rdpUser;
    private readonly string _rdpPassword;
    private readonly string _serverId;

    public UbuntuDeployStatusView(
        string apiToken,
        string projectId,
        SliplaneService service,
        string rdpUser,
        string rdpPassword,
        string serverId)
    {
        _apiToken = apiToken;
        _projectId = projectId;
        _service = service;
        _rdpUser = rdpUser;
        _rdpPassword = rdpPassword;
        _serverId = serverId;
    }

    public override object? Build()
    {
        var client = UseService<SliplaneApiClient>();

        var eventsQuery = UseQuery<List<SliplaneServiceEvent>, (string, string, string)>(
            key: ("ubuntu-deploy-events", _projectId, _service.Id),
            fetcher: async _ => await client.GetServiceEventsAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(2), KeepPrevious = true });

        var serviceQuery = UseQuery<SliplaneService?, (string, string, string)>(
            key: ("ubuntu-deploy-service", _projectId, _service.Id),
            fetcher: async _ => await client.GetServiceAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(3), KeepPrevious = true });

        var serverQuery = UseQuery<SliplaneServer?, (string, string)>(
            key: ("ubuntu-deploy-server", _serverId),
            fetcher: async _ => await client.GetServerAsync(_apiToken, _serverId));

        var events = eventsQuery.Value ?? [];
        var latestService = serviceQuery.Value ?? _service;
        var server = serverQuery.Value;

        var status = DeriveStatus(events);
        var noVncHost = ResolveSiteHost(latestService) ?? ResolveSiteHost(_service) ?? string.Empty;
        var noVncUrl = string.IsNullOrWhiteSpace(noVncHost)
            ? string.Empty
            : noVncHost.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? noVncHost : $"https://{noVncHost}";

        var serverIp = server?.Ipv4 ?? string.Empty;

        var content = Layout.Vertical().Gap(4).AlignContent(Align.Left).Width(Size.Full());

        // ── Deploy status callout ─────────────────────────────────────────────
        if (status == DeployStatus.Failed)
        {
            var failMsg = events.LastOrDefault(e =>
                    e.Type is "service_deploy_failed" or "service_build_failed")?.Message;
            content = content | new Callout(
                Text.Markdown($"**Build/deploy failed.**\n\n{Escape(failMsg ?? "Check Sliplane logs for details.")}"),
                variant: CalloutVariant.Error).Width(Size.Full());
        }
        else if (status == DeployStatus.Success)
        {
            content = content | new Callout(
                Text.Block("Your Ubuntu Desktop is building and starting up. It will be ready in a few minutes."),
                "Deployment succeeded",
                CalloutVariant.Success).Width(Size.Full());
        }
        else
        {
            content = content | new Callout(
                Layout.Horizontal().Gap(3).AlignContent(Align.Center)
                    | new Progress().Indeterminate()
                    | Text.Block("Building the Ubuntu Desktop image… this takes 5–10 minutes on first deploy."),
                "Building",
                CalloutVariant.Info).Width(Size.Full());
        }

        // ── Connection info ───────────────────────────────────────────────────
        var connCard = Layout.Vertical().Gap(3).Width(Size.Full());

        if (!string.IsNullOrEmpty(noVncUrl))
        {
            connCard = connCard
                | Text.H3("Browser access (noVNC)")
                | (Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                    | Text.Muted("URL:")
                    | new Button(noVncUrl).Link().Url(noVncUrl).Width(Size.Fit()))
                | Text.Muted("Open the link → enter your password → full desktop in your browser.");
        }

        if (!string.IsNullOrEmpty(serverIp))
        {
            var rdpAddress = $"{serverIp}:3389";
            connCard = connCard
                | Text.H3("RDP access (native client)")
                | (Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                    | Text.Muted("Address:")
                    | Text.Code(rdpAddress))
                | Text.Muted("Use Windows Remote Desktop, Remmina, or Microsoft Remote Desktop on macOS/iOS.");
        }
        else if (!string.IsNullOrEmpty(serverIp) == false)
        {
            connCard = connCard
                | Text.H3("RDP access")
                | Text.Muted("Server IP is loading… check Sliplane dashboard for server details.");
        }

        // ── Credentials ───────────────────────────────────────────────────────
        var vncPassword = _rdpPassword.Length > 8 ? _rdpPassword[..8] : _rdpPassword;
        connCard = connCard
            | Text.H3("Credentials")
            | (Layout.Horizontal().Gap(4).Wrap()
                | (Layout.Vertical().Gap(1)
                    | Text.Muted("Username")
                    | Text.Code(_rdpUser))
                | (Layout.Vertical().Gap(1)
                    | Text.Muted("RDP / Linux password")
                    | Text.Code(_rdpPassword))
                | (Layout.Vertical().Gap(1)
                    | Text.Muted("noVNC password (max 8 chars)")
                    | Text.Code(vncPassword)));

        content = content | new Card(connCard).Width(Size.Full());

        // ── Next steps ────────────────────────────────────────────────────────
        content = content | new Callout(
            Layout.Vertical().Gap(2)
                | Text.Markdown("**JetBrains Rider** is not pre-installed (keeps the image under 1 GB). " +
                    "After connecting, open a terminal and run:")
                | Text.Code("install-rider")
                | Text.Muted("Or click the \"Install Rider\" shortcut on the desktop. Download is ~2.5 GB."),
            "Install Rider after connecting",
            CalloutVariant.Info).Width(Size.Full());

        // ── Manage link ───────────────────────────────────────────────────────
        content = content
            | (Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                | Text.Block("Manage your service:")
                | new Button("Open Sliplane").Link().Url("https://ivy-sliplane-management.sliplane.app/$auth").Width(Size.Fit()));

        return content;
    }

    private static string? ResolveSiteHost(SliplaneService? s)
    {
        if (s == null) return null;
        var custom = s.Network?.CustomDomains?.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Domain))?.Domain;
        if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();
        var managed = s.Network?.ManagedDomain;
        if (!string.IsNullOrWhiteSpace(managed)) return managed.Trim();
        return s.Domains?.Select(d => d.Domain).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))?.Trim();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("*", "\\*").Replace("_", "\\_").Replace("#", "\\#").Replace("`", "\\`");

    private enum DeployStatus { Unknown, Deploying, Success, Failed }

    private static DeployStatus DeriveStatus(List<SliplaneServiceEvent> events)
    {
        if (events.Any(e => e.Type == "service_deploy_success")) return DeployStatus.Success;
        if (events.Any(e => e.Type is "service_deploy_failed" or "service_build_failed")) return DeployStatus.Failed;
        return events.Any() ? DeployStatus.Deploying : DeployStatus.Unknown;
    }
}
