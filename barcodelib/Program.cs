using BarcodeLibExample.Apps;
using Ivy;
using Ivy.Chrome;
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
var customHeader = Layout.Vertical().Gap(2).Align(Align.Center)
    | new Html(@"
        <div>
          <a href=""https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fbarcodelib%2Fdevcontainer.json&location=EuropeWest"">
            <img src=""https://github.com/codespaces/badge.svg"" alt=""Open BarcodeLib in Codespaces"" />
          </a>
        </div>
      ");
var chromeSettings = new ChromeSettings()
    .DefaultApp<BarcodeLibApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);
await server.RunAsync();
