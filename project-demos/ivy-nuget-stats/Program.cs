using IvyInsights.Apps;
using IvyInsights.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
var ivyInsightsPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "/starred", "/unstarred", "/stars", "/downloads", "/downloads/history", "/summary"
};
var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IvyInsights" };
server.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        foreach (var path in document.Paths.Keys.Where(p => !ivyInsightsPaths.Contains(p)).ToList())
            document.Paths.Remove(path);
        if (document.Tags != null)
        {
            foreach (var tag in document.Tags.Where(t => t.Name != null && !allowedTags.Contains(t.Name)).ToList())
                document.Tags.Remove(tag);
        }
        return Task.CompletedTask;
    });
});

server.Services.AddHttpClient<NuGetApiClient>(client =>
{
    // Timeout: NuGet API can be slow, especially registration pages (10-30 seconds recommended)
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
    app.MapScalarApiReference();

    app.MapGet("/starred", async (IDatabaseService db, CancellationToken ct) =>
    {
        var all = await db.GetGithubStargazersAsync(ct);
        var starred = all.Where(s => s.IsActive).Select(s => new { s.Username, s.StarredAt });
        return Results.Ok(starred);
    });

    app.MapGet("/unstarred", async (IDatabaseService db, CancellationToken ct) =>
    {
        var all = await db.GetGithubStargazersAsync(ct);
        var unstarred = all.Where(s => !s.IsActive).Select(s => new { s.Username, s.StarredAt, s.UnstarredAt });
        return Results.Ok(unstarred);
    });

    app.MapGet("/stars", async (IDatabaseService db, CancellationToken ct) =>
    {
        var all = await db.GetGithubStargazersAsync(ct);
        var starred = all.Count(s => s.IsActive);
        var unstarred = all.Count(s => !s.IsActive);
        return Results.Ok(new { starred, unstarred, totalEver = all.Count });
    });

    app.MapGet("/downloads", async (IDatabaseService db, CancellationToken ct) =>
    {
        var daily = await db.GetDailyDownloadStatsAsync(days: 2, ct);
        var latest = daily.FirstOrDefault();
        return Results.Ok(new
        {
            totalDownloads = latest?.TotalDownloads ?? 0L,
            dailyGrowth = latest?.DailyGrowth ?? 0L,
            asOfDate = latest?.Date.ToString("O") ?? (string?)null
        });
    });

    app.MapGet("/downloads/history", async (IDatabaseService db, int days = 30, CancellationToken ct = default) =>
    {
        var daily = await db.GetDailyDownloadStatsAsync(days: Math.Clamp(days, 1, 365), ct);
        return Results.Ok(daily.Select(s => new { s.Date, s.TotalDownloads, s.DailyGrowth }));
    });

    app.MapGet("/summary", async (IDatabaseService db, CancellationToken ct) =>
    {
        var stargazers = await db.GetGithubStargazersAsync(ct);
        var daily = await db.GetDailyDownloadStatsAsync(days: 2, ct);
        var latest = daily.FirstOrDefault();
        return Results.Ok(new
        {
            stars = stargazers.Count(s => s.IsActive),
            starredCount = stargazers.Count(s => s.IsActive),
            unstarredCount = stargazers.Count(s => !s.IsActive),
            totalStargazersEver = stargazers.Count,
            totalDownloads = latest?.TotalDownloads ?? 0L,
            downloadsDailyGrowth = latest?.DailyGrowth ?? 0L,
            downloadsAsOfDate = latest?.Date.ToString("O") ?? (string?)null
        });
    });
});

await server.RunAsync();