namespace PrStagingDeploy.Services;

using Microsoft.Extensions.Hosting;
using PrStagingDeploy.Models;

/// <summary>
/// Polls Sliplane events for newly deployed services concurrently (each PR gets its own Task).
/// Does nothing on success — links are already in the PR comment.
/// On build/deploy failure replaces the comment with an error message.
/// Polls every 5 s, gives up after 20 min.
/// </summary>
public class StagingErrorWatcherBackgroundService : BackgroundService
{
    private readonly StagingErrorWatcherQueue _queue;
    private readonly SliplaneStagingClient _sliplane;
    private readonly PrStagingDeployCommentService _comments;
    private readonly IConfiguration _config;
    private readonly ILogger<StagingErrorWatcherBackgroundService> _logger;

    private const int MaxWatchMinutes = 20;
    private const int PollIntervalMs = 5000;

    public StagingErrorWatcherBackgroundService(
        StagingErrorWatcherQueue queue,
        SliplaneStagingClient sliplane,
        PrStagingDeployCommentService comments,
        IConfiguration config,
        ILogger<StagingErrorWatcherBackgroundService> logger)
    {
        _queue = queue;
        _sliplane = sliplane;
        _comments = comments;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var req in _queue.Channel.Reader.ReadAllAsync(stoppingToken))
            _ = WatchAsync(req, stoppingToken);
    }

    private async Task WatchAsync(StagingErrorWatchRequest req, CancellationToken ct)
    {
        var apiToken = _config["Sliplane:ApiToken"] ?? "";
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(projectId))
            return;

        var deadline = DateTime.UtcNow.AddMinutes(MaxWatchMinutes);

        while (!ct.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollIntervalMs, ct);

            var docsEvents = !string.IsNullOrEmpty(req.DocsServiceId)
                ? await _sliplane.GetServiceEventsAsync(apiToken, projectId, req.DocsServiceId)
                : new List<SliplaneServiceEvent>();

            var samplesEvents = !string.IsNullOrEmpty(req.SamplesServiceId)
                ? await _sliplane.GetServiceEventsAsync(apiToken, projectId, req.SamplesServiceId)
                : new List<SliplaneServiceEvent>();

            var docsState = ResolveState(req.DocsServiceId, docsEvents);
            var samplesState = ResolveState(req.SamplesServiceId, samplesEvents);

            if (docsState == "pending" || samplesState == "pending")
                continue;

            _logger.LogInformation(
                "PR #{Pr} build terminal: docs={Docs} samples={Samples}",
                req.PrNumber, docsState, samplesState);

            if (docsState == "failed" || samplesState == "failed")
            {
                var errLines = BuildErrorLines(docsState, samplesState, docsEvents, samplesEvents);
                await _comments.TryPostStagingAsync(
                    req.Owner, req.Repo, req.PrNumber,
                    docsUrl: null, samplesUrl: null,
                    error: string.Join(" | ", errLines),
                    cancellationToken: ct);
            }
            // success — PR comment already has links, nothing to do
            return;
        }

        if (DateTime.UtcNow >= deadline)
            _logger.LogWarning(
                "PR #{Pr} build watcher timed out after {Min} min", req.PrNumber, MaxWatchMinutes);
    }

    private static string ResolveState(string? serviceId, List<SliplaneServiceEvent> events)
    {
        if (string.IsNullOrEmpty(serviceId)) return "skipped";
        if (events.Count == 0) return "pending";

        var relevant = events
            .Where(IsDeployEvent)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        if (relevant.Count == 0) return "pending";
        var last = relevant[0];
        if (IsFailEvent(last)) return "failed";
        if (IsSuccessEvent(last)) return "deployed";
        return "pending";
    }

    private static bool IsDeployEvent(SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t.Contains("deploy") || t.Contains("build");
    }

    private static bool IsSuccessEvent(SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t is "service_deploy_success" or "service_resume_success";
    }

    private static bool IsFailEvent(SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t is "service_deploy_failed" or "service_build_failed";
    }

    private static IEnumerable<string> BuildErrorLines(
        string docsState,
        string samplesState,
        List<SliplaneServiceEvent> docsEvents,
        List<SliplaneServiceEvent> samplesEvents)
    {
        if (docsState == "failed")
        {
            var last = docsEvents.Where(IsFailEvent).OrderByDescending(e => e.CreatedAt).FirstOrDefault();
            yield return last != null ? $"Docs: {Trunc(last.Message ?? last.Type, 200)}" : "Docs: build failed";
        }

        if (samplesState == "failed")
        {
            var last = samplesEvents.Where(IsFailEvent).OrderByDescending(e => e.CreatedAt).FirstOrDefault();
            yield return last != null ? $"Samples: {Trunc(last.Message ?? last.Type, 200)}" : "Samples: build failed";
        }
    }

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().Replace("\r", "").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "…";
    }
}
