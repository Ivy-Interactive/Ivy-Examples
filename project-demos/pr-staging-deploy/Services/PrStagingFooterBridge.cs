namespace PrStagingDeploy.Services;

/// <summary>Pending footer actions consumed by the PR Staging app when that view is shown.</summary>
public static class PrStagingFooterBridge
{
    static string? _pending;

    public static void Request(string action) => _pending = action;

    public static string? Consume()
    {
        var v = _pending;
        _pending = null;
        return v;
    }
}
