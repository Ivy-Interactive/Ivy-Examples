using Ivy;

namespace LovableTravelBooking.Apps.TravelBooking;

public class PackageCard(TravelPackage package, Action onBookClick) : ViewBase
{
    public override object? Build()
    {
        var image = new Image(package.ImageUrl)
            .Width(Size.Full())
            .Height(Size.Units(48));
        image.ObjectFit = ImageFit.Cover;

        var content = Layout.Vertical().Gap(3)
            | image
            | (Layout.Vertical().Gap(2).Padding(4)
                | new Badge(package.Category).Secondary()
                | Text.H3(package.Name)
                | Text.Muted(package.Destination)
                | (Layout.Horizontal().Gap(3)
                    | Text.P($"★ {package.Rating:F1}").Bold().Color(Colors.Amber)
                    | Text.P(package.Duration).Color(Colors.Muted))
                | Text.H3($"${package.PricePerPerson:N0} / person").Color(Colors.Emerald)
                | new Button("Book Now", onBookClick).Primary().Width(Size.Full()));

        return new Card(content);
    }
}
