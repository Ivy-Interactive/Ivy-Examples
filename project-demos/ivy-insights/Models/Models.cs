namespace IvyInsights.Models;

// NuGet Search API models
public sealed class NuGetSearchResponse
{
    [JsonPropertyName("data")]
    public List<NuGetSearchResult> Data { get; set; } = new();
}

public sealed class NuGetSearchResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("authors")]
    public List<string>? Authors { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long? TotalDownloads { get; set; }

    [JsonPropertyName("versions")]
    public List<NuGetSearchVersion>? Versions { get; set; }
}

public sealed class NuGetSearchVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }
}

// NuGet Registration API models
public sealed class NuGetRegistrationIndex
{
    [JsonPropertyName("items")]
    public List<NuGetRegistrationPage> Items { get; set; } = new();
}

public sealed class NuGetRegistrationPage
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<NuGetRegistrationItem> Items { get; set; } = new();
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
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }
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

public sealed class DailyDownloadStats
{
    public DateOnly Date { get; set; }
    public long TotalDownloads { get; set; }
    public long DailyGrowth { get; set; }
}