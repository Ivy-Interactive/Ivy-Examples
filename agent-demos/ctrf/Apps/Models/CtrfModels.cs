using System.Text.Json.Serialization;

namespace CTRF.Apps.Models;

public record CtrfReport
{
    [JsonPropertyName("reportFormat")] public string ReportFormat { get; init; } = "";
    [JsonPropertyName("specVersion")] public string? SpecVersion { get; init; }
    [JsonPropertyName("reportId")] public string? ReportId { get; init; }
    [JsonPropertyName("timestamp")] public string? Timestamp { get; init; }
    [JsonPropertyName("results")] public CtrfResults Results { get; init; } = new();
    [JsonPropertyName("insights")] public Dictionary<string, InsightMetric>? Insights { get; init; }
    [JsonPropertyName("baseline")] public CtrfBaseline? Baseline { get; init; }
}

public record CtrfResults
{
    [JsonPropertyName("tool")] public CtrfTool Tool { get; init; } = new();
    [JsonPropertyName("summary")] public CtrfSummary Summary { get; init; } = new();
    [JsonPropertyName("tests")] public List<CtrfTest> Tests { get; init; } = [];
    [JsonPropertyName("environment")] public CtrfEnvironment? Environment { get; init; }
}

public record CtrfTool
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("version")] public string? Version { get; init; }
}

public record CtrfSummary
{
    [JsonPropertyName("tests")] public int Tests { get; init; }
    [JsonPropertyName("passed")] public int Passed { get; init; }
    [JsonPropertyName("failed")] public int Failed { get; init; }
    [JsonPropertyName("pending")] public int Pending { get; init; }
    [JsonPropertyName("skipped")] public int Skipped { get; init; }
    [JsonPropertyName("other")] public int Other { get; init; }
    [JsonPropertyName("flaky")] public int? Flaky { get; init; }
    [JsonPropertyName("suites")] public int? Suites { get; init; }
    [JsonPropertyName("start")] public long? Start { get; init; }
    [JsonPropertyName("stop")] public long? Stop { get; init; }
    [JsonPropertyName("duration")] public long? Duration { get; init; }

    public long ComputedDuration => Duration ?? (Start.HasValue && Stop.HasValue ? Stop.Value - Start.Value : 0);
}

public record CtrfTest
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("duration")] public long Duration { get; init; }
    [JsonPropertyName("start")] public long? Start { get; init; }
    [JsonPropertyName("stop")] public long? Stop { get; init; }
    [JsonPropertyName("suite")] public List<string>? Suite { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; init; }
    [JsonPropertyName("filePath")] public string? FilePath { get; init; }
    [JsonPropertyName("line")] public int? Line { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("trace")] public string? Trace { get; init; }
    [JsonPropertyName("stdout")] public List<string>? Stdout { get; init; }
    [JsonPropertyName("stderr")] public List<string>? Stderr { get; init; }
    [JsonPropertyName("flaky")] public bool? Flaky { get; init; }
    [JsonPropertyName("retries")] public int? Retries { get; init; }
    [JsonPropertyName("retryAttempts")] public List<RetryAttempt>? RetryAttempts { get; init; }
    [JsonPropertyName("attachments")] public List<CtrfAttachment>? Attachments { get; init; }
    [JsonPropertyName("steps")] public List<CtrfStep>? Steps { get; init; }
    [JsonPropertyName("insights")] public Dictionary<string, InsightMetric>? Insights { get; init; }
    [JsonPropertyName("browser")] public string? Browser { get; init; }

    public string SuitePath => Suite != null && Suite.Count > 0 ? string.Join(" > ", Suite) : "";
}

public record RetryAttempt
{
    [JsonPropertyName("attempt")] public int Attempt { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("duration")] public long Duration { get; init; }
    [JsonPropertyName("start")] public long? Start { get; init; }
    [JsonPropertyName("stop")] public long? Stop { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("trace")] public string? Trace { get; init; }
    [JsonPropertyName("attachments")] public List<CtrfAttachment>? Attachments { get; init; }
}

public record CtrfAttachment
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("contentType")] public string ContentType { get; init; } = "";
    [JsonPropertyName("path")] public string? Path { get; init; }
}

public record CtrfStep
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "";
}

public record CtrfEnvironment
{
    [JsonPropertyName("reportName")] public string? ReportName { get; init; }
    [JsonPropertyName("appName")] public string? AppName { get; init; }
    [JsonPropertyName("appVersion")] public string? AppVersion { get; init; }
    [JsonPropertyName("buildId")] public string? BuildId { get; init; }
    [JsonPropertyName("buildName")] public string? BuildName { get; init; }
    [JsonPropertyName("buildNumber")] public int? BuildNumber { get; init; }
    [JsonPropertyName("buildUrl")] public string? BuildUrl { get; init; }
    [JsonPropertyName("repositoryName")] public string? RepositoryName { get; init; }
    [JsonPropertyName("repositoryUrl")] public string? RepositoryUrl { get; init; }
    [JsonPropertyName("commit")] public string? Commit { get; init; }
    [JsonPropertyName("branchName")] public string? BranchName { get; init; }
    [JsonPropertyName("osPlatform")] public string? OsPlatform { get; init; }
    [JsonPropertyName("osRelease")] public string? OsRelease { get; init; }
    [JsonPropertyName("osVersion")] public string? OsVersion { get; init; }
    [JsonPropertyName("testEnvironment")] public string? TestEnvironment { get; init; }
    [JsonPropertyName("healthy")] public bool? Healthy { get; init; }
}

public record InsightMetric
{
    [JsonPropertyName("current")] public double Current { get; init; }
    [JsonPropertyName("baseline")] public double Baseline { get; init; }
    [JsonPropertyName("change")] public double Change { get; init; }
}

public record CtrfBaseline
{
    [JsonPropertyName("reportId")] public string? ReportId { get; init; }
    [JsonPropertyName("timestamp")] public string? Timestamp { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("buildNumber")] public int? BuildNumber { get; init; }
    [JsonPropertyName("buildName")] public string? BuildName { get; init; }
    [JsonPropertyName("buildUrl")] public string? BuildUrl { get; init; }
    [JsonPropertyName("commit")] public string? Commit { get; init; }
}

/// <summary>Wrapper used in the report list sidebar</summary>
public record UploadedReport
{
    public string FileName { get; init; } = "";
    public CtrfReport Report { get; init; } = new();
    public DateTime UploadedAt { get; init; } = DateTime.Now;
}
