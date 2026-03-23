using System.Globalization;
using PrStagingDeploy.Apps;
using PrStagingDeploy.Services;
using Ivy;

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
server.Services.AddScoped<PrStagingDeployCommentService>();
server.Services.AddScoped<GitHubWebhookHandler>();
server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, WebhookEndpointFilter>();
server.Services.AddHostedService<ExpiryCleanupBackgroundService>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var appShellSettings = AppShellSettings.Default()
    .DefaultApp<PrStagingDeployApp>()
    .UseTabs(preventDuplicates: true)
    .UseFooterMenuItemsTransformer((items, navigator) =>
    {
        // Convert to list for easier manipulation
        var list = items.ToList();

        list.Add(MenuItem.Default("Deploy All Open PRs").Icon(Icons.Rocket).OnSelect(() =>
        {
            PrStagingFooterBridge.Request("deploy-all");
            navigator.Navigate(typeof(PrStagingDeployApp));
        }));
        list.Add(MenuItem.Default("Delete All Branches").Icon(Icons.Trash2).OnSelect(() =>
        {
            PrStagingFooterBridge.Request("delete-all");
            navigator.Navigate(typeof(PrStagingDeployApp));
        }));

        return list;
    });
server.UseAppShell(appShellSettings);

await server.RunAsync();
