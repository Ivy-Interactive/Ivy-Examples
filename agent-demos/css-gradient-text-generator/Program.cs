using Ivy;
using CSSGradientTextGenerator.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("CSS Gradient Text Generator");
server.SetMetaDescription("An interactive tool for creating CSS gradient text effects with customizable colors, direction, font size, and weight.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/css-gradient-text-generator");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(CSSGradientTextGeneratorApp));
await server.RunAsync();