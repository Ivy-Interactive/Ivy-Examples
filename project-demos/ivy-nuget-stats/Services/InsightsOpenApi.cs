using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IvyInsights.Services;

public static class InsightsOpenApi
{
    public static string PackageDoc(bool includeRepo = false, bool includeDays = false) =>
        includeDays
            ? "Query **package**: `ivy` | `tendril`. **days**: 1–365."
            : includeRepo
                ? "Query **package**: `ivy` | `tendril`. Optional **repo**: `framework` | `tendril`."
                : "Query **package**: `ivy` (default) | `tendril`.";

    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/starred", "/unstarred", "/stars", "/downloads", "/downloads/history", "/summary"
    };

    public const string TagName = "Metrics";

    public static void Configure(OpenApiOptions options) => options.AddDocumentTransformer(TransformAsync);

    private static Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext _,
        CancellationToken __)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Ivy Insights API",
            Version = "v1",
            Description =
                "Use query **package**: `ivy` (default) or `tendril`. " +
                "Example: `/summary?package=tendril`"
        };

        document.Servers =
        [
            new OpenApiServer { Url = "https://ivy-nuget-stats.sliplane.app", Description = "Production" }
        ];

        foreach (var path in document.Paths.Keys.Where(p => !AllowedPaths.Contains(p)).ToList())
            document.Paths.Remove(path);

        document.Tags = new HashSet<OpenApiTag>
        {
            new() { Name = TagName, Description = "NuGet + GitHub metrics per product." }
        };

        document.Components?.Schemas?.Clear();

        return Task.CompletedTask;
    }
}
