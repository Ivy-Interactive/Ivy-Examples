using Ivy;
using PDF.Compressor.Apps;

var server = new Server();
server.SetMetaTitle("PDF Compressor");
server.SetMetaDescription("A web application that lets you upload PDF files and compress them to reduce file size, with adjustable compression quality levels.");
// TODO: Uncomment when Ivy publishes SetMeta methods
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/pdf-compressor");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(CompressorApp));
await server.RunAsync();