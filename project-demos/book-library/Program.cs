using BookLibrary.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
// Local dev: volume = project folder so db.sqlite is the file next to .csproj (visible in git), not under bin/
server.UseVolume(new FolderVolume(ResolveVolumeRoot()));
var appShellSettings = new AppShellSettings()
    .DefaultApp<DashboardApp>()
    .UseTabs(preventDuplicates: true);

server.UseAppShell(appShellSettings);
await server.RunAsync();

static string? ResolveVolumeRoot()
{
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        return "/app/data";

    var env = Environment.GetEnvironmentVariable("BOOK_LIBRARY_DATA");
    if (!string.IsNullOrWhiteSpace(env))
        return Path.GetFullPath(env);

    // Typical SDK layout: .../project/bin/Debug/net10.0/ → project root is three levels up
    return Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory, "..", "..", ".."));
}
