namespace SliplaneDeploy.Services;

using SliplaneDeploy.Models;

/// <summary>
/// Creates a multi-service stack described by an <see cref="IvyDeployManifest"/>.
///
/// Order of operations:
/// <list type="number">
///   <item>Resolve early-bound <c>{parentServiceName}</c> substitutions and compute generated
///     env values for every child.</item>
///   <item>Create each child via <see cref="SliplaneApiClient.CreateServiceAsync"/>, polling
///     until <c>network.internalDomain</c> is known so late-bound <c>{{child:host}}</c> refs
///     can be resolved.</item>
///   <item>Resolve late-bound refs in the parent env and create the parent service.</item>
/// </list>
/// The orchestrator is stateless — every call is self-contained and reports progress via the
/// <see cref="StackDeploymentProgress"/> callback so the UI can surface per-service status.
/// </summary>
public class IvyDeployOrchestrator
{
    private const int HostPollMaxAttempts = 20;
    private static readonly TimeSpan HostPollDelay = TimeSpan.FromSeconds(1);

    private readonly SliplaneApiClient _client;
    private readonly GitHubDockerfilePathResolver _dockerfileResolver;

    public IvyDeployOrchestrator(SliplaneApiClient client, GitHubDockerfilePathResolver dockerfileResolver)
    {
        _client = client;
        _dockerfileResolver = dockerfileResolver;
    }

    public async Task<StackDeploymentResult> DeployAsync(
        string apiToken,
        string projectId,
        string serverId,
        string parentServiceName,
        IvyDeployManifest manifest,
        StackDeploymentProgress? progress = null,
        CancellationToken cancellationToken = default)
    {
        var childResolutions = new Dictionary<string, IvyDeployTemplateEngine.ChildResolution>(StringComparer.Ordinal);
        var createdChildren = new List<SliplaneService>();

        foreach (var child in manifest.ChildServices)
        {
            var (created, resolution) = await CreateChildAsync(
                apiToken, projectId, serverId, parentServiceName, child, cancellationToken, progress);
            createdChildren.Add(created);
            childResolutions[created.Name] = resolution;
        }

        progress?.Invoke(parentServiceName, StackStepState.Starting, "Creating parent service");
        var parent = await CreateParentAsync(
            apiToken, projectId, serverId, parentServiceName, manifest, childResolutions, cancellationToken);
        progress?.Invoke(parentServiceName, StackStepState.Succeeded, null);

        return new StackDeploymentResult(parent, createdChildren);
    }

    private async Task<(SliplaneService Service, IvyDeployTemplateEngine.ChildResolution Resolution)> CreateChildAsync(
        string apiToken,
        string projectId,
        string serverId,
        string parentServiceName,
        IvyDeployChildService child,
        CancellationToken cancellationToken,
        StackDeploymentProgress? progress)
    {
        var childName = IvyDeployTemplateEngine.SubstituteEarly(child.ServiceName, parentServiceName);
        progress?.Invoke(childName, StackStepState.Starting, child.Description);

        var env = BuildChildEnv(child, parentServiceName);

        var volumeMounts = child.Volumes
            .Select(v => new ServiceVolumeMount(
                MountPath: v.MountPath,
                VolumeName: IvyDeployTemplateEngine.SubstituteEarly(v.VolumeName, parentServiceName)))
            .ToList();

        var isImage = !string.IsNullOrWhiteSpace(child.ImageUrl);
        object deployment = isImage
            ? new ImageDeployment(Url: child.ImageUrl!.Trim())
            : new RepositoryDeployment(
                Url: NormalizeGitHubRepoUrl(child.GithubRepo!.Trim()),
                Branch: string.IsNullOrWhiteSpace(child.Branch) ? "main" : child.Branch!.Trim(),
                AutoDeploy: true,
                DockerfilePath: string.IsNullOrWhiteSpace(child.DockerfilePath) ? "Dockerfile" : child.DockerfilePath!.Trim(),
                DockerContext: ".");

        // Sliplane docs: databases and many registry images are not HTTP — use "tcp" (or "udp"). Using "http" here
        // has been seen to fail create with misleading errors (e.g. "Invalid external docker image") for postgres.
        var networkProtocol = isImage ? "tcp" : "http";

        var request = new CreateServiceRequest(
            Name: childName,
            ServerId: serverId,
            Network: new ServiceNetworkRequest(Public: false, Protocol: networkProtocol),
            Deployment: deployment,
            Env: env.Count > 0 ? env.Select(kv => new EnvironmentVariable(kv.Key, kv.Value, kv.Secret)).ToList() : null,
            Volumes: volumeMounts.Count > 0 ? volumeMounts : null);

        var created = await _client.CreateServiceAsync(apiToken, projectId, request)
            ?? throw new InvalidOperationException($"Sliplane returned no body when creating child '{childName}'.");

        var host = await ResolveHostAsync(apiToken, projectId, created, cancellationToken);
        var resolution = new IvyDeployTemplateEngine.ChildResolution(
            Host: host,
            Env: env.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal));

        progress?.Invoke(childName, StackStepState.Succeeded, $"Internal host: {host}");
        return (created, resolution);
    }

    private async Task<SliplaneService> CreateParentAsync(
        string apiToken,
        string projectId,
        string serverId,
        string parentServiceName,
        IvyDeployManifest manifest,
        IReadOnlyDictionary<string, IvyDeployTemplateEngine.ChildResolution> children,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifest.GithubRepo))
            throw new InvalidOperationException("ivy-deploy.yaml: parent 'githubRepo' is required.");

        var repoUrl = NormalizeGitHubRepoUrl(manifest.GithubRepo!);
        var branch = string.IsNullOrWhiteSpace(manifest.Branch) ? "main" : manifest.Branch!;
        var dockerfilePath = string.IsNullOrWhiteSpace(manifest.DockerfilePath) ? "Dockerfile" : manifest.DockerfilePath!;

        var resolution = await _dockerfileResolver.ResolveAsync(
            repoUrl, branch, dockerfilePath, ".", cancellationToken);
        var baseEnv = resolution.AdditionalEnv?.ToList() ?? [];

        var parentEnv = BuildParentEnv(manifest, parentServiceName, children);
        var env = baseEnv
            .Concat(parentEnv.Select(e => new EnvironmentVariable(e.Key, e.Value, e.Secret)))
            .ToList();

        var volumeMounts = manifest.Volumes
            .Select(v => new ServiceVolumeMount(
                MountPath: v.MountPath,
                VolumeName: IvyDeployTemplateEngine.SubstituteEarly(v.VolumeName, parentServiceName)))
            .ToList();

        var autoDeploy = manifest.DeployRules?.AutoDeploy ?? true;
        var ignorePaths = manifest.DeployRules?.IgnorePaths;

        var request = new CreateServiceRequest(
            Name: parentServiceName,
            ServerId: serverId,
            Network: new ServiceNetworkRequest(Public: true, Protocol: "http"),
            Deployment: new RepositoryDeployment(
                Url: repoUrl,
                Branch: branch,
                AutoDeploy: autoDeploy,
                DockerfilePath: resolution.DockerfilePath,
                DockerContext: resolution.DockerContext,
                IgnorePaths: ignorePaths is { Count: > 0 } ? ignorePaths.ToList() : null),
            Healthcheck: string.IsNullOrWhiteSpace(manifest.HealthCheck?.Path) ? null : manifest.HealthCheck!.Path,
            Env: env.Count > 0 ? env : null,
            Volumes: volumeMounts.Count > 0 ? volumeMounts : null);

        return await _client.CreateServiceAsync(apiToken, projectId, request)
            ?? throw new InvalidOperationException($"Sliplane returned no body when creating parent '{parentServiceName}'.");
    }

    private static List<(string Key, string Value, bool Secret)> BuildChildEnv(
        IvyDeployChildService child, string parentServiceName)
    {
        var result = new List<(string Key, string Value, bool Secret)>(child.Env.Count);
        foreach (var e in child.Env)
        {
            var value = ChooseRawValue(e) ?? throw new InvalidOperationException(
                $"Env '{e.Name}' on child '{child.ServiceName}' has no value, default, or generate spec.");
            var resolved = IvyDeployTemplateEngine.SubstituteEarly(value, parentServiceName);
            result.Add((e.Name, resolved, e.Secret));
        }
        return result;
    }

    private static List<(string Key, string Value, bool Secret)> BuildParentEnv(
        IvyDeployManifest manifest,
        string parentServiceName,
        IReadOnlyDictionary<string, IvyDeployTemplateEngine.ChildResolution> children)
    {
        var result = new List<(string Key, string Value, bool Secret)>(manifest.Env.Count);
        foreach (var e in manifest.Env)
        {
            var value = ChooseRawValue(e) ?? throw new InvalidOperationException(
                $"Env '{e.Name}' on parent service has no value, default, or generate spec.");
            var resolved = IvyDeployTemplateEngine.Resolve(value, parentServiceName, children);
            result.Add((e.Name, resolved, e.Secret));
        }
        return result;
    }

    private static string? ChooseRawValue(IvyDeployEnvVar e)
    {
        if (!string.IsNullOrEmpty(e.Value)) return e.Value;
        if (!string.IsNullOrEmpty(e.Default)) return e.Default;
        if (!string.IsNullOrEmpty(e.Generate)) return IvyDeployTemplateEngine.Generate(e.Generate);
        return null;
    }

    private async Task<string> ResolveHostAsync(
        string apiToken, string projectId, SliplaneService created, CancellationToken cancellationToken)
    {
        var immediate = created.Network?.InternalDomain;
        if (!string.IsNullOrWhiteSpace(immediate)) return immediate!;

        for (var attempt = 0; attempt < HostPollMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(HostPollDelay, cancellationToken).ConfigureAwait(false);

            var latest = await _client.GetServiceAsync(apiToken, projectId, created.Id);
            var host = latest?.Network?.InternalDomain;
            if (!string.IsNullOrWhiteSpace(host)) return host!;
        }

        throw new InvalidOperationException(
            $"Timed out waiting for internalDomain of service '{created.Name}' ({created.Id}).");
    }

    /// <summary>
    /// Sliplane API expects a repository URL with scheme (see <c>format: uri</c> in OpenAPI). UI often accepts
    /// <c>owner/repo</c>; we normalize to <c>https://github.com/owner/repo</c>.
    /// </summary>
    private static string NormalizeGitHubRepoUrl(string raw)
    {
        var t = raw.Trim();
        if (t.Length == 0) return t;
        if (t.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return t;
        if (t.StartsWith("github.com/", StringComparison.OrdinalIgnoreCase))
            return "https://" + t;
        return "https://github.com/" + t.TrimStart('/');
    }
}

public enum StackStepState { Starting, Succeeded, Failed }

public delegate void StackDeploymentProgress(string serviceName, StackStepState state, string? detail);

public record StackDeploymentResult(SliplaneService Parent, IReadOnlyList<SliplaneService> Children);
