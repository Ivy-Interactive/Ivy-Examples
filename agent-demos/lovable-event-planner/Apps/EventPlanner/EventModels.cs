namespace Lovable.Event.Planner.Apps.EventPlanner;

public enum EventCategory
{
    Conference,
    Workshop,
    Social,
    Sports,
    Music,
    Food,
    Tech,
    Art
}

public record AttendeeModel(
    string Id,
    string Name,
    string Email,
    DateTime RsvpDate
);

public record EventModel(
    string Id,
    string Title,
    string Description,
    EventCategory Category,
    DateTime DateTime,
    string Location,
    int Capacity,
    string ImageUrl,
    string Organizer,
    List<AttendeeModel> Attendees
);

public record EventFormModel(
    string Title,
    string Description,
    EventCategory Category,
    DateTime DateTime,
    string Location,
    int Capacity,
    string Organizer
)
{
    public EventFormModel() : this("", "", EventCategory.Conference, DateTime.Now.AddDays(7), "", 50, "") { }
}

public record RsvpFormModel(string Name, string Email)
{
    public RsvpFormModel() : this("", "") { }
}
