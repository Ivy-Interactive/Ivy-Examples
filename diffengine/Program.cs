const string CodespacesUrl = "https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fdiffengine%2Fdevcontainer.json&location=EuropeWest";

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var customHeader = Layout.Vertical().Gap(2)
    |new Embed(CodespacesUrl);
var chromeSettings = new ChromeSettings()
    .DefaultApp<DiffEngineApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);
await server.RunAsync();
