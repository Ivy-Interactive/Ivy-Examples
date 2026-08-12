namespace TendrilPrStaging.Services;

public static class GitHubDeployPermissions
{
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
