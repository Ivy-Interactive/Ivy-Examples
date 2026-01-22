using IvyInsights.Apps;
using IvyInsights.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Register HttpClient for NuGet API
server.Services.AddHttpClient("NuGet", client =>
{
    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("IvyInsights", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Register NuGet Services
server.Services.AddScoped<NuGetApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("NuGet");
    return new NuGetApiClient(httpClient);
});
server.Services.AddScoped<INuGetStatisticsProvider, NuGetStatisticsProvider>();

#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<NuGetStatsApp>()
    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

await server.RunAsync();