namespace TendrilPrStaging.Services;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Polls Sliplane events for a newly created staging service.
/// On build/deploy failure replaces the PR comment with an error message.
/// Polls every 5 s, gives up after 20 min.
/// </summary>
public class TendrilErrorWatcherBackgroundService : BackgroundService
{
    private readonly TendrilErrorWatcherQueue _queue;
    private readonly SliplaneStagingClient _sliplane;
    private readonly TendrilPrCommentService _comments;
    private readonly IConfiguration _config;
    private readonly ILogger<TendrilErrorWatcherBackgroundService> _logger;

    private const int MaxWatchMinutes = 20;
    private const int PollIntervalMs = 5000;

    public TendrilErrorWatcherBackgroundService(
        TendrilErrorWatcherQueue queue,
        SliplaneStagingClient sliplane,
        TendrilPrCommentService comments,
        IConfiguration config,
        ILogger<TendrilErrorWatcherBackgroundService> logger)
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

    private async Task WatchAsync(TendrilErrorWatchRequest req, CancellationToken ct)
    {
        var apiToken = _config["Sliplane:ApiToken"] ?? "";
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(projectId))
            return;

        var deadline = DateTime.UtcNow.AddMinutes(MaxWatchMinutes);

        while (!ct.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollIntervalMs, ct);

            var events = await _sliplane.GetServiceEventsAsync(apiToken, projectId, req.ServiceId);
            var state = ResolveState(events);

            if (state == "pending")
                continue;

            _logger.LogInformation("PR #{Pr} ({RepoKey}) build terminal: {State}", req.PrNumber, req.RepoKey, state);

            if (state == "failed")
            {
                var last = events
                    .Where(e => IsFailEvent(e))
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefault();
                var errMsg = last != null ? Trunc(last.Message ?? last.Type, 200) : "build failed";
                await _comments.TryPostStagingAsync(req.Owner, req.Repo, req.PrNumber,
                    serviceUrl: null, error: errMsg, cancellationToken: ct);
            }
            return;
        }

        if (DateTime.UtcNow >= deadline)
            _logger.LogWarning("PR #{Pr} ({RepoKey}) build watcher timed out after {Min} min", req.PrNumber, req.RepoKey, MaxWatchMinutes);
    }

    private static string ResolveState(List<Models.SliplaneServiceEvent> events)
    {
        if (events.Count == 0) return "pending";
        var relevant = events.Where(IsDeployEvent).OrderByDescending(e => e.CreatedAt).ToList();
        if (relevant.Count == 0) return "pending";
        var last = relevant[0];
        if (IsFailEvent(last)) return "failed";
        if (IsSuccessEvent(last)) return "deployed";
        return "pending";
    }

    private static bool IsDeployEvent(Models.SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t.Contains("deploy") || t.Contains("build");
    }

    private static bool IsSuccessEvent(Models.SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t is "service_deploy_success" or "service_resume_success";
    }

    private static bool IsFailEvent(Models.SliplaneServiceEvent e)
    {
        var t = (e.Type ?? "").ToLowerInvariant();
        return t is "service_deploy_failed" or "service_build_failed";
    }

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().Replace("\r", "").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "…";
    }
}
