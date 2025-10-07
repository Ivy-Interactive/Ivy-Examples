using AsposeOcrExample.Apps;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var customHeader = Layout.Vertical().Gap(2)
    | new Html(@"
        <div>
          <a href=""https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Faspose-ocr%2Fdevcontainer.json&location=EuropeWest"">
            <img src=""https://github.com/codespaces/badge.svg"" alt=""Open in GitHub Codespaces"" />
          </a>
        </div>
      ");
var chromeSettings = new ChromeSettings()
    .DefaultApp<ImageToTextApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);
await server.RunAsync();
