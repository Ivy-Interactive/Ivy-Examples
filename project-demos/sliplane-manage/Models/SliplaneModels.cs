namespace SliplaneManage.Models;

using System.Text.Json.Serialization;

// ─── Projects ────────────────────────────────────────────────────────────────

public record SliplaneProject(
    string Id,
    string Name
);

public record CreateProjectRequest(
    string Name
);

public record UpdateProjectRequest(
    string Name
);

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

/// <summary>
/// One item from GET /servers/{id}/metrics?range=1h (API returns array).
/// </summary>
public record SliplaneServerMetrics(
    [property: JsonPropertyName("cpuUsage")] double CpuUsagePercent,
    [property: JsonPropertyName("usedMemory")] double MemoryUsageMb,
    [property: JsonPropertyName("totalMemory")] double MemoryTotalMb,
    [property: JsonPropertyName("freeMemory")] double? FreeMemoryMb = null,
    [property: JsonPropertyName("createdAt")] DateTime? CreatedAt = null
);

public record SliplaneVolume(
    string Id,
    string Name,
    int SizeGb,
    string MountPath
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
    [property: JsonPropertyName("healthcheck")] string? Healthcheck = null
);

public record SliplaneServiceDomain(
    string Id,
    string Domain,
    bool IsCustom
);

public record SliplaneServiceResources(
    double CpuLimit,
    int MemoryLimit
);

public record SliplaneServiceNetwork(
    bool Public,
    string Protocol,
    string ManagedDomain,
    string InternalDomain,
    [property: JsonPropertyName("customDomains")] List<SliplaneServiceCustomDomain>? CustomDomains
);

public record SliplaneServiceCustomDomain(
    string Id,
    string Domain,
    string Status
);

public record SliplaneServiceDeployment(
    string Url,
    string DockerfilePath,
    string DockerContext,
    bool AutoDeploy,
    string Branch
);

public record SliplaneServiceMetrics(
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double MemoryUsageMb,
    double MemoryTotalMb
);

public record SliplaneServiceEvent(
    string Type,
    string Message,
    DateTime CreatedAt
);

public record SliplaneServiceLog(
    [property: JsonPropertyName("message")] string Line,
    [property: JsonPropertyName("createdAt")] DateTime Timestamp
);

/// <summary>
/// POST /projects/{projectId}/services — matches the Sliplane API spec.
/// </summary>
public record CreateServiceRequest(
    string Name,
    string ServerId,
    ServiceNetworkRequest Network,
    RepositoryDeployment Deployment,
    string? Cmd = null,
    string? Healthcheck = null,
    List<EnvironmentVariable>? Env = null,
    List<ServiceVolumeMount>? Volumes = null
);

/// <summary>Volume mount when creating a service.</summary>
public record ServiceVolumeMount(
    [property: JsonPropertyName("id")] string VolumeId,
    [property: JsonPropertyName("mountPath")] string MountPath
);

/// <summary>Network settings for a new service.</summary>
public record ServiceNetworkRequest(
    bool Public = true,
    string Protocol = "http"
);

/// <summary>Repository-based deployment configuration.</summary>
public record RepositoryDeployment(
    string Url,
    string Branch = "main",
    bool AutoDeploy = true,
    string DockerfilePath = "Dockerfile",
    string DockerContext = "."
);

/// <summary>A single environment variable.</summary>
public record EnvironmentVariable(
    string Key,
    string Value,
    bool Secret = false
);

/// <summary>PATCH /projects/{projectId}/services/{serviceId} — partial update body.</summary>
public record UpdateServiceRequest(
    string? Name = null,
    string? Cmd = null,
    string? Healthcheck = null,
    UpdateServiceDeployment? Deployment = null,
    List<EnvironmentVariable>? Env = null
);

/// <summary>Deployment section for PATCH service.</summary>
public record UpdateServiceDeployment(
    string Url,
    string Branch = "main",
    bool AutoDeploy = true,
    string DockerfilePath = "Dockerfile",
    string DockerContext = "."
);

public record AddDomainRequest(
    string Domain
);

// ─── Registry Credentials ────────────────────────────────────────────────────

public record SliplaneRegistryCredential(
    string Id,
    string Name,
    string Registry,
    string Username,
    DateTime CreatedAt
);

public record CreateRegistryCredentialRequest(
    string Name,
    string Registry,
    string Username,
    string Password
);

public record UpdateRegistryCredentialRequest(
    string? Name,
    string? Username,
    string? Password
);

// ─── Dashboard aggregation ────────────────────────────────────────────────────

public record SliplaneOverview(
    List<SliplaneProject> Projects,
    List<SliplaneServer> Servers,
    Dictionary<string, List<SliplaneService>> ServicesByProject,
    Dictionary<string, List<SliplaneServiceEvent>>? EventsByService = null
);
