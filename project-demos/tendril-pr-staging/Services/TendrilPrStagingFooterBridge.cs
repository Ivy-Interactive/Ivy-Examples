namespace TendrilPrStaging.Services;

public static class TendrilPrStagingFooterBridge
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
