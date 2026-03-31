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
        // After this long stuck on "pending" via events, cross-check listing status.
        const int listingFallbackMinutes = 5;
        var pendingSince = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            if (DateTime.UtcNow >= deadline)
            {
                await _commentService.TryPostOrUpdateStagingCommentAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: null, samplesUrl: null,
                    status: "Deploy timed out",
                    logLines: new[] { $"No terminal state received from Sliplane after {maxTotalMinutes} minutes." },
                    forceNewComment: true,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var (docsEvents, samplesEvents) = await _deployService.GetDeploymentEventsForServicesAsync(
                apiToken, docsServiceId, samplesServiceId);

            var combined = SliplaneDeploymentStatusResolver.ResolveCombinedStatus(
                docsServiceId, docsEvents, samplesServiceId, samplesEvents);

            _logger.LogDebug("PR #{Pr} deploy status from events: {Status} (docs={DocsEvents}, samples={SamplesEvents} events)",
                req.PrNumber, combined, docsEvents.Count, samplesEvents.Count);

            // If events API is not returning useful data after a while, fall back to the service
            // status field from the listing API (Sliplane always populates this, even when events lag).
            if (combined == "pending" && (DateTime.UtcNow - pendingSince).TotalMinutes >= listingFallbackMinutes)
            {
                var dep = await _deployService.GetDeploymentByPrNumberAsync(apiToken, req.PrNumber);
                var listingStatus = ResolveStatusFromListing(dep, docsServiceId, samplesServiceId);
                _logger.LogDebug("PR #{Pr} listing fallback status: {Status} (DocsStatus={Docs}, SamplesStatus={Samples})",
                    req.PrNumber, listingStatus, dep?.DocsStatus, dep?.SamplesStatus);
                if (listingStatus != "pending")
                    combined = listingStatus;
            }

            var urls = await _deployService.GetDeploymentUrlsForPrAsync(apiToken, req.PrNumber);

            if (combined == "failed")
            {
                var logLines = BuildRecentLogLines(docsEvents, samplesEvents, maxLines: 10);
                await _commentService.TryPostOrUpdateStagingCommentAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: urls.DocsUrl, samplesUrl: urls.SamplesUrl,
                    status: "Deploy failed",
                    logLines: logLines,
                    forceNewComment: true,
                    cancellationToken: CancellationToken.None);
                return;
            }

            if (combined == "deployed")
            {
                // Track when "deployed" was first seen.
                deployedAt ??= DateTime.UtcNow;

                var bothUrlsReady = !string.IsNullOrWhiteSpace(urls.DocsUrl) && !string.IsNullOrWhiteSpace(urls.SamplesUrl);
                var urlWaitExpired = (DateTime.UtcNow - deployedAt.Value).TotalSeconds >= 30;

                // Post as soon as both URLs are available, or after 30s if they never appear.
                if (bothUrlsReady || urlWaitExpired)
                {
                    await _commentService.TryPostOrUpdateStagingCommentAsync(
                        req.Owner, req.Repo, req.PrNumber,
                        docsUrl: urls.DocsUrl, samplesUrl: urls.SamplesUrl,
                        status: "Deployed",
                        logLines: null,
                        forceNewComment: true,
                        cancellationToken: CancellationToken.None);
                    return;
                }
                // URLs not ready yet — keep polling with short delay.
                await Task.Delay(2500, ct);
                continue;
            }

            // combined == "pending": poll silently, no intermediate comment
            await Task.Delay(delayMs, ct);
            if (delayMs < maxDelayMs)
                delayMs = Math.Min(maxDelayMs, (int)(delayMs * 1.25));
        }
        // App shutting down — do nothing, startup scan will resume on next deploy.
    }

    /// <summary>
    /// Determines deploy status from the Sliplane listing's status field.
    /// Used as a fallback when the events API returns no data.
    /// Sliplane status values include: "live", "deploying", "building", "error", "dead", "starting".
    /// </summary>
    private static string ResolveStatusFromListing(StagingDeployment? dep, string? docsServiceId, string? samplesServiceId)
    {
        if (dep == null) return "pending";

        var statuses = new List<string?>();
        if (!string.IsNullOrEmpty(docsServiceId) && dep.DocsServiceId == docsServiceId)
            statuses.Add(dep.DocsStatus);
        if (!string.IsNullOrEmpty(samplesServiceId) && dep.SamplesServiceId == samplesServiceId)
            statuses.Add(dep.SamplesStatus);

        // If we couldn't match the specific service IDs, use whatever status is available.
        if (statuses.Count == 0)
            statuses = new List<string?> { dep.DocsStatus, dep.SamplesStatus };

        var relevant = statuses.Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (relevant.Count == 0) return "pending";

        // Any terminal-failure state → report failed.
        if (relevant.Any(s => s is "error" or "dead" or "crashed" or "unhealthy"))
            return "failed";

        // All live → deployed.
        if (relevant.All(s => s == "live"))
            return "deployed";

        // Still building / deploying / starting → pending.
        return "pending";
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

