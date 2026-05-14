using Ivy.Auth.Sliplane;
using TendrilUbuntuDeploy.Apps;
using TendrilUbuntuDeploy.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

server.Services.AddHttpClient("Ivy", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-UbuntuDeploy/1.0");
});

server.Services.AddScoped<SliplaneApiClient>();
server.Services.AddSingleton(server.Configuration);
server.Services.AddHttpContextAccessor();

Server.ConfigureAuthCookieOptions = options =>
{
    options.Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(60));
};

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseAuth<SliplaneAuthProvider>();

var appShellSettings = new AppShellSettings()
    .DefaultApp<UbuntuDeployApp>()
    .UseTabs(preventDuplicates: true);

server.UseAppShell(appShellSettings);

await server.RunAsync();
