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

/// <summary>POST /projects/{projectId}/services — matches the Sliplane API spec.</summary>
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

public record ServiceVolumeMount(
    [property: JsonPropertyName("id")] string VolumeId,
    [property: JsonPropertyName("mountPath")] string MountPath
);

public record ServiceNetworkRequest(bool Public = true, string Protocol = "http");

public record RepositoryDeployment(
    string Url,
    string Branch = "main",
    bool AutoDeploy = true,
    string DockerfilePath = "Dockerfile",
    string DockerContext = "."
);

public record EnvironmentVariable(string Key, string Value, bool Secret = false);
