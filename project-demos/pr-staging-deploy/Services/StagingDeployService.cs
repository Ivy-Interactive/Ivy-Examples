namespace PrStagingDeploy.Services;

using PrStagingDeploy.Models;

/// <summary>Orchestrates deploy/delete of docs + samples for a branch.</summary>
public class StagingDeployService
{
    private readonly SliplaneStagingClient _sliplane;
    private readonly GitHubApiClient _github;
    private readonly IConfiguration _config;

    public StagingDeployService(SliplaneStagingClient sliplane, GitHubApiClient github, IConfiguration config)
    {
        _sliplane = sliplane;
        _github = github;
        _config = config;
    }

    public string SamplesRepo => _config["Staging:SamplesRepo"] ?? "https://github.com/Ivy-Interactive/Ivy-Examples";
    public string DocsRepo => _config["Staging:DocsRepo"] ?? "https://github.com/Ivy-Interactive/Ivy-Framework";
    public string SamplesContext => _config["Staging:SamplesDockerContext"] ?? "project-demos/sliplane-manage";
    public string DocsContext => _config["Staging:DocsDockerContext"] ?? "docs";
    public string SamplesDockerfile => _config["Staging:SamplesDockerfile"] ?? "Dockerfile";
    public string DocsDockerfile => _config["Staging:DocsDockerfile"] ?? "Dockerfile";
    public int ExpiryDays => int.TryParse(_config["Staging:ExpiryDays"], out var d) ? d : 7;

    public async Task<StagingDeployResult> DeployBranchAsync(string apiToken, string branchName, int prNumber, string? samplesRepoOverride = null)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var serverId = _config["Sliplane:ServerId"] ?? "";
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serverId))
            return new StagingDeployResult(false, "Sliplane:ProjectId and ServerId required.");

        var docsName = $"ivy-staging-docs-{prNumber}";
        var samplesName = $"ivy-staging-samples-{prNumber}";

        // For fork PRs use the fork's clone URL so Sliplane can find the branch.
        var samplesRepo = !string.IsNullOrEmpty(samplesRepoOverride) ? samplesRepoOverride : SamplesRepo;

        string? docsUrl = null;
        string? samplesUrl = null;
        string? docsId = null;
        string? samplesId = null;

        try
        {
            var docsResult = await _sliplane.CreateServiceAsync(
                apiToken, projectId, serverId,
                docsName, DocsRepo, branchName, DocsDockerfile, DocsContext);
            if (docsResult.Service != null)
            {
                docsId = docsResult.Service.Id;
                docsUrl = string.IsNullOrEmpty(docsResult.Service.ManagedDomain)
                    ? null
                    : "https://" + docsResult.Service.ManagedDomain;
            }

            var samplesResult = await _sliplane.CreateServiceAsync(
                apiToken, projectId, serverId,
                samplesName, samplesRepo, branchName, SamplesDockerfile, SamplesContext);
            if (samplesResult.Service != null)
            {
                samplesId = samplesResult.Service.Id;
                samplesUrl = string.IsNullOrEmpty(samplesResult.Service.ManagedDomain)
                    ? null
                    : "https://" + samplesResult.Service.ManagedDomain;
            }

            var ok = docsResult.Service != null || samplesResult.Service != null;
            var msg = ok
                ? $"Deployed: docs={docsUrl ?? "pending"}, samples={samplesUrl ?? "pending"}"
                : (docsResult.Error ?? samplesResult.Error ?? "Failed to create services.");
            return new StagingDeployResult(ok, msg, docsUrl, samplesUrl, docsId, samplesId);
        }
        catch (Exception ex)
        {
            return new StagingDeployResult(false, ex.Message);
        }
    }

    public async Task<StagingDeployResult> RedeployBranchAsync(string apiToken, string branchName, int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListStagingServicesAsync(apiToken, projectId, "ivy-staging-");
        var docsSvc = services.FirstOrDefault(s => s.Name == $"ivy-staging-docs-{prNumber}");
        var samplesSvc = services.FirstOrDefault(s => s.Name == $"ivy-staging-samples-{prNumber}");

        var triggered = 0;
        if (docsSvc != null && await _sliplane.RedeployServiceAsync(apiToken, projectId, docsSvc.Id))
            triggered++;
        if (samplesSvc != null && await _sliplane.RedeployServiceAsync(apiToken, projectId, samplesSvc.Id))
            triggered++;

        return new StagingDeployResult(triggered > 0, $"Redeploy triggered for {triggered} service(s).");
    }

    /// <summary>Resolves docs/samples URLs from existing Sliplane services for a PR (e.g. after redeploy).</summary>
    public async Task<(string? DocsUrl, string? SamplesUrl)> GetDeploymentUrlsForPrAsync(string apiToken, int prNumber)
    {
        var list = await ListDeploymentsAsync(apiToken);
        var dep = list.FirstOrDefault(d => string.Equals(d.BranchSafe, prNumber.ToString(), StringComparison.OrdinalIgnoreCase));
        if (dep == null)
            return (null, null);
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

    public async Task<StagingDeployment?> GetDeploymentByPrNumberAsync(string apiToken, int prNumber)
    {
        var list = await ListDeploymentsAsync(apiToken);
        return list.FirstOrDefault(d => string.Equals(d.BranchSafe, prNumber.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<StagingDeleteResult> DeleteBranchAsync(string apiToken, int prNumber)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";

        var services = await _sliplane.ListStagingServicesAsync(apiToken, projectId, "ivy-staging-");
        var toDelete = services
            .Where(s => s.Name == $"ivy-staging-docs-{prNumber}" || s.Name == $"ivy-staging-samples-{prNumber}")
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

    public async Task<List<StagingDeployment>> ListDeploymentsAsync(string apiToken)
    {
        var projectId = _config["Sliplane:ProjectId"] ?? "";
        var services = await _sliplane.ListStagingServicesAsync(apiToken, projectId, "ivy-staging-");
        var byBranch = new Dictionary<string, (string? DocsId, string? DocsUrl, string? DocsStatus, string? SamplesId, string? SamplesUrl, string? SamplesStatus, DateTime Oldest)>();

        foreach (var svc in services)
        {
            var name = svc.Name ?? "";
            if (!name.StartsWith("ivy-staging-docs-") && !name.StartsWith("ivy-staging-samples-"))
                continue;
            var branchSafe = name.StartsWith("ivy-staging-docs-")
                ? name["ivy-staging-docs-".Length..]
                : name["ivy-staging-samples-".Length..];
            var url = string.IsNullOrEmpty(svc.ManagedDomain) ? null : "https://" + svc.ManagedDomain;
            var status = svc.Status ?? "live";

            if (!byBranch.TryGetValue(branchSafe, out var cur))
                cur = (null, null, null, null, null, null, svc.CreatedAt);

            var oldest = cur.Oldest < svc.CreatedAt ? cur.Oldest : svc.CreatedAt;
            if (name.StartsWith("ivy-staging-docs-"))
                cur = (svc.Id, url, status, cur.SamplesId, cur.SamplesUrl, cur.SamplesStatus, oldest);
            else
                cur = (cur.DocsId, cur.DocsUrl, cur.DocsStatus, svc.Id, url, status, oldest);

            byBranch[branchSafe] = cur;
        }

        return byBranch.Select(kv => new StagingDeployment(
            BranchName: kv.Key,
            BranchSafe: kv.Key,
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

    /// <summary>Deletes deployments that are past ExpiryDays AND whose PR is closed.</summary>
    public async Task<StagingDeleteResult> DeleteExpiredAsync(string apiToken)
    {
        var deployments = await ListDeploymentsAsync(apiToken);
        var expired = deployments.Where(d => d.ExpiresAt < DateTime.UtcNow).ToList();
        if (expired.Count == 0)
            return new StagingDeleteResult(false, "No expired deployments.");

        var owner = _config["GitHub:Owner"] ?? "";
        var repo = _config["GitHub:Repo"] ?? "";
        var ghToken = _config["GitHub:Token"] ?? "";
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return new StagingDeleteResult(false, "GitHub:Owner/Repo not configured, skipping expiry cleanup.");

        var openPrs = await _github.GetPullRequestsAsync(owner, repo, ghToken, "open");
        var openPrNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pr in openPrs)
            openPrNumbers.Add(pr.Number.ToString());

        var toDelete = expired.Where(d => !openPrNumbers.Contains(d.BranchSafe)).ToList();
        var deleted = 0;
        foreach (var d in toDelete)
        {
            if (!int.TryParse(d.BranchSafe, out var prNum)) continue;
            var r = await DeleteBranchAsync(apiToken, prNum);
            if (r.Success) deleted++;
        }
        return new StagingDeleteResult(deleted > 0, $"Deleted {deleted} expired deployment(s) (closed PRs only).");
    }
}

public record StagingDeployResult(bool Success, string Message, string? DocsUrl = null, string? SamplesUrl = null, string? DocsServiceId = null, string? SamplesServiceId = null);
public record StagingDeleteResult(bool Success, string Message);
