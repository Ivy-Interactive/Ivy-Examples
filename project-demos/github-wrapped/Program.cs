using GitHubWrapped.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Register the HttpClient factory for GitHub API calls
server.Services.AddHttpClient("GitHubAuth", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-GitHubWrapped");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
});

// Ensure IConfiguration is registered
server.Services.AddSingleton(server.Configuration);

// Register GitHub Services
server.Services.AddScoped<GitHubApiClient>();
server.Services.AddScoped<GitHubStatsService>();

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

// Configure GitHub Auth Provider
server.UseAuth<GitHubAuthProvider>(c => c.UseGitHub());

await server.RunAsync();