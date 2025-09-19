using DnsClient;
using IvySample.DnsClient.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddApp(typeof(DnsLookUpApp), isDefault:true);

server.AddConnectionsFromAssembly();

server.Services.AddSingleton<ILookupClient>(i => new LookupClient());

await server.RunAsync();