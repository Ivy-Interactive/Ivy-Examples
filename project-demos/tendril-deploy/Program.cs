using Ivy.Auth.Sliplane;
using TendrilDeploy.Api;
using TendrilDeploy.Apps;
using TendrilDeploy.Services;
using TendrilDeploy;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

server.Services.AddHttpClient("Ivy", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Ivy-TendrilDeploy/1.0");
});

server.Services.AddHttpClient("GitHubRaw", client =>
{
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Ivy-TendrilDeploy/1.0");
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
server.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, TendrilApiStartupFilter>();
server.Services.AddScoped<TendrilDeployService>();
server.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title       = "Tendril Deploy API";
        doc.Info.Version     = "v1";
        doc.Info.Description =
            "Programmatic Tendril instance deployment on Sliplane.\n\n" +
            "**Authentication** is optional — if `TendrilDeploy:ApiKey` is set in config, " +
            "send it via `X-Api-Key`. Endpoints that query Sliplane also require `X-Sliplane-Token` " +
            "(Sliplane → Team Settings → API Tokens).";

        // Show only our endpoints — remove everything Ivy registers internally.
        foreach (var (path, item) in doc.Paths.ToList())
        {
            var isTendrilApi = item.Operations.Values.Any(op =>
                op.Tags?.Any(t => t.Name == "TendrilApi") == true);
            if (!isTendrilApi)
                doc.Paths.Remove(path);
        }
        if (doc.Tags != null)
        {
            var keep = doc.Tags.Where(t => t.Name == "TendrilApi").ToList();
            doc.Tags.Clear();
            foreach (var t in keep)
                doc.Tags.Add(t);
        }

        return Task.CompletedTask;
    });
});

#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

server.UseAuth<SliplaneAuthProvider>();

var cfg = server.Configuration;
var appShellSettings = new AppShellSettings()
    .DefaultApp<TendrilDeployApp>()
    .UseTabs(preventDuplicates: true)
    .UseFooterMenuItemsTransformer((items, navigator) =>
    {
        var list = items.ToList();
        list.Insert(0, MenuItem.Default("API documentation").Icon(Icons.BookOpen).OnSelect(() =>
            navigator.Navigate(TendrilDocsLink.ResolveScalarUrl(cfg))));
        return list;
    });
server.UseAppShell(appShellSettings);

await server.RunAsync();
