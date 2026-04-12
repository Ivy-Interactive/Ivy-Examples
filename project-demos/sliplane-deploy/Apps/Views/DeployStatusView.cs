namespace SliplaneDeploy.Apps.Views;

using SliplaneDeploy.Models;
using SliplaneDeploy.Services;

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
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(2), KeepPrevious = true });
        var serviceQuery = this.UseQuery<SliplaneService?, (string, string, string)>(
            key: ("deploy-service-details", _projectId, _service.Id),
            fetcher: async ct => await client.GetServiceAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(3), KeepPrevious = true });

        var events = eventsQuery.Value ?? [];
        var status = DeriveStatus(events);

        var latestService = serviceQuery.Value ?? _service;
        var siteHost = ResolveSiteHost(latestService) ?? ResolveSiteHost(_service) ?? string.Empty;
        var siteUrlAbsolute = string.IsNullOrWhiteSpace(siteHost) ? string.Empty
            : siteHost.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? siteHost
            : "https://" + siteHost;

        var manageUrl = "https://ivy-sliplane-management.sliplane.app/$auth";

        var content = Layout.Vertical().Gap(2).AlignContent(Align.Left).Width(Size.Full());

        if (!string.IsNullOrEmpty(siteUrlAbsolute))
            content = content | LabelPlusUrlRow("Your app will be available at:", siteUrlAbsolute);

        content = content | LabelPlusUrlRow("You can manage it at:", manageUrl);

        if (status == DeployStatus.Failed)
        {
            var failureMessage = GetFailureMessage(events);
            var errBody = !string.IsNullOrEmpty(failureMessage)
                ? failureMessage
                : "Deployment failed. Check the service logs in Sliplane dashboard.";
            content = content | new Callout(Text.Markdown($"**Deployment failed.**\n\n{MarkdownEscapePlain(errBody)}"),
                variant: CalloutVariant.Error).Width(Size.Full());
        }

        return content;
    }

    /// <summary>
    /// Single horizontal row: bold label + link button. Avoids <see cref="Text.Markdown"/> here because
    /// it renders as block content (line breaks, extra icons) and breaks inline layout with the URL.
    /// </summary>
    private static object LabelPlusUrlRow(string label, string absoluteUrl) =>
        Layout.Horizontal().AlignContent(Align.Left).Gap(2)
            | Text.Block(label).Bold()
            | new Button(absoluteUrl).Link().Url(absoluteUrl).Width(Size.Fit());

    private static string MarkdownEscapePlain(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("*", "\\*", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal)
         .Replace("#", "\\#", StringComparison.Ordinal)
         .Replace("`", "\\`", StringComparison.Ordinal);

    private static string? ResolveSiteHost(SliplaneService? s)
    {
        if (s == null) return null;
        var custom = s.Network?.CustomDomains?.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Domain))?.Domain;
        if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();
        var managed = s.Network?.ManagedDomain;
        if (!string.IsNullOrWhiteSpace(managed)) return managed.Trim();
        return s.Domains?.Select(d => d.Domain).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))?.Trim();
    }

    private enum DeployStatus { Unknown, Deploying, Success, Failed }

    private static DeployStatus DeriveStatus(List<SliplaneServiceEvent> events)
    {
        if (events.Any(e => e.Type == "service_deploy_success")) return DeployStatus.Success;
        if (events.Any(e => e.Type == "service_deploy_failed" || e.Type == "service_build_failed")) return DeployStatus.Failed;
        var last = events.LastOrDefault();
        if (last == null) return DeployStatus.Unknown;
        return last.Type switch
        {
            "service_deploy_success" => DeployStatus.Success,
            "service_deploy_failed" or "service_build_failed" => DeployStatus.Failed,
            _ => DeployStatus.Deploying,
        };
    }

    private static string? GetFailureMessage(List<SliplaneServiceEvent> events) =>
        events.LastOrDefault(e => e.Type is "service_deploy_failed" or "service_build_failed")
              ?.Message is { Length: > 0 } msg ? msg : null;
}
