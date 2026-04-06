namespace IvyAskStatistics.Apps;

public static class GenerateAllBridge
{
    static volatile bool _pending;
    static Action? _flushFromQuestionsView;

    /// <summary>
    /// Footer: set pending and flush immediately if <see cref="QuestionsApp"/> is already mounted.
    /// Otherwise <c>Navigate</c> runs a build that flushes via the registered handler.
    /// (Same-tab <c>Navigate</c> is often a no-op with <c>preventDuplicates</c>, so this avoids a missing dialog.)
    /// </summary>
    public static void Request()
    {
        _pending = true;
        _flushFromQuestionsView?.Invoke();
    }

    /// <summary>True while a footer request is pending and not yet consumed.</summary>
    public static bool IsPending => _pending;

    /// <summary>Latest flush from <see cref="QuestionsApp"/> <c>OnBuild</c>; cleared on unmount.</summary>
    public static void SetFlushHandler(Action? handler) => _flushFromQuestionsView = handler;

    public static bool Consume()
    {
        if (!_pending) return false;
        _pending = false;
        return true;
    }
}
