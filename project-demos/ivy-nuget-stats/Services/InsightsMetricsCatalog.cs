namespace IvyInsights.Services;

/// <summary>NuGet package + GitHub repo pair exposed in the Ivy Insights API and dashboard tabs.</summary>
public readonly record struct MetricsTarget(string PackageId, string RepoName, string DisplayName)
{
    public static MetricsTarget Ivy { get; } = new("Ivy", GithubRepoCatalog.IvyFramework, "Ivy Framework");
    public static MetricsTarget Tendril { get; } = new("Ivy.Tendril", GithubRepoCatalog.IvyTendril, "Ivy Tendril");

    public static readonly IReadOnlyList<MetricsTarget> All = [Ivy, Tendril];
}

/// <summary>Resolves <c>?package=</c> and <c>?repo=</c> query values for the HTTP API.</summary>
public static class InsightsMetricsCatalog
{
    public static bool TryResolve(string? package, string? repo, out MetricsTarget target, out string? error)
    {
        target = default;
        error = null;

        if (string.IsNullOrWhiteSpace(package) && string.IsNullOrWhiteSpace(repo))
        {
            target = MetricsTarget.Ivy;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(repo))
        {
            if (!TryResolvePackage(package!, out var packageId, out error))
                return false;
            if (!TryResolveRepo(repo!, out var repoName, out error))
                return false;

            var expectedRepo = GithubRepoCatalog.GetRepoForPackage(packageId);
            if (!string.Equals(expectedRepo, repoName, StringComparison.Ordinal))
            {
                error =
                    $"package '{packageId}' maps to GitHub repo '{expectedRepo}', but repo '{repoName}' was requested. " +
                    "Omit repo or use a matching value (e.g. package=tendril with repo=Ivy-Interactive/Ivy-Tendril).";
                return false;
            }

            target = new MetricsTarget(packageId, repoName, GithubRepoCatalog.GetDisplayName(repoName));
            return true;
        }

        if (!string.IsNullOrWhiteSpace(package))
        {
            if (!TryResolvePackage(package!, out var packageId, out error))
                return false;

            var repoFromPackage = GithubRepoCatalog.GetRepoForPackage(packageId);
            target = new MetricsTarget(packageId, repoFromPackage, GithubRepoCatalog.GetDisplayName(repoFromPackage));
            return true;
        }

        if (!TryResolveRepo(repo!, out var repoOnly, out error))
            return false;

        var packageFromRepo = GetPackageForRepo(repoOnly);
        target = new MetricsTarget(packageFromRepo, repoOnly, GithubRepoCatalog.GetDisplayName(repoOnly));
        return true;
    }

    public static bool TryResolvePackage(string? value, out string packageId, out string? error)
    {
        error = null;
        packageId = MetricsTarget.Ivy.PackageId;

        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = value.Trim().ToLowerInvariant().Replace('_', '-');
        var resolved = normalized switch
        {
            "ivy" or "framework" or "ivy-framework" => MetricsTarget.Ivy.PackageId,
            "tendril" or "ivy.tendril" or "ivy-tendril" => MetricsTarget.Tendril.PackageId,
            _ when string.Equals(value, MetricsTarget.Ivy.PackageId, StringComparison.OrdinalIgnoreCase) => MetricsTarget.Ivy.PackageId,
            _ when string.Equals(value, MetricsTarget.Tendril.PackageId, StringComparison.OrdinalIgnoreCase) => MetricsTarget.Tendril.PackageId,
            _ => null
        };

        if (resolved is null)
        {
            error = $"Unknown package '{value}'. Use ivy (Ivy Framework) or tendril (Ivy.Tendril).";
            return false;
        }

        packageId = resolved;
        return true;
    }

    public static bool TryResolveRepo(string? value, out string repoName, out string? error)
    {
        error = null;
        repoName = MetricsTarget.Ivy.RepoName;

        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = value.Trim().ToLowerInvariant().Replace('_', '-');
        var resolved = normalized switch
        {
            "framework" or "ivy" or "ivy-framework" => GithubRepoCatalog.IvyFramework,
            "tendril" or "ivy-tendril" => GithubRepoCatalog.IvyTendril,
            _ when string.Equals(value, GithubRepoCatalog.IvyFramework, StringComparison.OrdinalIgnoreCase) => GithubRepoCatalog.IvyFramework,
            _ when string.Equals(value, GithubRepoCatalog.IvyTendril, StringComparison.OrdinalIgnoreCase) => GithubRepoCatalog.IvyTendril,
            _ => null
        };

        if (resolved is null)
        {
            error =
                $"Unknown repo '{value}'. Use framework, tendril, or full name (e.g. {GithubRepoCatalog.IvyTendril}).";
            return false;
        }

        repoName = resolved;
        return true;
    }

    public static string GetPackageForRepo(string repoName) => repoName switch
    {
        GithubRepoCatalog.IvyTendril => MetricsTarget.Tendril.PackageId,
        _ => MetricsTarget.Ivy.PackageId,
    };
}
