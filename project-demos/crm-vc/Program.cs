CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);
server.UseVolume(new FolderVolume(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" ? "/app/data" : null));
await server.RunAsync();