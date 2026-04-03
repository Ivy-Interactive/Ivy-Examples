namespace PrStagingDeploy.Services;

using System.Threading.Channels;
using PrStagingDeploy.Models;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Background worker that waits for Sliplane deploy to become deployed/failed
/// and updates the PR comment when the final state is known.
/// </summary>
public class PrStagingDeployCommentUpdateBackgroundService : BackgroundService
{
    private readonly PrStagingDeployCommentUpdateQueue _queue;
    private readonly PrStagingDeployCommentService _commentService;
    private readonly StagingDeployService _deployService;
    private readonly IConfiguration _config;
    private readonly ILogger<PrStagingDeployCommentUpdateBackgroundService> _logger;

    public PrStagingDeployCommentUpdateBackgroundService(
        PrStagingDeployCommentUpdateQueue queue,
        PrStagingDeployCommentService commentService,
        StagingDeployService deployService,
        IConfiguration config,
        ILogger<PrStagingDeployCommentUpdateBackgroundService> logger)
    {
        _queue = queue;
        _commentService = commentService;
        _deployService = deployService;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var req in _queue.Channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(req, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process PR staging update request for PR #{Pr}", req.PrNumber);
            }
        }
    }

    private async Task ProcessAsync(PrStagingDeployCommentUpdateRequest req, CancellationToken ct)
    {
        var apiToken = _config["Sliplane:ApiToken"] ?? "";
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            _logger.LogWarning("Sliplane:ApiToken is missing; skip PR #{Pr} comment update", req.PrNumber);
            return;
        }

        var docsServiceId = req.DocsServiceId;
        var samplesServiceId = req.SamplesServiceId;

        if (string.IsNullOrWhiteSpace(docsServiceId) && string.IsNullOrWhiteSpace(samplesServiceId))
        {
            await _commentService.TryPostOrUpdateStagingCommentAsync(
                req.Owner,
                req.Repo,
                req.PrNumber,
                docsUrl: null,
                samplesUrl: null,
                status: "Deploy failed",
                logLines: new[] { "Sliplane service ids are missing; cannot determine deploy result." },
                cancellationToken: ct);
            return;
        }

        var delayMs = 2500;
        const int maxDelayMs = 20000;
        const int maxTotalMinutes = 30;
        var deadline = DateTime.UtcNow.AddMinutes(maxTotalMinutes);
        DateTime? deployedAt = null;

        while (!ct.IsCancellationRequested)
        {
            if (DateTime.UtcNow >= deadline)
            {
                await _commentService.TryPostOrUpdateStagingCommentAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: null, samplesUrl: null,
                    status: "Deploy timed out",
                    logLines: new[] { $"No terminal state received from Sliplane after {maxTotalMinutes} minutes." },
                    cancellationToken: CancellationToken.None);
                return;
            }

            var (docsEvents, samplesEvents) = await _deployService.GetDeploymentEventsForServicesAsync(
                apiToken, docsServiceId, samplesServiceId);

            var dep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, req.PrNumber);
            var docsMerged = MergeServiceWithListing(
                docsServiceId, docsEvents, GetListingFieldForService(dep, docsServiceId, docs: true));
            var samplesMerged = MergeServiceWithListing(
                samplesServiceId, samplesEvents, GetListingFieldForService(dep, samplesServiceId, docs: false));
            var overall = SliplaneDeploymentStatusResolver.ResolveOverallTerminal(docsMerged, samplesMerged);

            _logger.LogDebug(
                "PR #{Pr} deploy status: overall={Overall} (docs={Docs}, samples={Samples}; docsEv={DocsEv}, samplesEv={SamplesEv})",
                req.PrNumber, overall, docsMerged, samplesMerged, docsEvents.Count, samplesEvents.Count);

            var urls = await _deployService.GetDeploymentUrlsForPrAsync(apiToken, req.PrNumber);

            if (overall == "partial")
            {
                var logLines = BuildLogLinesForFailedServicesOnly(
                    docsMerged, samplesMerged, docsEvents, samplesEvents, maxLines: 12);
                var breakdown = new StagingCommentPartialBreakdown(
                    docsMerged, samplesMerged, urls.DocsUrl, urls.SamplesUrl);
                await _commentService.TryPostOrUpdateStagingCommentAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: urls.DocsUrl, samplesUrl: urls.SamplesUrl,
                    status: null,
                    logLines: logLines,
                    partial: breakdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            if (overall == "failed")
            {
                var logLines = BuildRecentLogLines(docsEvents, samplesEvents, maxLines: 10);
                await _commentService.TryPostOrUpdateStagingCommentAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: urls.DocsUrl, samplesUrl: urls.SamplesUrl,
                    status: "Deploy failed",
                    logLines: logLines,
                    cancellationToken: CancellationToken.None);
                return;
            }

            if (overall == "deployed")
            {
                deployedAt ??= DateTime.UtcNow;

                var docsWantsUrl = !string.IsNullOrEmpty(docsServiceId);
                var samplesWantsUrl = !string.IsNullOrEmpty(samplesServiceId);
                var urlsReady =
                    (!docsWantsUrl || !string.IsNullOrWhiteSpace(urls.DocsUrl))
                    && (!samplesWantsUrl || !string.IsNullOrWhiteSpace(urls.SamplesUrl));
                var urlWaitExpired = (DateTime.UtcNow - deployedAt.Value).TotalSeconds >= 30;

                if (urlsReady || urlWaitExpired)
                {
                    await _commentService.TryPostOrUpdateStagingCommentAsync(
                        req.Owner, req.Repo, req.PrNumber,
                        docsUrl: urls.DocsUrl, samplesUrl: urls.SamplesUrl,
                        status: "Deployed",
                        logLines: null,
                        cancellationToken: CancellationToken.None);
                    return;
                }

                await Task.Delay(2500, ct);
                continue;
            }

            // overall == "pending": poll silently, no intermediate comment
            await Task.Delay(delayMs, ct);
            if (delayMs < maxDelayMs)
                delayMs = Math.Min(maxDelayMs, (int)(delayMs * 1.25));
        }
        // App shutting down — do nothing, startup scan will resume on next deploy.
    }

    private static string? GetListingFieldForService(StagingDeployment? dep, string? serviceId, bool docs)
    {
        if (dep == null || string.IsNullOrEmpty(serviceId))
            return null;
        if (docs)
            return dep.DocsServiceId == serviceId ? dep.DocsStatus : null;
        return dep.SamplesServiceId == serviceId ? dep.SamplesStatus : null;
    }

    private static string MergeServiceWithListing(string? serviceId, List<SliplaneServiceEvent> events, string? listingField)
    {
        if (string.IsNullOrEmpty(serviceId))
            return "skipped";
        var fromEvents = SliplaneDeploymentStatusResolver.ResolveStatusFromEvents(events);
        var fromListing = string.IsNullOrEmpty(listingField)
            ? "pending"
            : SliplaneDeploymentStatusResolver.ListingFieldToState(listingField);
        return SliplaneDeploymentStatusResolver.MergeServiceState(fromEvents, fromListing);
    }

    private static IReadOnlyList<string> BuildLogLinesForFailedServicesOnly(
        string docsMerged,
        string samplesMerged,
        List<SliplaneServiceEvent> docsEvents,
        List<SliplaneServiceEvent> samplesEvents,
        int maxLines)
    {
        if (docsMerged == "failed" && samplesMerged == "failed")
            return BuildRecentLogLines(docsEvents, samplesEvents, maxLines);

        if (docsMerged == "failed")
            return BuildRecentLogLines(docsEvents, new List<SliplaneServiceEvent>(), maxLines);

        if (samplesMerged == "failed")
            return BuildRecentLogLines(new List<SliplaneServiceEvent>(), samplesEvents, maxLines);

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> BuildRecentLogLines(
        List<SliplaneServiceEvent> docsEvents,
        List<SliplaneServiceEvent> samplesEvents,
        int maxLines)
    {
        var combined = docsEvents.Concat(samplesEvents)
            .OrderByDescending(e => e.CreatedAt)
            .Take(maxLines)
            .ToList();

        if (combined.Count == 0)
            return new[] { "Waiting for Sliplane events..." };

        return combined.Select(e =>
        {
            var time = e.CreatedAt.ToLocalTime().ToString("HH:mm:ss");
            var type = (e.Type ?? "").Trim();
            var msg = !string.IsNullOrWhiteSpace(e.Message) ? e.Message : e.Reason;
            var friendly = type switch
            {
                "service_deploy_success" => "Service deployed successfully",
                "service_deploy_failed"  => "Service deploy failed",
                "service_build_failed"   => "Build failed",
                "service_build"          => "Service build",
                "service_deploy"         => "Deploy started",
                _ => type
            };
            var text = string.IsNullOrWhiteSpace(msg) ? friendly : $"{friendly}: {TruncLine(msg, 180)}";
            return $"{time} {text}";
        }).ToList();
    }

    private static string TruncLine(string? s, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var line = s.Trim().Replace("\r", "").Replace("\n", " ");
        return line.Length <= maxLen ? line : line[..maxLen] + "...";
    }
}

