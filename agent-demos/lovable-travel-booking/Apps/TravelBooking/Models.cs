namespace LovableTravelBooking.Apps.TravelBooking;

public record TravelPackage(
    int Id,
    string Name,
    string Destination,
    string ImageUrl,
    decimal PricePerPerson,
    string Duration,
    double Rating,
    string Description,
    string Category
);

public record Booking(
    int Id,
    TravelPackage Package,
    DateTime CheckIn,
    DateTime CheckOut,
    int Travelers,
    decimal TotalPrice,
    DateTime BookedAt,
    string Status
);
