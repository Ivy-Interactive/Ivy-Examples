using Ivy;
using Todo.Apps;

var server = new Server();
server.SetMetaTitle("Todo");
server.SetMetaDescription("A simple to-do list app with add, check, and delete functionality built with Ivy.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/todo");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(TodoApp));
await server.RunAsync();