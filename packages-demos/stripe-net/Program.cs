using StripeNetExample;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");


var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.Services.AddSingleton(config);
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings()
    .DefaultApp<StripeNetApp>()
    .UseTabs(preventDuplicates: true);
server.Services.AddHttpContextAccessor();
server.UseChrome(chromeSettings);
await server.RunAsync();
