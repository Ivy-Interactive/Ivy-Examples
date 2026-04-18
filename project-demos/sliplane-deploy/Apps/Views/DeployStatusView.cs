namespace SliplaneDeploy.Apps.Views;

using SliplaneDeploy.Models;
using SliplaneDeploy.Services;

public class DeployStatusView : ViewBase
{
    private static readonly TimeSpan HealthCheckStartDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromMinutes(5);

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
        var httpClientFactory = this.UseService<IHttpClientFactory>();
        var deployingStartedAt = this.UseState<DateTime?>(null);

        var eventsQuery = this.UseQuery<List<SliplaneServiceEvent>, (string, string, string)>(
            key: ("deploy-status-events", _projectId, _service.Id),
            fetcher: async ct =>
            {
                var fetched = await client.GetServiceEventsAsync(_apiToken, _projectId, _service.Id);
                var derivedStatus = DeriveStatus(fetched);
                if (derivedStatus == DeployStatus.Deploying && deployingStartedAt.Value == null)
                    deployingStartedAt.Set(DateTime.UtcNow);
                else if (derivedStatus is DeployStatus.Success or DeployStatus.Failed)
                    deployingStartedAt.Set(null);
                return fetched;
            },
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(2), KeepPrevious = true });

        var serviceQuery = this.UseQuery<SliplaneService?, (string, string, string)>(
            key: ("deploy-service-details", _projectId, _service.Id),
            fetcher: async ct => await client.GetServiceAsync(_apiToken, _projectId, _service.Id),
            options: new QueryOptions { RefreshInterval = TimeSpan.FromSeconds(3), KeepPrevious = true });

        // Key uses only constructor fields + state (no intermediate local vars) so this hook
        // stays at the top before any non-hook statements, satisfying IVYHOOK005.
        var healthQuery = this.UseQuery<int?, (string, string, bool)>(
            key: (
                "deploy-health-check",
                _service.Id,
                deployingStartedAt.Value.HasValue
                    && (DateTime.UtcNow - deployingStartedAt.Value.Value) >= HealthCheckStartDelay
            ),
            fetcher: async ct =>
            {
                // Guard: skip when health check window hasn't started yet
                if (!deployingStartedAt.Value.HasValue
                    || (DateTime.UtcNow - deployingStartedAt.Value.Value) < HealthCheckStartDelay)
                    return null;
                var url = GetSiteUrl(serviceQuery.Value ?? _service);
                if (string.IsNullOrEmpty(url)) return null;
                try
                {
                    using var httpClient = httpClientFactory.CreateClient();
                    using var response = await httpClient.GetAsync(url, ct);
                    return (int)response.StatusCode;
                }
                catch { return null; }
            },
            options: new QueryOptions
            {
                RefreshInterval = deployingStartedAt.Value.HasValue
                    && (DateTime.UtcNow - deployingStartedAt.Value.Value) >= HealthCheckStartDelay
                        ? TimeSpan.FromSeconds(15) : null,
                KeepPrevious = true
            });

        var events = eventsQuery.Value ?? [];
        var status = DeriveStatus(events);

        var latestService = serviceQuery.Value ?? _service;
        var siteUrlAbsolute = GetSiteUrl(latestService) is { Length: > 0 } u ? u : GetSiteUrl(_service);

        var deployingDuration = deployingStartedAt.Value.HasValue
            ? DateTime.UtcNow - deployingStartedAt.Value.Value
            : TimeSpan.Zero;

        var healthCheckActive = status == DeployStatus.Deploying
            && deployingDuration >= HealthCheckStartDelay
            && !string.IsNullOrEmpty(siteUrlAbsolute);

        var timedOut = status == DeployStatus.Deploying
            && deployingDuration >= HealthCheckStartDelay + HealthCheckTimeout;

        var siteIsUp = healthQuery.Value == 200 && status == DeployStatus.Deploying;

        var content = Layout.Vertical().Gap(2).AlignContent(Align.Left).Width(Size.Full());

        if (status == DeployStatus.Deploying && !siteIsUp)
        {
            if (timedOut)
            {
                content = content | new Callout(
                    Text.Markdown("**Deployment appears stuck.**\n\nThe service has been deploying for over 10 minutes and the site is not responding. Check the service logs in the Sliplane dashboard."),
                    variant: CalloutVariant.Error).Width(Size.Full());
            }
            else if (healthCheckActive)
            {
                var healthStatus = healthQuery.Value.HasValue
                    ? $"Last check returned HTTP {healthQuery.Value}."
                    : "Waiting for response…";
                content = content | new Callout(
                    Layout.Vertical()
                        | Text.Block("Deployment is taking longer than expected.").Bold()
                        | Text.Block($"Checking if the site is reachable. {healthStatus}"),
                    "Still deploying…",
                    CalloutVariant.Warning).Width(Size.Full());
            }
            else
            {
                content = content | new Callout(
                    Layout.Vertical()
                        | Text.Block("Deployment in progress.").Bold()
                        | new Progress().Indeterminate().Goal("Please wait…"),
                    "Deploying",
                    CalloutVariant.Info).Width(Size.Full());
            }
        }

        if (status == DeployStatus.Success)
        {
            var successMarkdown = string.IsNullOrEmpty(siteUrlAbsolute)
                ? "**Deployment successful.** Your app is live."
                : $"**Deployment successful.** Open your app: [{siteUrlAbsolute}]({siteUrlAbsolute})";
            content = content | new Callout(
                Text.Markdown(successMarkdown),
                "Deployed",
                CalloutVariant.Success).Width(Size.Full());
        }

        if (siteIsUp)
        {
            content = content | new Callout(
                Text.Markdown("**Site is responding with HTTP 200.** Your app is live."),
                variant: CalloutVariant.Success).Width(Size.Full());
        }

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

    private static string MarkdownEscapePlain(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("*", "\\*", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal)
         .Replace("#", "\\#", StringComparison.Ordinal)
         .Replace("`", "\\`", StringComparison.Ordinal);

    private static string GetSiteUrl(SliplaneService? s)
    {
        var host = ResolveSiteHost(s);
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;
        return host.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? host : "https://" + host;
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

    private enum DeployStatus { Deploying, Success, Failed }

    private static DeployStatus DeriveStatus(List<SliplaneServiceEvent> events)
    {
        if (events.Any(e => e.Type == "service_deploy_success")) return DeployStatus.Success;
        if (events.Any(e => e.Type == "service_deploy_failed" || e.Type == "service_build_failed")) return DeployStatus.Failed;
        var last = events.LastOrDefault();
        // No events yet (or still loading with empty list): treat as deploying so the UI does not flash.
        if (last == null) return DeployStatus.Deploying;
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
