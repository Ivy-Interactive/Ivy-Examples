using IvyBenchmark.Apps;

var server = new Server();
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseAppShell(new AppShellSettings()
    .DefaultApp<DashboardApp>()
    .UseTabs(preventDuplicates: true));
await server.RunAsync();
