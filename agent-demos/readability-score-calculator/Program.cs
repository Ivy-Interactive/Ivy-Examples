using Ivy;
using Microsoft.Extensions.DependencyInjection;
using Readability.Score.Calculator.Apps;

var server = new Server();
server.SetMetaTitle("Readability Score Calculator");
server.SetMetaDescription("Analyze text readability across multiple indices including Flesch Reading Ease, Gunning Fog, and more.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/readability-score-calculator");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(ReadabilityApp));
await server.RunAsync();