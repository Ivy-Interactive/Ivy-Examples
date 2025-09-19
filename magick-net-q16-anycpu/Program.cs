using MagickNetExample.Apps;

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
var chromeSettings = new ChromeSettings()
    .DefaultApp<MagickApp>()
    .UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);

// Start the server
await server.RunAsync();