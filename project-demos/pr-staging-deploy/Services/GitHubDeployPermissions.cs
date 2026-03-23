namespace PrStagingDeploy.Services;

/// <summary>
/// Optional <c>GitHub:DeployAllowedUsers</c> (comma-separated GitHub logins).
/// When empty/unset, deploy is not restricted by user. When set, only listed users pass the check.
/// </summary>
public static class GitHubDeployPermissions
{
    /// <summary>
    /// Used for auto-deploy on PR events (author must be listed) and for <c>/deploy</c> (comment author must be listed).
    /// </summary>
    public static bool IsUserAllowed(IConfiguration config, string? githubLogin)
    {
        if (string.IsNullOrWhiteSpace(githubLogin))
            return false;

        var login = githubLogin.Trim();
        var allowed = ParseLoginSet(config["GitHub:DeployAllowedUsers"]);

        if (allowed.Count == 0)
            return true;

        return allowed.Contains(login, StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> ParseLoginSet(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
