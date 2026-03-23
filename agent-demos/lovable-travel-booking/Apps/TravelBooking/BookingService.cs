namespace LovableTravelBooking.Apps.TravelBooking;

public class BookingService
{
    private readonly List<Booking> _bookings = [];
    private int _nextId = 1;

    public Booking AddBooking(TravelPackage package, DateTime checkIn, DateTime checkOut, int travelers)
    {
        var totalPrice = package.PricePerPerson * travelers;
        var booking = new Booking(
            _nextId++,
            package,
            checkIn,
            checkOut,
            travelers,
            totalPrice,
            DateTime.Now,
            "Confirmed"
        );
        _bookings.Add(booking);
        return booking;
    }

    public IReadOnlyList<Booking> GetBookings() => _bookings.AsReadOnly();
}
