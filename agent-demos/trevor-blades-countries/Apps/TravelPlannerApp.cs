using Ivy;

namespace Trevor.Blades.Countries;

[App(title: "Travel Planner", icon: Icons.Globe, group: new[] { "Travel" })]
public class TravelPlannerApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Tabs(
            new Tab("🌍 Explore", new ExploreView()),
            new Tab("⚖️ Compare", new CompareView()),
            new Tab("🗺️ Itinerary", new ItineraryView())
        );
    }
}
