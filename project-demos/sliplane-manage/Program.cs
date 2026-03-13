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

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

// Sliplane OAuth auth provider
server.UseAuth<SliplaneAuthProvider>();

var chromeSettings = new ChromeSettings()
    .DefaultApp<SliplaneServicesApp>()        // show Servers app as wallpaper when no tabs are open
    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

await server.RunAsync();