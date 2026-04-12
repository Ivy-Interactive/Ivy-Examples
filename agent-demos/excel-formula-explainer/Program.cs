using Ivy;
using Excel.Formula.Explainer.Apps;

var server = new Server();
server.SetMetaTitle("Excel Formula Explainer");
server.SetMetaDescription("Paste any Excel formula and get a plain-English explanation with step-by-step breakdown of each function used.");
// TODO: Uncomment when Ivy publishes SetMeta methods
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/excel-formula-explainer");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(FormulaExplainerApp));
await server.RunAsync();