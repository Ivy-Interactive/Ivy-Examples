CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var customHeader = Layout.Vertical().Gap(2).Align(Align.Center)
    | new Html(@"
        <div>
          <a href=""https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fdiffengine%2Fdevcontainer.json&location=EuropeWest"">
            <img src=""https://github.com/codespaces/badge.svg"" alt=""Open DiffEngine in Codespaces"" />
          </a>
        </div>
      ")
    | new Button("Source Code").Url("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/diffengine").Icon(Icons.ExternalLink).Width(Size.Full()).Height(Size.Units(10));
var chromeSettings = new ChromeSettings()
    .DefaultApp<DiffEngineApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);
await server.RunAsync();
