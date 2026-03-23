namespace Lovable.Event.Planner.Apps.EventPlanner;

public class EventService
{
    private readonly List<EventModel> _events = new();
    private readonly Lock _lock = new();

    public EventService()
    {
        var now = DateTime.Now;

        _events.AddRange(new[]
        {
            new EventModel(
                Guid.NewGuid().ToString(), "Tech Innovation Summit 2026",
                "A premier conference bringing together tech leaders, developers, and entrepreneurs to explore the latest in AI, cloud computing, and software development.",
                EventCategory.Tech, now.AddDays(14).Date.AddHours(9), "Convention Center, San Francisco", 500,
                "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800", "TechForward Inc.",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Alice Chen", "alice@example.com", now.AddDays(-5)),
                    new(Guid.NewGuid().ToString(), "Bob Martinez", "bob@example.com", now.AddDays(-3)),
                    new(Guid.NewGuid().ToString(), "Carol White", "carol@example.com", now.AddDays(-1))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "Italian Cooking Masterclass",
                "Learn to make authentic pasta, risotto, and tiramisu from a Michelin-starred chef. All ingredients and equipment provided.",
                EventCategory.Food, now.AddDays(7).Date.AddHours(18), "Culinary Arts Studio, Chicago", 24,
                "https://images.unsplash.com/photo-1556910103-1c02745aae4d?w=800", "Chef Giovanni Rossi",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Diana Park", "diana@example.com", now.AddDays(-2)),
                    new(Guid.NewGuid().ToString(), "Edward Kim", "edward@example.com", now.AddDays(-1))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "Modern Art Exhibition Opening",
                "Featuring works from emerging artists exploring themes of identity, technology, and nature through mixed media installations.",
                EventCategory.Art, now.AddDays(10).Date.AddHours(19), "Downtown Gallery, New York", 150,
                "https://images.unsplash.com/photo-1531243269054-5ebf6f34081e?w=800", "NYC Arts Collective",
                new List<AttendeeModel>()),
            new EventModel(
                Guid.NewGuid().ToString(), "City Marathon 2026",
                "Annual marathon through scenic city routes. Categories for full marathon, half marathon, and 10K fun run. All fitness levels welcome.",
                EventCategory.Sports, now.AddDays(30).Date.AddHours(6), "Central Park, New York", 2000,
                "https://images.unsplash.com/photo-1513593771513-7b58b6c4af38?w=800", "NYC Running Club",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Frank Lopez", "frank@example.com", now.AddDays(-10)),
                    new(Guid.NewGuid().ToString(), "Grace Tanaka", "grace@example.com", now.AddDays(-7)),
                    new(Guid.NewGuid().ToString(), "Hank Williams", "hank@example.com", now.AddDays(-4)),
                    new(Guid.NewGuid().ToString(), "Iris Nguyen", "iris@example.com", now.AddDays(-2))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "Jazz Under the Stars",
                "An outdoor evening of live jazz performances featuring local and international artists. Food trucks and craft beverages available.",
                EventCategory.Music, now.AddDays(5).Date.AddHours(20), "Riverside Amphitheater, Austin", 300,
                "https://images.unsplash.com/photo-1511192336575-5a79af67a629?w=800", "Austin Music Society",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Jake Rivera", "jake@example.com", now.AddDays(-3))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "UX Design Workshop",
                "Hands-on workshop covering user research methods, wireframing, prototyping, and usability testing. Bring your laptop.",
                EventCategory.Workshop, now.AddDays(12).Date.AddHours(10), "Design Hub, Seattle", 30,
                "https://images.unsplash.com/photo-1531403009284-440f080d1e12?w=800", "DesignLab Academy",
                new List<AttendeeModel>()),
            new EventModel(
                Guid.NewGuid().ToString(), "Community Block Party",
                "Annual neighborhood celebration with live music, games, food stalls, and activities for the whole family.",
                EventCategory.Social, now.AddDays(21).Date.AddHours(12), "Maple Street Park, Portland", 500,
                "https://images.unsplash.com/photo-1529543544282-ea30407faa44?w=800", "Portland Community Assoc.",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Karen O'Brien", "karen@example.com", now.AddDays(-6)),
                    new(Guid.NewGuid().ToString(), "Leo Chang", "leo@example.com", now.AddDays(-4))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "AI & Machine Learning Conference",
                "Deep-dive sessions on neural networks, NLP, computer vision, and responsible AI. Featuring hands-on labs and keynote speakers from top research labs.",
                EventCategory.Conference, now.AddDays(45).Date.AddHours(9), "Tech Campus, Boston", 400,
                "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?w=800", "ML Research Group",
                new List<AttendeeModel>
                {
                    new(Guid.NewGuid().ToString(), "Maya Patel", "maya@example.com", now.AddDays(-8)),
                    new(Guid.NewGuid().ToString(), "Noah Fischer", "noah@example.com", now.AddDays(-5))
                }),
            new EventModel(
                Guid.NewGuid().ToString(), "Sunset Yoga Retreat",
                "A relaxing weekend of yoga, meditation, and wellness workshops set against stunning ocean views. All levels welcome.",
                EventCategory.Sports, now.AddDays(18).Date.AddHours(16), "Ocean Bluff Resort, Malibu", 40,
                "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800", "Zen Wellness Studio",
                new List<AttendeeModel>())
        });
    }

    public List<EventModel> GetAll()
    {
        lock (_lock) return _events.ToList();
    }

    public EventModel? GetById(string id)
    {
        lock (_lock) return _events.FirstOrDefault(e => e.Id == id);
    }

    public EventModel AddEvent(EventFormModel form)
    {
        var evt = new EventModel(
            Guid.NewGuid().ToString(),
            form.Title, form.Description, form.Category,
            form.DateTime, form.Location, form.Capacity,
            "", form.Organizer, new List<AttendeeModel>()
        );
        lock (_lock) _events.Add(evt);
        return evt;
    }

    public bool Rsvp(string eventId, string name, string email)
    {
        lock (_lock)
        {
            var evt = _events.FirstOrDefault(e => e.Id == eventId);
            if (evt is null) return false;
            if (evt.Attendees.Count >= evt.Capacity) return false;
            if (evt.Attendees.Any(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase))) return false;

            evt.Attendees.Add(new AttendeeModel(Guid.NewGuid().ToString(), name, email, DateTime.Now));
            return true;
        }
    }

    public bool CancelRsvp(string eventId, string email)
    {
        lock (_lock)
        {
            var evt = _events.FirstOrDefault(e => e.Id == eventId);
            if (evt is null) return false;
            var attendee = evt.Attendees.FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (attendee is null) return false;
            evt.Attendees.Remove(attendee);
            return true;
        }
    }
}
