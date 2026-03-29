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

        // Poll until either "failed" or "deployed". No fixed short timeout.
        // Safety: bounded by cancellation token only (worker stop) + bounded channel size upstream.
        var delayMs = 2500;
        const int maxDelayMs = 20000;
        var iteration = 0;

        while (!ct.IsCancellationRequested)
        {
            iteration++;
            var (docsEvents, samplesEvents) = await _deployService.GetDeploymentEventsForServicesAsync(
                apiToken,
                docsServiceId,
                samplesServiceId);

            var combined = SliplaneDeploymentStatusResolver.ResolveCombinedStatus(
                docsServiceId, docsEvents,
                samplesServiceId, samplesEvents);

            // Fetch URLs on each iteration so we can show partial links as they appear.
            var urls = await _deployService.GetDeploymentUrlsForPrAsync(apiToken, req.PrNumber);
            var hasAnyUrl = !string.IsNullOrWhiteSpace(urls.DocsUrl) || !string.IsNullOrWhiteSpace(urls.SamplesUrl);

            // Check if any individual service already has an error event
            // (so we can show error details even while other service is still pending).
            var hasAnyFailureEvent = docsEvents.Any(SliplaneDeploymentStatusResolver.IsFailEvent)
                                    || samplesEvents.Any(SliplaneDeploymentStatusResolver.IsFailEvent);

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
                var bothUrlsReady = !string.IsNullOrWhiteSpace(urls.DocsUrl) && !string.IsNullOrWhiteSpace(urls.SamplesUrl);
                if (bothUrlsReady || iteration > 25)
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
                // Deployed but managed domains not ready yet — keep polling silently.
            }
            // combined == "pending": poll silently, no intermediate comment

            await Task.Delay(delayMs, ct);
            if (delayMs < maxDelayMs)
                delayMs = Math.Min(maxDelayMs, (int)(delayMs * 1.25));
        }
        // App shutting down — do nothing, startup scan will resume on next deploy.
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

