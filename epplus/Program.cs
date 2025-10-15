
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings()

    .UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

ExcelPackage.License.SetNonCommercialOrganization("Ivy");
await server.RunAsync();
