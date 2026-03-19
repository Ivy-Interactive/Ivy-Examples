using System.Globalization;
using PrStagingDeploy.Apps;
using PrStagingDeploy.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

server.Services.AddHttpClient("GitHub", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "PrStagingDeploy/1.0");
});

server.Services.AddHttpClient("Sliplane", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "PrStagingDeploy/1.0");
});

server.Services.AddSingleton(server.Configuration);
server.Services.AddScoped<GitHubApiClient>();
server.Services.AddScoped<SliplaneStagingClient>();
server.Services.AddScoped<StagingDeployService>();
server.Services.AddScoped<GitHubWebhookHandler>();
server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, WebhookEndpointFilter>();
server.Services.AddHostedService<ExpiryCleanupBackgroundService>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<PrStagingDeployApp>()
    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

await server.RunAsync();
