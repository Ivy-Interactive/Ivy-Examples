using Ivy;
using Microsoft.Extensions.DependencyInjection;
using RickAndMortyGraphQL.Services;

var server = new Server();
server.SetMetaTitle("Rick and Morty GraphQL");
server.SetMetaDescription("Browse Rick and Morty characters, episodes, and locations using the Rick and Morty GraphQL API with filtering, pagination, and detail views.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/rick-and-morty-graphql");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.Services.AddHttpClient();
server.Services.AddSingleton<RickAndMortyClient>();
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseAppShell(new AppShellSettings().UseTabs(preventDuplicates: true));
await server.RunAsync();