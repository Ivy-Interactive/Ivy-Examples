namespace PrStagingDeploy.Services;

using PrStagingDeploy.Models;

/// <summary>Orchestrates deploy/delete of docs + samples for a branch across one or more repos.</summary>
public class StagingDeployService
{
    private readonly SliplaneStagingClient _sliplane;
    private readonly GitHubApiClient _github;
    private readonly StagingReposProvider _reposProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<StagingDeployService> _logger;

    public StagingDeployService(
        SliplaneStagingClient sliplane,
        GitHubApiClient github,
        StagingReposProvider reposProvider,
        IConfiguration config,
        ILogger<StagingDeployService> logger)
    {
        _sliplane = sliplane;
        _github = github;
        _reposProvider = reposProvider;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Configured deployment slug when <c>Staging:DeploymentKey</c> is set; otherwise default <c>ivy</c> for legacy name parsing only.
    /// Actual service names use <see cref="ResolveServiceSlug"/> (DeploymentKey if set, else each repo’s <c>Key</c>).
    /// </summary>
    public string DeploymentKey
    {
        get
        {
            var raw = _config["Staging:DeploymentKey"];
            if (string.IsNullOrWhiteSpace(raw))
                return "ivy";
            return StagingReposProvider.SanitizeKey(raw);
        }
    }

    /// <summary>
    /// Single slug at the start of service names: <c>{slug}-staging-docs-{PR}</c>.
    /// If <c>Staging:DeploymentKey</c> is set, all repos share that slug; otherwise each repo uses its own <paramref name="repo"/>.Key.
    /// </summary>
    public string ResolveServiceSlug(StagingRepoConfig repo)
    {
        var raw = _config["Staging:DeploymentKey"];
        if (!string.IsNullOrWhiteSpace(raw))
            return StagingReposProvider.SanitizeKey(raw);
        return repo.Key;
    }

    public int ExpiryDays => int.TryParse(_config["Staging:ExpiryDays"], out var d) ? d : 7;

    /// <summary>Pause (ms) after tearing down old services and before calling Sliplane create. Lets merge/close webhooks win the race. Default 1200; set 0 to disable.</summary>
    public int PreDeployDelayMs =>
        int.TryParse(_config["Staging:PreDeployDelayMs"], out var ms) ? Math.Max(0, ms) : 1200;

    public IReadOnlyList<StagingRepoConfig> AllRepos => _reposProvider.All;

    public StagingRepoConfig? FindRepoByKey(string? key) => _reposProvider.FindByKey(key);

    public StagingRepoConfig? FindRepoByOwner(string? owner, string? repo) =>
        _reposProvider.FindByOwnerRepo(owner, repo);

    /// <summary>Sliplane service name: <c>{slug}-staging-docs-{PR}</c>.</summary>
    public string DocsServiceName(StagingRepoConfig repo, int prNumber)
        => $"{ResolveServiceSlug(repo)}-staging-docs-{prNumber}";

    /// <summary>Sliplane service name: <c>{slug}-staging-samples-{PR}</c>.</summary>
    public string SamplesServiceName(StagingRepoConfig repo, int prNumber)
        => $"{ResolveServiceSlug(repo)}-staging-samples-{prNumber}";

    public async Task<StagingDeployResult> DeployBranchAsync(
        string apiToken,
        StagingRepoConfig repoConfig,
        string branchName,
        int prNumber,
        string? cloneUrlOverride = null,
        CancellationToken cancellationToken = default)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var serverId = _config["Sliplane:ServerId"] ?? "";
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serverId))
            return new StagingDeployResult(false, "Sliplane:ProjectId and ServerId required.");

        var skip = await TryGetClosedOrMergedSkipAsync(prNumber, repoConfig.Owner, repoConfig.Repo, cancellationToken);
        if (skip is not null)
            return skip;

        // Tear down any existing services for this PR (within this repo) before creating new ones.
        await DeleteBranchAsync(apiToken, repoConfig, prNumber);

        var ghToken = _config["GitHub:Token"] ?? "";
        if (PreDeployDelayMs > 0
            && !string.IsNullOrEmpty(repoConfig.Owner)
            && !string.IsNullOrEmpty(repoConfig.Repo)
            && !string.IsNullOrEmpty(ghToken))
        {
            await Task.Delay(PreDeployDelayMs, cancellationToken);
            skip = await TryGetClosedOrMergedSkipAsync(prNumber, repoConfig.Owner, repoConfig.Repo, cancellationToken);
            if (skip is not null)
                return skip;
        }

        string? docsUrl = null;
        string? samplesUrl = null;
        string? docsId = null;
        string? samplesId = null;
        string? lastError = null;
        var anyAttempted = false;

        try
        {
            if (repoConfig.HasDocs)
            {
                anyAttempted = true;
                var docsResult = await _sliplane.CreateServiceAsync(
                    apiToken, projectId, serverId,
                    DocsServiceName(repoConfig, prNumber),
                    repoConfig.Docs!.Repo, branchName,
                    repoConfig.Docs.Dockerfile, repoConfig.Docs.Context);
                if (docsResult.Service != null)
                {
                    docsId = docsResult.Service.Id;
                    docsUrl = string.IsNullOrEmpty(docsResult.Service.ManagedDomain)
                        ? null
                        : "https://" + docsResult.Service.ManagedDomain;
                }
                else if (!string.IsNullOrEmpty(docsResult.Error))
                {
                    lastError = docsResult.Error;
                }
            }

            if (repoConfig.HasSamples)
            {
                anyAttempted = true;
                // For fork PRs use the fork's clone URL so Sliplane can find the branch.
                var samplesRepo = !string.IsNullOrEmpty(cloneUrlOverride) ? cloneUrlOverride : repoConfig.Samples!.Repo;
                var samplesResult = await _sliplane.CreateServiceAsync(
                    apiToken, projectId, serverId,
                    SamplesServiceName(repoConfig, prNumber),
                    samplesRepo, branchName,
                    repoConfig.Samples!.Dockerfile, repoConfig.Samples.Context);
                if (samplesResult.Service != null)
                {
                    samplesId = samplesResult.Service.Id;
                    samplesUrl = string.IsNullOrEmpty(samplesResult.Service.ManagedDomain)
                        ? null
                        : "https://" + samplesResult.Service.ManagedDomain;
                }
                else if (!string.IsNullOrEmpty(samplesResult.Error))
                {
                    lastError = samplesResult.Error;
                }
            }

            if (!anyAttempted)
                return new StagingDeployResult(false, $"Repo {repoConfig.Key} has neither Docs nor Samples configured.");

            var ok = docsId != null || samplesId != null;
            var msg = ok
                ? FormatDeployMessage(repoConfig, docsUrl, samplesUrl)
                : (lastError ?? "Failed to create services.");
            return new StagingDeployResult(ok, msg, docsUrl, samplesUrl, docsId, samplesId);
        }
        catch (Exception ex)
        {
            return new StagingDeployResult(false, ex.Message);
        }
    }

    private static string FormatDeployMessage(StagingRepoConfig repo, string? docsUrl, string? samplesUrl)
    {
        var parts = new List<string>();
        if (repo.HasDocs) parts.Add($"docs={docsUrl ?? "pending"}");
        if (repo.HasSamples) parts.Add($"samples={samplesUrl ?? "pending"}");
        return $"Deployed: {string.Join(", ", parts)}";
    }

    public async Task<StagingDeployResult> RedeployBranchAsync(string apiToken, StagingRepoConfig repoConfig, string branchName, int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);
        var docsName = DocsServiceName(repoConfig, prNumber);
        var samplesName = SamplesServiceName(repoConfig, prNumber);
        var docsSvc = services.FirstOrDefault(s => string.Equals(s.Name, docsName, StringComparison.OrdinalIgnoreCase));
        var samplesSvc = services.FirstOrDefault(s => string.Equals(s.Name, samplesName, StringComparison.OrdinalIgnoreCase));

        var triggered = 0;
        if (docsSvc != null && await _sliplane.RedeployServiceAsync(apiToken, projectId, docsSvc.Id))
            triggered++;
        if (samplesSvc != null && await _sliplane.RedeployServiceAsync(apiToken, projectId, samplesSvc.Id))
            triggered++;

        // Suppress unused parameter warning while keeping signature stable.
        _ = branchName;
        return new StagingDeployResult(triggered > 0, $"Redeploy triggered for {triggered} service(s).");
    }

    /// <summary>Resolves docs/samples URLs from existing Sliplane services for a PR (e.g. after redeploy).</summary>
    public async Task<(string? DocsUrl, string? SamplesUrl)> GetDeploymentUrlsForPrAsync(string apiToken, StagingRepoConfig repoConfig, int prNumber)
    {
        var dep = await GetDeploymentByPrNumberAsync(apiToken, repoConfig, prNumber);
        if (dep == null) return (null, null);
        return (dep.DocsUrl, dep.SamplesUrl);
    }

    public async Task<(List<SliplaneServiceEvent> DocsEvents, List<SliplaneServiceEvent> SamplesEvents)> GetDeploymentEventsForServicesAsync(
        string apiToken,
        string? docsServiceId,
        string? samplesServiceId)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        if (string.IsNullOrEmpty(projectId))
            return (new List<SliplaneServiceEvent>(), new List<SliplaneServiceEvent>());

        var docsEvents = !string.IsNullOrEmpty(docsServiceId)
            ? await _sliplane.GetServiceEventsAsync(apiToken, projectId, docsServiceId)
            : new List<SliplaneServiceEvent>();

        var samplesEvents = !string.IsNullOrEmpty(samplesServiceId)
            ? await _sliplane.GetServiceEventsAsync(apiToken, projectId, samplesServiceId)
            : new List<SliplaneServiceEvent>();

        return (docsEvents, samplesEvents);
    }

    public async Task<StagingDeployment?> GetDeploymentByPrNumberAsync(string apiToken, StagingRepoConfig repoConfig, int prNumber)
    {
        var list = await ListDeploymentsAsync(apiToken);
        return list.FirstOrDefault(d =>
            d.RepoKey.Equals(repoConfig.Key, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(d.BranchSafe, prNumber.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<StagingDeleteResult> DeleteBranchAsync(string apiToken, StagingRepoConfig repoConfig, int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";

        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);
        // Match by parsed name so legacy formats (e.g. ivy-tendril-staging-ivy-tendril-docs-1) are deleted,
        // not only the current canonical DocsServiceName/SamplesServiceName strings.
        var toDelete = services
            .Where(s =>
            {
                var parsed = ParseServiceName(s.Name ?? "");
                return parsed != null
                       && parsed.Value.RepoKey.Equals(repoConfig.Key, StringComparison.OrdinalIgnoreCase)
                       && parsed.Value.PrNumber == prNumber;
            })
            .ToList();

        var deleteTasks = toDelete.Select(svc => DeleteWithRetryAsync(apiToken, projectId, svc.Id));
        var results = await Task.WhenAll(deleteTasks);
        var deleted = results.Count(r => r);

        return new StagingDeleteResult(deleted > 0, $"Deleted {deleted} service(s).");
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

    /// <summary>Lists deployments across all configured repos (parses repoKey out of the service name).</summary>
    public async Task<List<StagingDeployment>> ListDeploymentsAsync(string apiToken)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListAllServicesAsync(apiToken, projectId);

        // Composite key: "{repoKey}|{prNumber}"
        var byBranch = new Dictionary<string, (string RepoKey, string PrNumber, string? DocsId, string? DocsUrl, string? DocsStatus, string? SamplesId, string? SamplesUrl, string? SamplesStatus, DateTime Oldest)>();

        foreach (var svc in services)
        {
            var name = svc.Name ?? "";
            var parsed = ParseServiceName(name);
            if (parsed == null) continue;

            var (repoKey, type, prNumber) = parsed.Value;
            var url = string.IsNullOrEmpty(svc.ManagedDomain) ? null : "https://" + svc.ManagedDomain;
            var status = svc.Status ?? "live";
            var compositeKey = $"{repoKey}|{prNumber}";

            if (!byBranch.TryGetValue(compositeKey, out var cur))
                cur = (repoKey, prNumber.ToString(), null, null, null, null, null, null, svc.CreatedAt);

            var oldest = cur.Oldest < svc.CreatedAt ? cur.Oldest : svc.CreatedAt;
            if (type == "docs")
                cur = (cur.RepoKey, cur.PrNumber, svc.Id, url, status, cur.SamplesId, cur.SamplesUrl, cur.SamplesStatus, oldest);
            else
                cur = (cur.RepoKey, cur.PrNumber, cur.DocsId, cur.DocsUrl, cur.DocsStatus, svc.Id, url, status, oldest);

            byBranch[compositeKey] = cur;
        }

        return byBranch.Select(kv => new StagingDeployment(
            RepoKey: kv.Value.RepoKey,
            BranchName: kv.Value.PrNumber,
            BranchSafe: kv.Value.PrNumber,
            DocsServiceId: kv.Value.DocsId,
            DocsUrl: kv.Value.DocsUrl,
            DocsStatus: kv.Value.DocsStatus,
            SamplesServiceId: kv.Value.SamplesId,
            SamplesUrl: kv.Value.SamplesUrl,
            SamplesStatus: kv.Value.SamplesStatus,
            DeployedAt: kv.Value.Oldest,
            ExpiresAt: kv.Value.Oldest.AddDays(ExpiryDays),
            Status: "live"
        )).OrderByDescending(d => d.DeployedAt).ToList();
    }

    /// <summary>
    /// Current format: <c>{slug}-staging-docs-{pr}</c> / <c>{slug}-staging-samples-{pr}</c> where <c>slug</c> is <see cref="ResolveServiceSlug"/>.
    /// Legacy format (still recognized): <c>{deploymentKey}-staging-{repoKey}-docs-{pr}</c> from older builds.
    /// </summary>
    private (string RepoKey, string Type, int PrNumber)? ParseServiceName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // New format: single slug, then -staging-docs- or -staging-samples-
        foreach (var rc in _reposProvider.All.OrderByDescending(r => ResolveServiceSlug(r).Length))
        {
            var slug = ResolveServiceSlug(rc);
            foreach (var (type, marker) in new[] { ("docs", "-staging-docs-"), ("samples", "-staging-samples-") })
            {
                var prefix = slug + marker;
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var tail = name[prefix.Length..];
                if (int.TryParse(tail, out var pr))
                    return (rc.Key, type, pr);
            }
        }

        // Legacy: {DeploymentKey}-staging-{repoKey}-docs-{pr}
        var dkStaging = $"{DeploymentKey}-staging-";
        if (name.StartsWith(dkStaging, StringComparison.OrdinalIgnoreCase))
        {
            var rest = name[dkStaging.Length..];
            var byLength = _reposProvider.All.OrderByDescending(r => r.Key.Length).ToList();
            foreach (var rc in byLength)
            {
                var keyDash = rc.Key + "-";
                if (!rest.StartsWith(keyDash, StringComparison.OrdinalIgnoreCase)) continue;
                var inner = rest[keyDash.Length..];
                var parsed = ParseTypeAndPr(inner);
                if (parsed != null)
                    return (rc.Key, parsed.Value.Type, parsed.Value.PrNumber);
            }

            if (_reposProvider.All.Count == 1)
            {
                var legacy = ParseTypeAndPr(rest);
                if (legacy != null)
                    return (_reposProvider.All[0].Key, legacy.Value.Type, legacy.Value.PrNumber);
            }
        }

        // Legacy B: any leading slug — <anything>-staging-{repoKey}-docs-{pr} (deployment prefix drift vs config)
        foreach (var rc in _reposProvider.All.OrderByDescending(r => r.Key.Length))
        {
            foreach (var (type, middle) in new[] { ("docs", $"-staging-{rc.Key}-docs-"), ("samples", $"-staging-{rc.Key}-samples-") })
            {
                var idx = name.IndexOf(middle, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var tail = name[(idx + middle.Length)..];
                if (int.TryParse(tail, out var pr))
                    return (rc.Key, type, pr);
            }
        }

        return null;
    }

    private static (string Type, int PrNumber)? ParseTypeAndPr(string remainder)
    {
        const string docs = "docs-";
        const string samples = "samples-";
        if (remainder.StartsWith(docs, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(remainder[docs.Length..], out var d))
            return ("docs", d);
        if (remainder.StartsWith(samples, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(remainder[samples.Length..], out var s))
            return ("samples", s);
        return null;
    }

    /// <summary>Deletes deployments that are past ExpiryDays AND whose PR is closed (per-repo PR lookup).</summary>
    public async Task<StagingDeleteResult> DeleteExpiredAsync(string apiToken)
    {
        var deployments = await ListDeploymentsAsync(apiToken);
        var expired = deployments.Where(d => d.ExpiresAt < DateTime.UtcNow).ToList();
        if (expired.Count == 0)
            return new StagingDeleteResult(false, "No expired deployments.");

        var ghToken = _config["GitHub:Token"] ?? "";

        // Cache open PR lists per (owner, repo) so we don't refetch.
        var openPrCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        async Task<HashSet<string>> GetOpenPrsAsync(StagingRepoConfig rc)
        {
            var key = $"{rc.Owner}/{rc.Repo}";
            if (openPrCache.TryGetValue(key, out var cached)) return cached;
            var prs = await _github.GetPullRequestsAsync(rc.Owner, rc.Repo, ghToken, "open");
            var set = new HashSet<string>(prs.Select(p => p.Number.ToString()), StringComparer.OrdinalIgnoreCase);
            openPrCache[key] = set;
            return set;
        }

        var deleted = 0;
        foreach (var d in expired)
        {
            var rc = _reposProvider.FindByKey(d.RepoKey);
            if (rc == null) continue;

            if (!int.TryParse(d.BranchSafe, out var prNum)) continue;

            var openPrs = await GetOpenPrsAsync(rc);
            if (openPrs.Contains(d.BranchSafe)) continue;

            var r = await DeleteBranchAsync(apiToken, rc, prNum);
            if (r.Success) deleted++;
        }

        return new StagingDeleteResult(deleted > 0, $"Deleted {deleted} expired deployment(s) (closed PRs only).");
    }

    /// <returns>Skip result if PR is definitely closed/merged; null to continue (including when GitHub cannot be verified).</returns>
    private async Task<StagingDeployResult?> TryGetClosedOrMergedSkipAsync(
        int prNumber,
        string? gitHubOwner,
        string? gitHubRepo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(gitHubOwner) || string.IsNullOrEmpty(gitHubRepo))
            return null;

        var ghToken = _config["GitHub:Token"] ?? "";
        if (string.IsNullOrEmpty(ghToken))
            return null;

        var info = await _github.GetPullRequestMergeInfoAsync(gitHubOwner, gitHubRepo, prNumber, ghToken, cancellationToken);
        if (!info.Found)
        {
            _logger.LogWarning("Could not verify PR #{Pr} open state on GitHub; continuing deploy.", prNumber);
            return null;
        }

        if (info.IsOpen)
            return null;

        return new StagingDeployResult(
            false,
            "PR is already closed or merged; staging deploy skipped.",
            SkippedBecausePrNotOpen: true);
    }
}

public record StagingDeployResult(
    bool Success,
    string Message,
    string? DocsUrl = null,
    string? SamplesUrl = null,
    string? DocsServiceId = null,
    string? SamplesServiceId = null,
    bool SkippedBecausePrNotOpen = false);
public record StagingDeleteResult(bool Success, string Message);
