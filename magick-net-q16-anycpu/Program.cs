using MagickNet;

// Set culture for consistent behavior
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

// Create and configure Ivy server
var server = new Server();

#if DEBUG
server.UseHotReload();
#endif

// Register apps and connections from this assembly
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

// Configure Chrome to start with MagickApp
var customHeader = Layout.Vertical().Gap(2)
    |new Embed("https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmagick-net-q16-anycpu%2Fdevcontainer.json&location=EuropeWest");
var chromeSettings = new ChromeSettings()
    .DefaultApp<MagickNetApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);

// Start the server
await server.RunAsync();