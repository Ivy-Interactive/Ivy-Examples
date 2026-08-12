using System.Globalization;
using TendrilPrStaging.Apps;
using TendrilPrStaging.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

server.Services.AddHttpClient("GitHub", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "TendrilPrStaging/1.0");
});

server.Services.AddHttpClient("Sliplane", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "TendrilPrStaging/1.0");
});

server.Services.AddSingleton(server.Configuration);
server.Services.AddSingleton<TendrilReposProvider>();
server.Services.AddScoped<GitHubApiClient>();
server.Services.AddScoped<SliplaneStagingClient>();
server.Services.AddScoped<TendrilStagingDeployService>();
server.Services.AddScoped<TendrilPrCommentService>();
server.Services.AddScoped<GitHubWebhookHandler>();
server.Services.AddSingleton<TendrilErrorWatcherQueue>();
server.Services.AddHostedService<TendrilErrorWatcherBackgroundService>();
server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, WebhookEndpointFilter>();
server.Services.AddHostedService<ExpiryCleanupBackgroundService>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();

var appShellSettings = AppShellSettings.Default()
    .DefaultApp<TendrilPrStagingApp>()
    .UseTabs(preventDuplicates: true)
    .UseFooterMenuItemsTransformer((items, navigator) =>
    {
        var list = items.ToList();
        list.Add(MenuItem.Default("Deploy All Open PRs").Icon(Icons.Rocket).OnSelect(() =>
        {
            TendrilPrStagingFooterBridge.Request("deploy-all");
            navigator.Navigate(typeof(TendrilPrStagingApp));
        }));
        list.Add(MenuItem.Default("Delete All Staging").Icon(Icons.Trash2).OnSelect(() =>
        {
            TendrilPrStagingFooterBridge.Request("delete-all");
            navigator.Navigate(typeof(TendrilPrStagingApp));
        }));
        return list;
    });
server.UseAppShell(appShellSettings);

await server.RunAsync();
