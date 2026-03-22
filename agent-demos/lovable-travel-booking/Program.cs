using Ivy;
using LovableTravelBooking.Apps.TravelBooking;
using Microsoft.Extensions.DependencyInjection;

var server = new Server();
server.SetMetaTitle("Lovable Travel Booking");
server.SetMetaDescription("A travel booking application that lets users browse, search, and filter travel packages by category, view package details, and make bookings.");
// TODO: Uncomment when Ivy publishes SetMeta methods
// server.SetMetaGitHubUrl("https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/agent-demos/lovable-travel-booking");
server.UseCulture("en-US");
#if DEBUG
server.UseHotReload();
#endif
server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();
server.Services.AddSingleton<BookingService>();
server.UseDefaultApp(typeof(LovableTravelBooking.Apps.TravelBookingApp));
await server.RunAsync();