namespace SliplaneDeploy.Apps.Views;

using SliplaneDeploy.Models;
using SliplaneDeploy.Services;

/// <summary>
/// Shows deployment status by polling service events after create.
/// </summary>
public class DeployStatusView : ViewBase
{
    private readonly string _apiToken;
    private readonly string _projectId;
    private readonly SliplaneService _service;

    public DeployStatusView(string apiToken, string projectId, SliplaneService service)
    {
        _apiToken = apiToken;
        _projectId = projectId;
        _service = service;
    }

    public override object? Build()
    {
        var client = this.UseService<SliplaneApiClient>();
        var eventsQuery = this.UseQuery<List<SliplaneServiceEvent>, (string, string, string)>(
            key: ("deploy-status-events", _projectId, _service.Id),
            fetcher: async ct => await client.GetServiceEventsAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions
            {
                RefreshInterval = TimeSpan.FromSeconds(2),
                KeepPrevious = true,
            });

        var events = eventsQuery.Value ?? [];
        var status = DeriveStatus(events);
        var statusText = status switch
        {
            DeployStatus.Success => "Deploy succeeded",
            DeployStatus.Failed => "Deploy failed",
            DeployStatus.Deploying => "Deploying…",
            _ => "Initializing…",
        };

        var calloutVariant = status switch
        {
            DeployStatus.Success => CalloutVariant.Success,
            DeployStatus.Failed => CalloutVariant.Error,
            _ => CalloutVariant.Info,
        };

        var serviceQuery = this.UseQuery<SliplaneService?, (string, string, string)>(
            key: ("deploy-service-details", _projectId, _service.Id),
            fetcher: async ct => await client.GetServiceAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions { KeepPrevious = true });

        var serviceForUrl = status == DeployStatus.Success ? serviceQuery.Value : _service;
        var siteUrl = serviceForUrl?.Network?.CustomDomains?.FirstOrDefault()?.Domain
                   ?? serviceForUrl?.Network?.ManagedDomain
                   ?? string.Empty;
        var siteUrlAbsolute = string.IsNullOrWhiteSpace(siteUrl)
            ? string.Empty
            : (siteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || siteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? siteUrl
                : "https://" + siteUrl);

        var header = status == DeployStatus.Success
            ? (Layout.Vertical().Gap(1) | Text.H3(statusText))
            : (Layout.Vertical().Gap(1) | Text.H3(statusText));

        object? progressBar = null;
        if (status is DeployStatus.Deploying or DeployStatus.Unknown)
        {
            progressBar = new Progress().Indeterminate().Goal("Building and deploying…");
        }

        object? linkSection = null;
        if (status == DeployStatus.Success && !string.IsNullOrEmpty(siteUrlAbsolute))
        {
            linkSection = (Layout.Horizontal().Align(Align.Left)| Text.Block("URL:").Bold() | new Button(siteUrl).Link().Url(siteUrlAbsolute).Width(Size.Fit()));
        }

        var content = Layout.Vertical().Align(Align.Left)
            | header;
        if (progressBar != null)
            content = content | progressBar;
        if (linkSection != null)
            content = content | linkSection;

        return new Callout(content, "Deployment status", calloutVariant).Icon(Icons.Rocket);
    }

    private enum DeployStatus { Unknown, Deploying, Success, Failed }

    private static DeployStatus DeriveStatus(List<SliplaneServiceEvent> events)
    {
        var last = events.LastOrDefault();
        if (last == null) return DeployStatus.Unknown;

        return last.Type switch
        {
            "service_deploy_success" => DeployStatus.Success,
            "service_deploy_failed" => DeployStatus.Failed,
            "service_deploy" => DeployStatus.Deploying,
            _ => events.Any(e => e.Type == "service_deploy_success") ? DeployStatus.Success
                 : events.Any(e => e.Type == "service_deploy_failed") ? DeployStatus.Failed
                 : DeployStatus.Deploying,
        };
    }
}
