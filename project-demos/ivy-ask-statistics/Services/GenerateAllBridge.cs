namespace IvyAskStatistics.Apps;

public static class GenerateAllBridge
{
    static volatile bool _pending;

    public static void Request() => _pending = true;

    public static bool Consume()
    {
        if (!_pending) return false;
        _pending = false;
        return true;
    }
}
