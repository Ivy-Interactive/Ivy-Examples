namespace TendrilPrStaging.Services;

using TendrilPrStaging.Models;

/// <summary>
/// Orchestrates deploy/redeploy/delete of a single Tendril staging service per PR.
/// Service name pattern: <c>{slug}-tendril-pr-{prNumber}</c>.
/// </summary>
public class TendrilStagingDeployService
{
    private readonly SliplaneStagingClient _sliplane;
    private readonly GitHubApiClient _github;
    private readonly TendrilReposProvider _reposProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<TendrilStagingDeployService> _logger;

    public TendrilStagingDeployService(
        SliplaneStagingClient sliplane,
        GitHubApiClient github,
        TendrilReposProvider reposProvider,
        IConfiguration config,
        ILogger<TendrilStagingDeployService> logger)
    {
        _sliplane = sliplane;
        _github = github;
        _reposProvider = reposProvider;
        _config = config;
        _logger = logger;
    }

    public int ExpiryDays => int.TryParse(_config["Staging:ExpiryDays"], out var d) ? d : 7;

    public int PreDeployDelayMs =>
        int.TryParse(_config["Staging:PreDeployDelayMs"], out var ms) ? Math.Max(0, ms) : 1200;

    public IReadOnlyList<TendrilRepoConfig> AllRepos => _reposProvider.All;

    public TendrilRepoConfig? FindRepoByKey(string? key) => _reposProvider.FindByKey(key);

    public TendrilRepoConfig? FindRepoByOwner(string? owner, string? repo) =>
        _reposProvider.FindByOwnerRepo(owner, repo);

    /// <summary>Sliplane service name: <c>{slug}-tendril-pr-{prNumber}</c>.</summary>
    public string ServiceName(TendrilRepoConfig repo, int prNumber)
        => $"{repo.Key}-tendril-pr-{prNumber}";

    public async Task<TendrilStagingDeployResult> DeployBranchAsync(
        string apiToken,
        TendrilRepoConfig repoConfig,
        string branchName,
        int prNumber,
        string? cloneUrlOverride = null,
        CancellationToken cancellationToken = default)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var serverId = _config["Sliplane:ServerId"] ?? "";
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serverId))
            return new TendrilStagingDeployResult(false, "Sliplane:ProjectId and ServerId required.");

        var skip = await TryGetClosedOrMergedSkipAsync(prNumber, repoConfig.Owner, repoConfig.Repo, cancellationToken);
        if (skip is not null)
            return skip;

        await DeleteBranchAsync(apiToken, repoConfig, prNumber);

        var ghToken = _config["GitHub:Token"] ?? "";
        if (PreDeployDelayMs > 0 && !string.IsNullOrEmpty(ghToken))
        {
            await Task.Delay(PreDeployDelayMs, cancellationToken);
            skip = await TryGetClosedOrMergedSkipAsync(prNumber, repoConfig.Owner, repoConfig.Repo, cancellationToken);
            if (skip is not null)
                return skip;
        }

        try
        {
            var env = BuildEnvVars();
            var gitUrl = !string.IsNullOrWhiteSpace(cloneUrlOverride) ? cloneUrlOverride.Trim() : repoConfig.GitUrl;

            var result = await _sliplane.CreateServiceAsync(
                apiToken, projectId, serverId,
                ServiceName(repoConfig, prNumber),
                gitUrl, branchName,
                repoConfig.DockerfilePath, repoConfig.DockerContext,
                env);

            if (result.Service != null)
            {
                var url = string.IsNullOrEmpty(result.Service.ManagedDomain)
                    ? null
                    : "https://" + result.Service.ManagedDomain;
                return new TendrilStagingDeployResult(true, $"Deployed: {url ?? "pending"}", url, result.Service.Id);
            }

            return new TendrilStagingDeployResult(false, result.Error ?? "Failed to create service.");
        }
        catch (Exception ex)
        {
            return new TendrilStagingDeployResult(false, ex.Message);
        }
    }

    public async Task<TendrilStagingDeployResult> RedeployBranchAsync(
        string apiToken,
        TendrilRepoConfig repoConfig,
        int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);
        var name = ServiceName(repoConfig, prNumber);
        var svc = services.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

        if (svc == null)
            return new TendrilStagingDeployResult(false, "No existing service found.");

        var ok = await _sliplane.RedeployServiceAsync(apiToken, projectId, svc.Id);
        return new TendrilStagingDeployResult(ok, ok ? "Redeploy triggered." : "Redeploy failed.");
    }

    public async Task<TendrilStagingDeleteResult> DeleteBranchAsync(
        string apiToken,
        TendrilRepoConfig repoConfig,
        int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);
        var toDelete = services
            .Where(s => ParseServiceName(s.Name ?? "") is { } p
                && p.RepoKey.Equals(repoConfig.Key, StringComparison.OrdinalIgnoreCase)
                && p.PrNumber == prNumber)
            .ToList();

        var deleteTasks = toDelete.Select(svc => DeleteWithRetryAsync(apiToken, projectId, svc.Id));
        var results = await Task.WhenAll(deleteTasks);
        var deleted = results.Count(r => r);
        return new TendrilStagingDeleteResult(deleted > 0, $"Deleted {deleted} service(s).");
    }

    private async Task<bool> DeleteWithRetryAsync(string apiToken, string projectId, string serviceId, int maxRetries = 3)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            if (await _sliplane.DeleteServiceAsync(apiToken, projectId, serviceId))
                return true;
            if (i < maxRetries - 1)
                await Task.Delay(TimeSpan.FromSeconds(1));
        }
        return false;
    }

    public async Task<List<TendrilStagingDeployment>> ListDeploymentsAsync(string apiToken)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);
        var list = new List<TendrilStagingDeployment>();

        foreach (var svc in services)
        {
            var parsed = ParseServiceName(svc.Name ?? "");
            if (parsed == null) continue;

            var url = string.IsNullOrEmpty(svc.ManagedDomain) ? null : "https://" + svc.ManagedDomain;
            list.Add(new TendrilStagingDeployment(
                RepoKey: parsed.Value.RepoKey,
                PrNumber: parsed.Value.PrNumber,
                ServiceId: svc.Id,
                ServiceUrl: url,
                ServiceStatus: svc.Status,
                DeployedAt: svc.CreatedAt,
                ExpiresAt: svc.CreatedAt.AddDays(ExpiryDays)
            ));
        }

        return list.OrderByDescending(d => d.DeployedAt).ToList();
    }

    public async Task<TendrilStagingDeployment?> GetDeploymentByPrNumberAsync(
        string apiToken,
        TendrilRepoConfig repoConfig,
        int prNumber)
    {
        var list = await ListDeploymentsAsync(apiToken);
        return list.FirstOrDefault(d =>
            d.RepoKey.Equals(repoConfig.Key, StringComparison.OrdinalIgnoreCase) &&
            d.PrNumber == prNumber);
    }

    /// <summary>Deletes expired deployments whose PR is confirmed closed on GitHub.</summary>
    public async Task<TendrilStagingDeleteResult> DeleteExpiredAsync(string apiToken)
    {
        var deployments = await ListDeploymentsAsync(apiToken);
        var expired = deployments.Where(d => d.ExpiresAt < DateTime.UtcNow).ToList();
        if (expired.Count == 0)
            return new TendrilStagingDeleteResult(false, "No expired deployments.");

        var ghToken = _config["GitHub:Token"] ?? "";
        var openPrCache = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        async Task<HashSet<int>> GetOpenPrsAsync(TendrilRepoConfig rc)
        {
            var key = $"{rc.Owner}/{rc.Repo}";
            if (openPrCache.TryGetValue(key, out var cached)) return cached;
            var prs = await _github.GetPullRequestsAsync(rc.Owner, rc.Repo, ghToken, "open");
            var set = prs.Select(p => p.Number).ToHashSet();
            openPrCache[key] = set;
            return set;
        }

        var deleted = 0;
        foreach (var d in expired)
        {
            var rc = _reposProvider.FindByKey(d.RepoKey);
            if (rc == null) continue;

            var openPrs = await GetOpenPrsAsync(rc);
            if (openPrs.Contains(d.PrNumber)) continue;

            var r = await DeleteBranchAsync(apiToken, rc, d.PrNumber);
            if (r.Success) deleted++;
        }

        return new TendrilStagingDeleteResult(deleted > 0, $"Deleted {deleted} expired deployment(s).");
    }

    private List<(string Key, string Value, bool Secret)> BuildEnvVars()
    {
        var list = new List<(string, string, bool)>();

        var apiKey = _config["Tendril:AnthropicApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            list.Add(("ANTHROPIC_API_KEY", apiKey, true));

        var authToken = _config["Tendril:AnthropicAuthToken"];
        if (!string.IsNullOrWhiteSpace(authToken))
            list.Add(("ANTHROPIC_AUTH_TOKEN", authToken, true));

        var baseUrl = _config["Tendril:AnthropicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
            list.Add(("ANTHROPIC_BASE_URL", baseUrl, false));

        var username = _config["Tendril:AuthUsername"];
        if (!string.IsNullOrWhiteSpace(username))
            list.Add(("TENDRIL_AUTH_USERNAME", username, false));

        var password = _config["Tendril:AuthPassword"];
        if (!string.IsNullOrWhiteSpace(password))
            list.Add(("TENDRIL_AUTH_PASSWORD", password, true));

        return list;
    }

    /// <summary>Parses service name format: <c>{repoKey}-tendril-pr-{prNumber}</c>.</summary>
    private (string RepoKey, int PrNumber)? ParseServiceName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        foreach (var rc in _reposProvider.All.OrderByDescending(r => r.Key.Length))
        {
            var prefix = $"{rc.Key}-tendril-pr-";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var tail = name[prefix.Length..];
            if (int.TryParse(tail, out var pr))
                return (rc.Key, pr);
        }

        return null;
    }

    private async Task<TendrilStagingDeployResult?> TryGetClosedOrMergedSkipAsync(
        int prNumber,
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return null;

        var ghToken = _config["GitHub:Token"] ?? "";
        if (string.IsNullOrEmpty(ghToken))
            return null;

        var info = await _github.GetPullRequestMergeInfoAsync(owner, repo, prNumber, ghToken, cancellationToken);
        if (!info.Found)
        {
            _logger.LogWarning("Could not verify PR #{Pr} open state; continuing deploy.", prNumber);
            return null;
        }

        if (info.IsOpen)
            return null;

        return new TendrilStagingDeployResult(
            false,
            "PR is already closed or merged; staging deploy skipped.",
            SkippedBecausePrNotOpen: true);
    }
}

public record TendrilStagingDeployResult(
    bool Success,
    string Message,
    string? ServiceUrl = null,
    string? ServiceId = null,
    bool SkippedBecausePrNotOpen = false);

public record TendrilStagingDeleteResult(bool Success, string Message);
