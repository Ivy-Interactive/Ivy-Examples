using Auth.GitHub.Test.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Register the HttpClient factory in your DI container:
server.Services.AddHttpClient("GitHubAuth", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-Framework");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
});

// Ensure IConfiguration is registered (it should be, but let's be explicit)
server.Services.AddSingleton(server.Configuration);

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings()
    .DefaultApp<TestAuthApp>()
    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

// Configure GitHub Auth Provider - UseAuth will create the provider via DI
server.UseAuth<GitHubAuthProvider>(c => c.UseGitHub());

await server.RunAsync();