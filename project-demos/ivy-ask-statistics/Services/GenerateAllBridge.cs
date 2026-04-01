namespace IvyAskStatistics.Apps;

public static class GenerateAllBridge
{
    static string? _pending;

    public static void Request(string action) => _pending = action;

    public static string? Consume()
    {
        var val = _pending;
        _pending = null;
        return val;
    }
}
