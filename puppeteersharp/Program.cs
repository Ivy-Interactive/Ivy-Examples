using PuppeteerSharpExample;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
var chromeSettings = new ChromeSettings().DefaultApp<PuppeteerSharpApp>().UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);
var fetcher = new BrowserFetcher();

Console.WriteLine("Downloading Browser... This may take a while depending on your internet connection...");
await fetcher.DownloadAsync();

Console.WriteLine("Download complete. Running App..");

server.UseBuilder(builder =>
{
    builder.Services.AddSingleton<IStartupFilter, DownloadEndpointStartupFilter>();
});

await server.RunAsync();
