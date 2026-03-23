using Helloworld.Apps;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var appShellSettings = new AppShellSettings().DefaultApp<HelloApp>().UseTabs(preventDuplicates: true);
server.UseAppShell(appShellSettings);
await server.RunAsync();
