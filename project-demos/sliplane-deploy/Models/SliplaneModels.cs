namespace SliplaneDeploy.Models;

using System.Text.Json.Serialization;

// ─── Projects ────────────────────────────────────────────────────────────────

public record SliplaneProject(string Id, string Name);

// ─── Servers ─────────────────────────────────────────────────────────────────

public record SliplaneServer(
    string Id,
    string Name,
    string Status,
    [property: JsonPropertyName("location")] string Region,
    [property: JsonPropertyName("instanceType")] string Plan,
    string? Ipv4,
    string? Ipv6,
    DateTime CreatedAt
);

// ─── Services ────────────────────────────────────────────────────────────────

public record SliplaneService(
    string Id,
    string Name,
    string Status,
    string? Image,
    string? GitRepo,
    string? GitBranch,
    int? Port,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<SliplaneServiceDomain>? Domains,
    SliplaneServiceResources? Resources,
    [property: JsonPropertyName("network")] SliplaneServiceNetwork? Network,
    [property: JsonPropertyName("deployment")] SliplaneServiceDeployment? Deployment,
    [property: JsonPropertyName("serverId")] string? ServerId,
    [property: JsonPropertyName("healthcheck")] string? Healthcheck = null,
    [property: JsonPropertyName("cmd")] string? Cmd = null,
    [property: JsonPropertyName("env")] List<EnvironmentVariable>? Env = null,
    [property: JsonPropertyName("volumes")] List<SliplaneServiceVolumeInfo>? Volumes = null,
    [property: JsonPropertyName("webhook")] string? Webhook = null
);

public record SliplaneServiceDomain(string Id, string Domain, bool IsCustom);

public record SliplaneServiceVolumeInfo(string Id, string Name, string MountPath);

public record SliplaneServiceResources(double CpuLimit, int MemoryLimit);

public record SliplaneServiceNetwork(
    bool Public,
    string Protocol,
    string ManagedDomain,
    string InternalDomain,
    [property: JsonPropertyName("customDomains")] List<SliplaneServiceCustomDomain>? CustomDomains
);

public record SliplaneServiceCustomDomain(string Id, string Domain, string Status);

public record SliplaneServiceDeployment(
    string Url,
    string DockerfilePath,
    string DockerContext,
    bool AutoDeploy,
    string Branch
);

public record SliplaneServiceEvent(string Type, string Message, DateTime CreatedAt);

/// <summary>
/// POST /projects/{projectId}/services — matches the Sliplane API spec.
/// <see cref="Deployment"/> is a <see cref="RepositoryDeployment"/> or <see cref="ImageDeployment"/>
/// (API oneOf). System.Text.Json emits the runtime type when declared as <c>object</c>.
/// </summary>
public record CreateServiceRequest(
    string Name,
    string ServerId,
    ServiceNetworkRequest Network,
    object Deployment,
    string? Cmd = null,
    string? Healthcheck = null,
    List<EnvironmentVariable>? Env = null,
    List<ServiceVolumeMount>? Volumes = null
);

/// <summary>
/// A volume mount. Either <see cref="VolumeId"/> (attach existing) or <see cref="VolumeName"/>
/// (create a new volume with this name on the server) — oneOf per API spec.
/// </summary>
public record ServiceVolumeMount(
    [property: JsonPropertyName("mountPath")] string MountPath,
    [property: JsonPropertyName("id")] string? VolumeId = null,
    [property: JsonPropertyName("name")] string? VolumeName = null
);

public record ServiceNetworkRequest(bool Public = true, string Protocol = "http");

public record RepositoryDeployment(
    string Url,
    string Branch = "main",
    bool AutoDeploy = true,
    string DockerfilePath = "Dockerfile",
    string DockerContext = ".",
    [property: JsonPropertyName("ignorePaths")] List<string>? IgnorePaths = null
);

/// <summary>Container image deployment (e.g., <c>docker.io/library/postgres:16</c>).</summary>
public record ImageDeployment(
    string Url,
    [property: JsonPropertyName("registryAuthenticationId")] string? RegistryAuthenticationId = null
);

public record EnvironmentVariable(string Key, string Value, bool Secret = false);
