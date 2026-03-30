using Ivy;
using ASCIIArtGenerator.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("ASCII Art Generator");
server.SetMetaDescription("Convert text to ASCII art using various fonts or transform images into ASCII character art with adjustable width and inversion.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/ascii-art-generator");
server.Services.AddSingleton<AsciiArtService>();
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(ASCIIArtGeneratorApp));
await server.RunAsync();