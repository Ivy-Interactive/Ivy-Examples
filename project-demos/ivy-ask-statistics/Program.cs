using IvyAskStatistics.Apps;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var appShellSettings = new AppShellSettings()
    .DefaultApp<RunApp>()
    .UseTabs(preventDuplicates: true)
    .UseFooterMenuItemsTransformer((items, navigator) =>
    {
        var list = items.ToList();
        list.Add(MenuItem.Default("Generate All Questions").Icon(Icons.Sparkles).OnSelect(() =>
        {
            GenerateAllBridge.Request("generate-all");
            navigator.Navigate(typeof(QuestionsApp));
        }));
        return list;
    });

server.UseAppShell(appShellSettings);
await server.RunAsync();
