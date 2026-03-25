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
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(docsServiceId))
            parts.Add(GetStatusFromEvents(docsEvents));
        if (!string.IsNullOrEmpty(samplesServiceId))
            parts.Add(GetStatusFromEvents(samplesEvents));

        if (parts.Count == 0)
            return "pending";

        // If anything is still pending — keep waiting. Don't stop just because one failed.
        if (parts.Exists(p => p == "pending"))
            return "pending";

        // All services resolved: report failed if any failed, otherwise deployed.
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

