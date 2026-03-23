using Ivy;
using Certificate.Decoder.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Certificate Decoder");
server.SetMetaDescription("A web application that decodes and inspects X.509 certificates. Paste a PEM or Base64-encoded certificate to view its details.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/certificate-decoder");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(CertificateDecoderApp));
await server.RunAsync();