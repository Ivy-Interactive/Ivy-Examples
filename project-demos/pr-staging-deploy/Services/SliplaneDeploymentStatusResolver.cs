namespace PrStagingDeploy.Services;

using PrStagingDeploy.Models;

/// <summary>
/// Resolves Sliplane deployment state from docs/samples event timelines.
/// Used to decide what the PR comment should show (pending/failed/deployed).
/// </summary>
public static class SliplaneDeploymentStatusResolver
{
    public static string ResolveCombinedStatus(
        string? docsServiceId,
        List<SliplaneServiceEvent> docsEvents,
        string? samplesServiceId,
        List<SliplaneServiceEvent> samplesEvents)
    {
        var docs = string.IsNullOrEmpty(docsServiceId) ? "skipped" : GetStatusFromEvents(docsEvents);
        var samples = string.IsNullOrEmpty(samplesServiceId) ? "skipped" : GetStatusFromEvents(samplesEvents);
        return ResolveOverallTerminal(docs, samples);
    }

    /// <summary>Per-service state from the events API: <c>pending</c>, <c>deployed</c>, or <c>failed</c>.</summary>
    public static string ResolveStatusFromEvents(List<SliplaneServiceEvent> events) => GetStatusFromEvents(events);

    /// <summary>Maps Sliplane listing <c>status</c> field to pending/deployed/failed.</summary>
    public static string ListingFieldToState(string? listingStatus)
    {
        if (string.IsNullOrWhiteSpace(listingStatus))
            return "pending";
        var s = listingStatus.ToLowerInvariant();
        if (s is "error" or "dead" or "crashed" or "unhealthy")
            return "failed";
        if (s == "live")
            return "deployed";
        return "pending";
    }

    /// <summary>Merge events + listing for one service (same rules as the background worker used for combined).</summary>
    public static string MergeServiceState(string fromEvents, string fromListing)
    {
        if (fromEvents == "failed" || fromListing == "failed")
            return "failed";
        if (fromEvents == "deployed" || fromListing == "deployed")
            return "deployed";
        return "pending";
    }

    /// <param name="docs">skipped | pending | deployed | failed</param>
    /// <param name="samples">skipped | pending | deployed | failed</param>
    /// <returns>pending | deployed | failed | partial</returns>
    public static string ResolveOverallTerminal(string docs, string samples)
    {
        var parts = new List<string>();
        if (docs != "skipped")
            parts.Add(docs);
        if (samples != "skipped")
            parts.Add(samples);

        if (parts.Count == 0)
            return "pending";
        if (parts.Exists(p => p == "pending"))
            return "pending";
        if (parts.Exists(p => p == "deployed") && parts.Exists(p => p == "failed"))
            return "partial";
        if (parts.Exists(p => p == "deployed") && parts.Exists(p => p == "skipped"))
            return "partial";
        if (parts.All(p => p == "deployed"))
            return "deployed";
        if (parts.Exists(p => p == "failed") && parts.Exists(p => p == "skipped"))
            return "partial";
        if (parts.Exists(p => p == "failed"))
            return "failed";
        return "deployed";
    }

    private static string GetStatusFromEvents(List<SliplaneServiceEvent> events)
    {
        if (events.Count == 0)
            return "pending";

        var deployEvents = events.Where(IsDeployEvent).OrderByDescending(e => e.CreatedAt).ToList();
        if (deployEvents.Count == 0)
            return "pending";

        var lastEv = deployEvents.First();
        if (IsFailEvent(lastEv))
            return "failed";
        if (IsPendingEvent(lastEv))
            return "pending";
        if (IsSuccessEvent(lastEv))
            return "deployed";

        return "deployed";
    }

    public static bool IsDeployEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        if (type is "service_resume_success" or "service_suspend_success" or "service_suspend" or "service_resume")
            return true;
        if (type.Contains("deploy") || type.Contains("build")) return true;
        if (msg.Contains("deploy") || msg.Contains("deployed") || msg.Contains("build failed")) return true;
        return false;
    }

    public static bool IsSuccessEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type is "service_deploy_success" or "service_resume_success"
            || msg.Contains("deployed successfully");
    }

    public static bool IsFailEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type is "service_deploy_failed" or "service_build_failed"
            || msg.Contains("deploy failed") || msg.Contains("deployment failed") || msg.Contains("build failed");
    }

    public static bool IsPendingEvent(SliplaneServiceEvent e)
    {
        var type = (e.Type ?? "").ToLowerInvariant();
        var msg = (e.Message ?? "").ToLowerInvariant();
        return type == "service_deploy" || msg.Contains("deploy started");
    }
}

