using Ivy;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.Job.Dashboard.Apps;
using Hangfire.Job.Dashboard.Apps.JobDashboard;

var server = new Server();
server.SetMetaTitle("Hangfire Job Dashboard");
server.SetMetaDescription("A real-time dashboard for monitoring and managing Hangfire background jobs, including recurring jobs, queues, and job history.");
// TODO: Uncomment when Ivy publishes SetMetaGitHubUrl method
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/hangfire-job-dashboard");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

// Configure Hangfire with in-memory storage
GlobalConfiguration.Configuration.UseInMemoryStorage();

server.Services.AddHangfire(config => config.UseInMemoryStorage());
server.Services.AddHangfireServer();
server.Services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
server.Services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
server.Services.AddSingleton<HangfireService>();

server.UseDefaultApp(typeof(JobDashboardApp));

await server.RunAsync();