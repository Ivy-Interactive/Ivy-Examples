using FastMemberExample;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings().DefaultApp<FastMemberApp>().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);
await server.RunAsync();
