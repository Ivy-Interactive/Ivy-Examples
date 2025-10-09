using DnsClient;
using DnsClientExample.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();

server.AddConnectionsFromAssembly();

server.Services.AddSingleton<ILookupClient>(i => new LookupClient());

var chromeSettings = new ChromeSettings().DefaultApp<DnsLookUpApp>().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);

await server.RunAsync();