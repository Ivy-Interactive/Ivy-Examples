using Ivy;
using LovableTravelBooking.Apps.TravelBooking;

namespace LovableTravelBooking.Apps;

[App(title: "Travel Booking", icon: Icons.Plane, group: new[] { "Travel" })]
public class TravelBookingApp : ViewBase
{
    public override object? Build()
    {
        var search = UseState("");
        var category = UseState("All");
        var selectedPackage = UseState<TravelPackage?>(null);
        var isSheetOpen = UseState(false);

        var categories = new[] { "All", "Beach", "Mountain", "City", "Adventure" };

        var filtered = TravelData.Packages
            .Where(p => category.Value == "All" || p.Category == category.Value)
            .Where(p => string.IsNullOrEmpty(search.Value) ||
                p.Name.Contains(search.Value, StringComparison.OrdinalIgnoreCase) ||
                p.Destination.Contains(search.Value, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search.Value, StringComparison.OrdinalIgnoreCase))
            .ToList();


        var categoryFilter = Layout.Horizontal().Gap(2);
        foreach (var cat in categories)
        {
            var c = cat;
            categoryFilter |= c == category.Value
                ? new Button(c, () => category.Set(c)).Primary().Small()
                : new Button(c, () => category.Set(c)).Outline().Small();
        }


        var grid = Layout.Grid().Columns(3).Gap(6);
        foreach (var pkg in filtered)
        {
            var p = pkg;
            grid |= new PackageCard(p, () =>
            {
                selectedPackage.Set(p);
                isSheetOpen.Set(true);
            });
        }


        var browseContent = Layout.Vertical()
            | (Layout.Vertical().Gap(2)
                | Text.H1("✈️ Travel Booking")
                | Text.Lead("Discover amazing travel packages and book your next adventure"))
            | search.ToTextInput().Placeholder("Search destinations, packages, or categories...")
            | categoryFilter
            | (filtered.Count > 0
                ? (object)grid
                : Callout.Info("No packages found matching your search criteria."));


        var historyContent = new BookingHistoryView();


        var tabs = Layout.Tabs(
            new Tab("Browse Packages", browseContent).Icon(Icons.Search),
            new Tab("Booking History", historyContent).Icon(Icons.Clock)
        );


        object? sheetView = null;
        if (selectedPackage.Value is { } selected)
        {
            sheetView = new BookingSheet(selected, () =>
            {
                isSheetOpen.Set(false);
            }).ToSheet(isSheetOpen, title: "Book Your Trip");
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | tabs
                | sheetView);
    }
}
