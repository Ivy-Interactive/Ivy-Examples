using Ivy;
using GradientGenerator.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Gradient Generator");
server.SetMetaDescription("A visual CSS gradient builder with customizable color stops, angles, and live preview.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/gradient-generator");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(GradientGeneratorApp));
await server.RunAsync();