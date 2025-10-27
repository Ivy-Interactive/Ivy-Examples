using GitHubDashboard.Apps;
using GitHubDashboard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Add services
server.Services.AddHttpClient<IGitHubApiService, GitHubApiService>();
server.Services.AddMemoryCache();
server.Services.AddLogging();

#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<GitHubDashboardApp>()
    .UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);
await server.RunAsync();
