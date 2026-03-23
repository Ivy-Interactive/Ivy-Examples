using Ivy;
using Microsoft.Extensions.DependencyInjection;
using SWAPI.Graph.QL.Apps.Services;

var server = new Server();
server.SetMetaTitle("SWAPI Graph QL");
server.SetMetaDescription("A Star Wars encyclopedia app that browses characters, starships, and planets from the SWAPI API with detailed views and tabbed navigation.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/swapi-graph-ql");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.Services.AddHttpClient<SwapiService>(c => c.BaseAddress = new Uri("https://swapi.dev/api/"));
server.UseAppShell(new AppShellSettings().UseTabs(preventDuplicates: true));
await server.RunAsync();