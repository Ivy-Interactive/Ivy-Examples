using CatFacts.Apps;
using CatFacts.Apps.CatFacts;
using Ivy;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Cat Facts");
server.SetMetaDescription("A fun cat facts explorer with daily random facts, breed browsing, and a favorites collection powered by the CatFact API.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/cat-facts");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.Services.AddSingleton<HttpClient>();
server.Services.AddSingleton<CatFactApiService>();
server.UseDefaultApp(typeof(CatFactsApp));
await server.RunAsync();