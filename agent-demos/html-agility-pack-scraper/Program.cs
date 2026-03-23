using Ivy;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("HTML Agility Pack Scraper");
server.SetMetaDescription("A web scraper tool that extracts structured data from any web page using CSS selectors, with JSON and CSV export.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/html-agility-pack-scraper");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseAppShell(new AppShellSettings().UseTabs(preventDuplicates: true));
await server.RunAsync();
