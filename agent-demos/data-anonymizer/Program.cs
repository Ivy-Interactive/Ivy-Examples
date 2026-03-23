using Ivy;
using Data.Anonymizer.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Data Anonymizer");
server.SetMetaDescription("Upload a CSV file and anonymize sensitive data using configurable strategies like masking, hashing, randomizing, or redacting columns.");
server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/data-anonymizer");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(DataAnonymizerApp));
await server.RunAsync();