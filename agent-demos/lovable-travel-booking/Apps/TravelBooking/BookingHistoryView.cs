using Ivy;

namespace LovableTravelBooking.Apps.TravelBooking;

public class BookingHistoryView : ViewBase
{
    private record BookingRow(
        string Destination,
        string CheckIn,
        string CheckOut,
        int Travelers,
        string Total,
        string Status
    );

    public override object? Build()
    {
        var bookingService = UseService<BookingService>();
        var bookings = bookingService.GetBookings();

        if (bookings.Count == 0)
            return Callout.Info("No bookings yet. Browse packages and make your first booking!");

        var rows = bookings.Select(b => new BookingRow(
            b.Package.Destination,
            b.CheckIn.ToShortDateString(),
            b.CheckOut.ToShortDateString(),
            b.Travelers,
            b.TotalPrice.ToString("C"),
            b.Status
        )).ToList();

        return Layout.Vertical()
            | Text.H3("Booking History")
            | new TableBuilder<BookingRow>(rows);
    }
}
