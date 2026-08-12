namespace TendrilPrStaging.Services;

using TendrilPrStaging.Models;

/// <summary>
/// Reads Tendril repo configs from configuration.
/// Supports <c>Repos[]</c> array; falls back to legacy <c>GitHub:Owner</c> / <c>GitHub:Repo</c>.
/// </summary>
public sealed class TendrilReposProvider
{
    private readonly IReadOnlyList<TendrilRepoConfig> _repos;

    public TendrilReposProvider(IConfiguration config, ILogger<TendrilReposProvider> logger)
    {
        var configured = config.GetSection("Repos").Get<List<TendrilRepoConfig>>() ?? new();
        configured = configured
            .Where(r => !string.IsNullOrWhiteSpace(r.Owner) && !string.IsNullOrWhiteSpace(r.Repo))
            .ToList();

        foreach (var r in configured)
            r.Key = string.IsNullOrWhiteSpace(r.Key) ? SanitizeKey(r.Repo) : SanitizeKey(r.Key);

        if (configured.Count == 0)
        {
            var owner = config["GitHub:Owner"];
            var repo = config["GitHub:Repo"];
            if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo))
            {
                logger.LogInformation("Repos[] not configured; using legacy GitHub:Owner/Repo {Owner}/{Repo}", owner, repo);
                configured.Add(new TendrilRepoConfig
                {
                    Key = SanitizeKey(repo!),
                    Owner = owner!,
                    Repo = repo!,
                    DockerfilePath = config["Staging:DockerfilePath"] ?? ".github/docker/Dockerfile.tendril-release",
                    DockerContext = config["Staging:DockerContext"] ?? ".",
                });
            }
            else
            {
                logger.LogWarning("No repos configured (neither Repos[] nor legacy GitHub:Owner/Repo).");
            }
        }

        // Deduplicate keys.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in configured)
        {
            var baseKey = string.IsNullOrEmpty(r.Key) ? "repo" : r.Key;
            var key = baseKey;
            var n = 2;
            while (!seen.Add(key))
                key = $"{baseKey}-{n++}";
            r.Key = key;
        }

        _repos = configured;
    }

    public IReadOnlyList<TendrilRepoConfig> All => _repos;

    public TendrilRepoConfig? FindByOwnerRepo(string? owner, string? repo)
    {
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return null;
        return _repos.FirstOrDefault(r =>
            r.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) &&
            r.Repo.Equals(repo, StringComparison.OrdinalIgnoreCase));
    }

    public TendrilRepoConfig? FindByKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _repos.FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static string SanitizeKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "repo";
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var ch in raw.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "repo" : slug;
    }
}
