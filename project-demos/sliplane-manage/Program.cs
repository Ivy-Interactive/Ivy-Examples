using Ivy.Auth.Sliplane;
using SliplaneManage.Services;
using SliplaneManage.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Register the HttpClient factory for Sliplane API calls
server.Services.AddHttpClient("Sliplane", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-SliplaneManager/1.0");
});

// Ensure IConfiguration is available to apps
server.Services.AddSingleton(server.Configuration);

// Register Sliplane API client
server.Services.AddScoped<SliplaneApiClient>();

Server.ConfigureAuthCookieOptions = options =>
{
    options.Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(30));
};

// Captures ?repo= from the initial HTTP GET before Ivy SPA strips query params.
server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, SliplaneManage.Services.RepoCaptureFilter>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

// Sliplane OAuth auth provider
server.UseAuth<SliplaneAuthProvider>();

var chromeSettings = new ChromeSettings()
    .UseTabs(preventDuplicates: true)
    .DefaultApp<SliplaneDeployApp>();
server.UseChrome(chromeSettings);

await server.RunAsync();