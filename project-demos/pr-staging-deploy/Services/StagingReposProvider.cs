namespace PrStagingDeploy.Services;

using PrStagingDeploy.Models;

/// <summary>
/// Reads the <c>Repos</c> array from configuration (multi-repo PR staging).
/// If <c>Repos</c> is not configured, falls back to the legacy single-repo keys
/// (<c>GitHub:Owner</c>, <c>GitHub:Repo</c>, <c>Staging:DocsRepo</c>, <c>Staging:SamplesRepo</c>, …)
/// so existing single-repo deployments keep working without any config changes.
/// </summary>
public sealed class StagingReposProvider
{
    private readonly IReadOnlyList<StagingRepoConfig> _repos;

    public StagingReposProvider(IConfiguration config, ILogger<StagingReposProvider> logger)
    {
        var configured = config.GetSection("Repos").Get<List<StagingRepoConfig>>() ?? new();
        configured = configured
            .Where(r => !string.IsNullOrWhiteSpace(r.Owner) && !string.IsNullOrWhiteSpace(r.Repo))
            .ToList();

        foreach (var r in configured)
        {
            if (string.IsNullOrWhiteSpace(r.Key))
                r.Key = SanitizeKey(r.Repo);
            else
                r.Key = SanitizeKey(r.Key);
        }

        if (configured.Count == 0)
        {
            var legacy = BuildLegacyEntry(config);
            if (legacy != null)
            {
                logger.LogInformation(
                    "Repos[] not configured; using legacy single-repo entry for {Owner}/{Repo}",
                    legacy.Owner, legacy.Repo);
                configured.Add(legacy);
            }
            else
            {
                logger.LogWarning("No staging repos configured (neither Repos[] nor legacy GitHub:Owner/Repo).");
            }
        }

        // Detect duplicate keys; rename collisions deterministically.
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

    public IReadOnlyList<StagingRepoConfig> All => _repos;

    public StagingRepoConfig? FindByOwnerRepo(string? owner, string? repo)
    {
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return null;
        return _repos.FirstOrDefault(r =>
            r.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) &&
            r.Repo.Equals(repo, StringComparison.OrdinalIgnoreCase));
    }

    public StagingRepoConfig? FindByKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _repos.FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Sluggify into Sliplane-safe alphanumeric+hyphen lowercase token.</summary>
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

    /// <summary>Build a single repo entry from the legacy keys, when Repos[] is empty.</summary>
    private static StagingRepoConfig? BuildLegacyEntry(IConfiguration config)
    {
        var owner = config["GitHub:Owner"];
        var repo = config["GitHub:Repo"];
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return null;

        var docsRepo = config["Staging:DocsRepo"];
        var samplesRepo = config["Staging:SamplesRepo"];

        var entry = new StagingRepoConfig
        {
            Key = SanitizeKey(repo),
            Owner = owner!,
            Repo = repo!,
        };

        if (!string.IsNullOrWhiteSpace(docsRepo))
        {
            entry.Docs = new StagingComponentConfig
            {
                Repo = docsRepo!,
                Dockerfile = config["Staging:DocsDockerfile"] ?? "Dockerfile",
                Context = config["Staging:DocsDockerContext"] ?? ".",
            };
        }

        if (!string.IsNullOrWhiteSpace(samplesRepo))
        {
            entry.Samples = new StagingComponentConfig
            {
                Repo = samplesRepo!,
                Dockerfile = config["Staging:SamplesDockerfile"] ?? "Dockerfile",
                Context = config["Staging:SamplesDockerContext"] ?? ".",
            };
        }

        // If neither Docs nor Samples specified, use repo as both for backward compat.
        if (entry.Docs == null && entry.Samples == null)
        {
            entry.Docs = new StagingComponentConfig
            {
                Repo = $"https://github.com/{owner}/{repo}",
                Dockerfile = config["Staging:DocsDockerfile"] ?? "Dockerfile",
                Context = config["Staging:DocsDockerContext"] ?? ".",
            };
            entry.Samples = new StagingComponentConfig
            {
                Repo = $"https://github.com/{owner}/{repo}",
                Dockerfile = config["Staging:SamplesDockerfile"] ?? "Dockerfile",
                Context = config["Staging:SamplesDockerContext"] ?? ".",
            };
        }

        return entry;
    }
}
