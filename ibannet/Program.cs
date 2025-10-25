using IbanNetExample;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<IbanNetApp>()
    .UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);
await server.RunAsync();