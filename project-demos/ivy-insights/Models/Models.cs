namespace IvyInsights.Models;

// NuGet API v3 response models
public sealed class NuGetPackageIndex
{
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
}

public sealed class NuGetPackageInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long? TotalDownloads { get; set; }
}

public sealed class NuGetSearchResponse
{
    [JsonPropertyName("data")]
    public List<NuGetSearchResult> Data { get; set; } = new();
}

public sealed class NuGetSearchResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("authors")]
    public List<string>? Authors { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long? TotalDownloads { get; set; }
}

// NuGet Registration API models
public sealed class NuGetRegistrationIndex
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<NuGetRegistrationPage> Items { get; set; } = new();
}

public sealed class NuGetRegistrationPage
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<NuGetRegistrationItem> Items { get; set; } = new();

    [JsonPropertyName("lower")]
    public string? Lower { get; set; }

    [JsonPropertyName("upper")]
    public string? Upper { get; set; }
}

public sealed class NuGetRegistrationItem
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("catalogEntry")]
    public NuGetCatalogEntry? CatalogEntry { get; set; }
}

public sealed class NuGetCatalogEntry
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long? TotalDownloads { get; set; }
}

// Aggregated statistics model
public sealed class PackageStatistics
{
    public string PackageId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Authors { get; set; }
    public string? ProjectUrl { get; set; }
    public int TotalVersions { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public DateTime? LatestVersionPublished { get; set; }
    public DateTime? FirstVersionPublished { get; set; }
    public long? TotalDownloads { get; set; }
    public List<VersionInfo> Versions { get; set; } = new();
}

public sealed class VersionInfo
{
    public string Version { get; set; } = string.Empty;
    public DateTime? Published { get; set; }
    public long? Downloads { get; set; }
}

// Daily snapshot for historical tracking
public sealed class DailyPackageSnapshot
{
    public DateOnly Date { get; set; }
    public string PackageId { get; set; } = string.Empty;
    public long TotalDownloads { get; set; }
    public Dictionary<string, long> DownloadsPerVersion { get; set; } = new();
    public int TotalVersions { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
}

