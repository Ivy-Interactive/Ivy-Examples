using Ivy.Auth.Sliplane;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
server.UseAuth<SliplaneAuthProvider>();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings()
    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);
await server.RunAsync();