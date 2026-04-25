namespace SliplaneDeploy.Services;

using System.Net;
using SliplaneDeploy.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Downloads and parses <c>ivy-deploy.yaml</c> from the root of a GitHub branch. Uses the same
/// <c>raw.githubusercontent.com</c> access pattern as <see cref="GitHubDockerfilePathResolver"/>.
/// </summary>
public class IvyDeployManifestService
{
    private const string ManifestFileName = "ivy-deploy.yaml";
    private const string ExpectedApiVersion = "ivy-deploy/v1";

    private readonly IHttpClientFactory _httpClientFactory;

    public IvyDeployManifestService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Attempts to fetch <c>ivy-deploy.yaml</c> for the given GitHub repo/branch. Returns
    /// <c>null</c> when the file is absent (HTTP 404). Throws on malformed YAML or unexpected
    /// API versions so the UI surfaces a clear error instead of silently falling back.
    /// </summary>
    public async Task<IvyDeployManifest?> TryFetchAsync(
        string gitRepoUrl,
        string branch,
        CancellationToken cancellationToken = default)
    {
        if (!GitHubDockerfilePathResolver.TryParseGitHubRepo(gitRepoUrl, out var owner, out var repo))
            return null;

        var branchTrim = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();
        var url = BuildRawUrl(owner, repo, branchTrim, ManifestFileName);

        using var client = _httpClientFactory.CreateClient("GitHubRaw");
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Failed to fetch {ManifestFileName} from {url}: {(int)response.StatusCode} {response.StatusCode}.");

        var yaml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return Parse(yaml);
    }

    /// <summary>Parses a YAML manifest string. Exposed for tests and error-reporting.</summary>
    public static IvyDeployManifest Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new InvalidOperationException("ivy-deploy.yaml is empty.");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        RawManifest raw;
        try
        {
            raw = deserializer.Deserialize<RawManifest>(yaml)
                  ?? throw new InvalidOperationException("ivy-deploy.yaml deserialized to null.");
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"ivy-deploy.yaml is not valid YAML: {ex.Message}", ex);
        }

        if (!string.Equals(raw.ApiVersion?.Trim(), ExpectedApiVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Unsupported apiVersion '{raw.ApiVersion}'. Expected '{ExpectedApiVersion}'.");

        if (string.IsNullOrWhiteSpace(raw.ServiceName))
            throw new InvalidOperationException("serviceName is required in ivy-deploy.yaml.");

        return new IvyDeployManifest(
            ApiVersion: raw.ApiVersion!,
            ServiceName: raw.ServiceName!.Trim(),
            Title: raw.Title,
            GithubRepo: raw.GithubRepo,
            Branch: raw.Branch,
            DockerfilePath: raw.DockerfilePath,
            Port: raw.Port,
            HealthCheck: raw.HealthCheck is null ? null : new IvyDeployHealthCheck(
                Path: raw.HealthCheck.Path,
                IntervalSeconds: raw.HealthCheck.IntervalSeconds,
                TimeoutSeconds: raw.HealthCheck.TimeoutSeconds,
                StartPeriodSeconds: raw.HealthCheck.StartPeriodSeconds),
            Env: (raw.Env ?? []).Select(ToEnvVar).ToList(),
            Volumes: (raw.Volumes ?? []).Select(ToVolume).ToList(),
            ChildServices: (raw.ChildServices ?? []).Select(ToChild).ToList(),
            DeployRules: raw.DeployRules is null ? null : new IvyDeployRules(
                AutoDeploy: raw.DeployRules.AutoDeploy ?? true,
                IgnorePaths: raw.DeployRules.IgnorePaths));
    }

    private static IvyDeployEnvVar ToEnvVar(RawEnv e)
    {
        if (string.IsNullOrWhiteSpace(e.Name))
            throw new InvalidOperationException("env entry is missing 'name'.");
        return new IvyDeployEnvVar(
            Name: e.Name!.Trim(),
            Value: e.Value,
            Default: e.Default,
            Generate: e.Generate,
            Secret: e.Secret ?? false);
    }

    private static IvyDeployVolumeSpec ToVolume(RawVolume v)
    {
        if (string.IsNullOrWhiteSpace(v.VolumeName))
            throw new InvalidOperationException("volume entry is missing 'volumeName'.");
        if (string.IsNullOrWhiteSpace(v.MountPath))
            throw new InvalidOperationException($"volume '{v.VolumeName}' is missing 'mountPath'.");
        return new IvyDeployVolumeSpec(
            VolumeName: v.VolumeName!.Trim(),
            MountPath: v.MountPath!.Trim(),
            Size: v.Size);
    }

    private static IvyDeployChildService ToChild(RawChild c)
    {
        if (string.IsNullOrWhiteSpace(c.ServiceName))
            throw new InvalidOperationException("childServices entry is missing 'serviceName'.");
        if (string.IsNullOrWhiteSpace(c.ImageUrl) && string.IsNullOrWhiteSpace(c.GithubRepo))
            throw new InvalidOperationException(
                $"child '{c.ServiceName}' must set either 'imageUrl' or 'githubRepo'.");
        return new IvyDeployChildService(
            ServiceName: c.ServiceName!.Trim(),
            ImageUrl: c.ImageUrl,
            GithubRepo: c.GithubRepo,
            Branch: c.Branch,
            DockerfilePath: c.DockerfilePath,
            Description: c.Description,
            Env: (c.Env ?? []).Select(ToEnvVar).ToList(),
            Volumes: (c.Volumes ?? []).Select(ToVolume).ToList());
    }

    private static string BuildRawUrl(string owner, string repo, string branch, string path)
    {
        var encoded = string.Join("/", path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        return $"https://raw.githubusercontent.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/{Uri.EscapeDataString(branch)}/{encoded}";
    }

    // ── Raw YAML shapes (YamlDotNet fills via reflection). Kept private + mutable. ──────────

    private class RawManifest
    {
        public string? ApiVersion { get; set; }
        public string? ServiceName { get; set; }
        public string? Title { get; set; }
        public string? GithubRepo { get; set; }
        public string? Branch { get; set; }
        public string? DockerfilePath { get; set; }
        public int? Port { get; set; }
        public RawHealthCheck? HealthCheck { get; set; }
        public List<RawEnv>? Env { get; set; }
        public List<RawVolume>? Volumes { get; set; }
        public List<RawChild>? ChildServices { get; set; }
        public RawDeployRules? DeployRules { get; set; }
    }

    private class RawHealthCheck
    {
        public string? Path { get; set; }
        public int? IntervalSeconds { get; set; }
        public int? TimeoutSeconds { get; set; }
        public int? StartPeriodSeconds { get; set; }
    }

    private class RawEnv
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Default { get; set; }
        public string? Generate { get; set; }
        public bool? Secret { get; set; }
    }

    private class RawVolume
    {
        public string? VolumeName { get; set; }
        public string? MountPath { get; set; }
        public string? Size { get; set; }
    }

    private class RawChild
    {
        public string? ServiceName { get; set; }
        public string? ImageUrl { get; set; }
        public string? GithubRepo { get; set; }
        public string? Branch { get; set; }
        public string? DockerfilePath { get; set; }
        public string? Description { get; set; }
        public List<RawEnv>? Env { get; set; }
        public List<RawVolume>? Volumes { get; set; }
    }

    private class RawDeployRules
    {
        public bool? AutoDeploy { get; set; }
        public List<string>? IgnorePaths { get; set; }
    }
}
