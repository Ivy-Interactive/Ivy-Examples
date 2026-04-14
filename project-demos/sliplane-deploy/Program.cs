using Ivy.Auth.Sliplane;
using SliplaneDeploy.Services;
using SliplaneDeploy.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

server.Services.AddHttpClient("Ivy", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-SliplaneDeploy/1.0");
});

server.Services.AddHttpClient("GitHubRaw", client =>
{
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Ivy-SliplaneDeploy/1.0");
});

server.Services.AddSingleton<GitHubDockerfilePathResolver>();

server.Services.AddSingleton(server.Configuration);
server.Services.AddHttpContextAccessor();
server.Services.AddScoped<DeploymentDraftStore>();
server.Services.AddScoped<SliplaneApiClient>();

Server.ConfigureAuthCookieOptions = options =>
{
    options.Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(30));
};

server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, RepoCaptureFilter>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

server.UseAuth<SliplaneAuthProvider>();

// DefaultApp is required so that after OAuth redirect (which lands on /) Ivy knows
// which app to open — without this the shell can't resolve the route and shows "app not found".
var appShellSettings = new AppShellSettings()
    .DefaultApp<SliplaneDeployApp>()
    .UseTabs(preventDuplicates: true);
server.UseAppShell(appShellSettings);

await server.RunAsync();
