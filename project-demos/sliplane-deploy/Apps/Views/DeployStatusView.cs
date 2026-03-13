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

        var statusColor = status switch
        {
            DeployStatus.Success => Colors.Success,
            DeployStatus.Failed => Colors.Destructive,
            _ => Colors.Muted,
        };

        var header = Layout.Vertical().Align(Align.Center).Gap(2)
            | Text.H2(statusText).Color(statusColor)
            | Text.Muted($"Service: {_service.Name}");

        object? eventsList = null;
        if (events.Count > 0)
        {
            var list = Layout.Vertical().Gap(1);
            foreach (var e in events.TakeLast(5).Reverse())
            {
                list = list | Text.Block($"{e.CreatedAt:HH:mm:ss} {e.Type}: {e.Message}").Muted();
            }
            eventsList = list;
        }

        var content = Layout.Vertical().Gap(4) | header;
        if (eventsList != null)
            content = content | new Separator() | eventsList;

        return content;
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
