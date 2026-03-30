using Ivy;
using MeetingCostCalculator.Apps;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Meeting Cost Calculator");
server.SetMetaDescription("Track the real-time cost of your meetings by calculating expenses based on attendees and hourly rates.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/meeting-cost-calculator");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.UseDefaultApp(typeof(MeetingCostCalculatorApp));
await server.RunAsync();