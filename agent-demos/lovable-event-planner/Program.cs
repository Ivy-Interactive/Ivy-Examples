using Ivy;
using Lovable.Event.Planner.Apps;
using Lovable.Event.Planner.Apps.EventPlanner;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Lovable Event Planner");
server.SetMetaDescription("An event planning and management app for browsing, creating, and RSVPing to events across categories like conferences, workshops, and social gatherings.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/lovable-event-planner");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(EventPlannerApp));
server.Services.AddSingleton<EventService>();
await server.RunAsync();