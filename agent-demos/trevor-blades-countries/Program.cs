using Ivy;
using Microsoft.Extensions.DependencyInjection;
using Trevor.Blades.Countries;

var server = new Server();
server.SetMetaTitle("Trevor Blades Countries");
server.SetMetaDescription("A travel planner app that lets you explore countries, compare them side by side, and build a travel itinerary using the Trevor Blades Countries GraphQL API.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/trevor-blades-countries");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif

server.Services.AddHttpClient<CountriesService>();

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseChrome(new ChromeSettings().UseTabs(preventDuplicates: true));
server.UseDefaultApp(typeof(TravelPlannerApp));
await server.RunAsync();