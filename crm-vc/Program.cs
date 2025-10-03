CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);
server.UseVolume(new FolderVolume(Ivy.Utils.IsProduction() ? "/app/data" : null));
await server.RunAsync();