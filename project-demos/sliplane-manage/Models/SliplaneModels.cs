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
    SliplaneServiceResources? Resources
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
    string Line,
    DateTime Timestamp
);

public record CreateServiceRequest(
    string Name,
    string? Image,
    string? GitRepo,
    string? GitBranch,
    int? Port
);

public record UpdateServiceRequest(
    string? Name,
    string? Image,
    string? GitRepo,
    string? GitBranch,
    int? Port
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
    Dictionary<string, List<SliplaneService>> ServicesByProject
);
