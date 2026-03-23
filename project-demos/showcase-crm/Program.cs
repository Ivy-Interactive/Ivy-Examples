using ShowcaseCrm.Apps;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var appShellSettings = new AppShellSettings()
    .UseTabs(preventDuplicates: true)
    .DefaultApp<DashboardApp>();
server.UseAppShell(appShellSettings);
await server.RunAsync();