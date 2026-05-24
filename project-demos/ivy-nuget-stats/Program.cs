using IvyInsights.Apps;
using IvyInsights.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
server.Services.AddOpenApi(InsightsOpenApi.Configure);

server.Services.AddHttpClient<NuGetApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("IvyInsights", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
    client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.All
});

server.Services.AddSingleton<INuGetStatisticsProvider, NuGetStatisticsProvider>();
server.Services.AddSingleton<IDatabaseService, DatabaseService>();

server.Services.AddHttpClient<IDatabaseUpdateService, DatabaseUpdateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("IvyInsights/1.0");
});

#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.ReservePaths("/starred", "/unstarred", "/stars", "/downloads", "/summary", "/openapi", "/scalar");

server.UseWebApplication(app =>
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Ivy Insights API");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });

    app.MapGet("/starred", GetStarred)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetStarred")
        .WithSummary("Stargazers (active)")
        .WithDescription(InsightsOpenApi.PackageDoc(includeRepo: true));

    app.MapGet("/unstarred", GetUnstarred)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetUnstarred")
        .WithSummary("Stargazers (left)")
        .WithDescription(InsightsOpenApi.PackageDoc(includeRepo: true));

    app.MapGet("/stars", GetStars)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetStars")
        .WithSummary("Star counts")
        .WithDescription(InsightsOpenApi.PackageDoc(includeRepo: true));

    app.MapGet("/downloads", GetDownloads)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetDownloads")
        .WithSummary("Latest NuGet downloads")
        .WithDescription(InsightsOpenApi.PackageDoc());

    app.MapGet("/downloads/history", GetDownloadsHistory)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetDownloadsHistory")
        .WithSummary("NuGet download history")
        .WithDescription(InsightsOpenApi.PackageDoc(includeDays: true));

    app.MapGet("/summary", GetSummary)
        .WithTags(InsightsOpenApi.TagName)
        .WithName("GetSummary")
        .WithSummary("Downloads + GitHub stars")
        .WithDescription(InsightsOpenApi.PackageDoc(includeRepo: true));
});

await server.RunAsync();

static IResult BadTarget(string error) => Results.BadRequest(new { error });

static bool TryResolveTarget(string? package, string? repo, out MetricsTarget target, out IResult? badRequest)
{
    if (InsightsMetricsCatalog.TryResolve(package, repo, out target, out var error))
    {
        badRequest = null;
        return true;
    }

    badRequest = BadTarget(error!);
    return false;
}

static async Task<IResult> GetStarred(IDatabaseService db, string? package, string? repo, CancellationToken ct)
{
    if (!TryResolveTarget(package, repo, out var target, out var bad)) return bad!;
    var all = await db.GetGithubStargazersAsync(target.RepoName, ct);
    var starred = all.Where(s => s.IsActive).Select(s => new { s.Username, s.StarredAt });
    return Results.Ok(new { target.PackageId, repo = target.RepoName, displayName = target.DisplayName, stargazers = starred });
}

static async Task<IResult> GetUnstarred(IDatabaseService db, string? package, string? repo, CancellationToken ct)
{
    if (!TryResolveTarget(package, repo, out var target, out var bad)) return bad!;
    var all = await db.GetGithubStargazersAsync(target.RepoName, ct);
    var unstarred = all.Where(s => !s.IsActive).Select(s => new { s.Username, s.StarredAt, s.UnstarredAt });
    return Results.Ok(new { target.PackageId, repo = target.RepoName, displayName = target.DisplayName, stargazers = unstarred });
}

static async Task<IResult> GetStars(IDatabaseService db, string? package, string? repo, CancellationToken ct)
{
    if (!TryResolveTarget(package, repo, out var target, out var bad)) return bad!;
    var all = await db.GetGithubStargazersAsync(target.RepoName, ct);
    var starred = all.Count(s => s.IsActive);
    var unstarred = all.Count(s => !s.IsActive);
    return Results.Ok(new
    {
        target.PackageId,
        repo = target.RepoName,
        displayName = target.DisplayName,
        starred,
        unstarred,
        totalEver = all.Count
    });
}

static async Task<IResult> GetDownloads(IDatabaseService db, string? package, CancellationToken ct)
{
    if (!InsightsMetricsCatalog.TryResolvePackage(package, out var packageId, out var error))
        return BadTarget(error!);

    var daily = await db.GetDailyDownloadStatsAsync(days: 2, packageName: packageId, cancellationToken: ct);
    var latest = daily.FirstOrDefault();
    var displayName = packageId == MetricsTarget.Tendril.PackageId
        ? MetricsTarget.Tendril.DisplayName
        : MetricsTarget.Ivy.DisplayName;

    return Results.Ok(new
    {
        package = packageId,
        displayName,
        totalDownloads = latest?.TotalDownloads ?? 0L,
        dailyGrowth = latest?.DailyGrowth ?? 0L,
        asOfDate = latest?.Date.ToString("O") ?? (string?)null
    });
}

static async Task<IResult> GetDownloadsHistory(IDatabaseService db, string? package, int days = 30, CancellationToken ct = default)
{
    if (!InsightsMetricsCatalog.TryResolvePackage(package, out var packageId, out var error))
        return BadTarget(error!);

    var displayName = packageId == MetricsTarget.Tendril.PackageId
        ? MetricsTarget.Tendril.DisplayName
        : MetricsTarget.Ivy.DisplayName;

    var daily = await db.GetDailyDownloadStatsAsync(
        days: Math.Clamp(days, 1, 365),
        packageName: packageId,
        cancellationToken: ct);

    return Results.Ok(new
    {
        package = packageId,
        displayName,
        days = Math.Clamp(days, 1, 365),
        history = daily.Select(s => new { s.Date, s.TotalDownloads, s.DailyGrowth })
    });
}

static async Task<IResult> GetSummary(IDatabaseService db, string? package, string? repo, CancellationToken ct)
{
    if (!TryResolveTarget(package, repo, out var target, out var bad)) return bad!;

    var stargazers = await db.GetGithubStargazersAsync(target.RepoName, ct);
    var daily = await db.GetDailyDownloadStatsAsync(days: 2, packageName: target.PackageId, cancellationToken: ct);
    var latest = daily.FirstOrDefault();

    return Results.Ok(new
    {
        package = target.PackageId,
        repo = target.RepoName,
        displayName = target.DisplayName,
        stars = stargazers.Count(s => s.IsActive),
        starredCount = stargazers.Count(s => s.IsActive),
        unstarredCount = stargazers.Count(s => !s.IsActive),
        totalStargazersEver = stargazers.Count,
        totalDownloads = latest?.TotalDownloads ?? 0L,
        downloadsDailyGrowth = latest?.DailyGrowth ?? 0L,
        downloadsAsOfDate = latest?.Date.ToString("O") ?? (string?)null
    });
}
