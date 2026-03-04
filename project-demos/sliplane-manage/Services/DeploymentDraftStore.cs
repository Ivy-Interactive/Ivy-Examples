namespace SliplaneManage.Services;

/// <summary>
/// Very simple in-memory store for the last used deployment repository URL.
/// Lives for the lifetime of the Sliplane Manage process.
/// </summary>
public static class DeploymentDraftStore
{
    private static readonly object _lock = new();
    private static string? _lastRepoUrl;

    public static string? LastRepoUrl
    {
        get
        {
            lock (_lock)
                return _lastRepoUrl;
        }
    }

    public static void SaveRepoUrl(string? repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
            return;

        lock (_lock)
        {
            _lastRepoUrl = repoUrl;
        }
    }
}

