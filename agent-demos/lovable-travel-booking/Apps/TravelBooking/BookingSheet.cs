using Ivy;

namespace LovableTravelBooking.Apps.TravelBooking;

public class BookingSheet(TravelPackage package, Action onBooked) : ViewBase
{
    public override object? Build()
    {
        var bookingService = UseService<BookingService>();
        var client = UseService<IClientProvider>();

        var checkIn = UseState(DateTime.Today.AddDays(7));
        var checkOut = UseState(DateTime.Today.AddDays(14));
        var travelers = UseState(2);

        var totalPrice = package.PricePerPerson * travelers.Value;
        var isValid = checkOut.Value > checkIn.Value && travelers.Value >= 1;

        return Layout.Vertical()
            | Text.H3(package.Name)
            | Text.Muted($"{package.Destination} · {package.Duration}")
            | Text.P($"${package.PricePerPerson} per person")
            | new Separator()
            | checkIn.ToDateTimeInput().WithField().Label("Check-in Date")
            | checkOut.ToDateTimeInput().WithField().Label("Check-out Date")
            | travelers.ToNumberInput().WithField().Label("Number of Travelers")
            | (checkOut.Value <= checkIn.Value
                ? Callout.Warning("Check-out date must be after check-in date.")
                : null)
            | new Separator()
            | Text.Lead($"Total: ${totalPrice:F2}")
            | new Button("Confirm Booking", () =>
            {
                if (!isValid) return;
                bookingService.AddBooking(package, checkIn.Value, checkOut.Value, travelers.Value);
                client.Toast("Booking confirmed!");
                onBooked();
            }).Primary().Disabled(!isValid);
    }
}
