using BarcodeLibExample.Apps;
using Ivy;
using System.Globalization;

var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var customHeader = Layout.Vertical().Gap(2)
    |new Embed("https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fbarcodelib%2Fdevcontainer.json&location=EuropeWest");
var appShellSettings = new AppShellSettings()
    .DefaultApp<BarcodeLibApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseAppShell(appShellSettings);
await server.RunAsync();
