using Ivy;

namespace Lovable.Event.Planner.Apps.EventPlanner;

public class EventDetailView(string eventId) : ViewBase
{
    public override object? Build()
    {
        var service = UseService<EventService>();
        var client = UseService<IClientProvider>();
        var refreshToken = UseState(0);
        var rsvpForm = UseState(() => new RsvpFormModel());

        var evt = service.GetById(eventId);
        if (evt is null)
        {
            return Layout.Center()
                | (Layout.Vertical().Center().Gap(2)
                    | Icons.CalendarX.ToIcon().Large()
                    | Text.H3("Event not found")
                    | Text.Muted("This event may have been removed."));
        }

        var isFull = evt.Attendees.Count >= evt.Capacity;
        var percentage = evt.Capacity > 0
            ? (int)((double)evt.Attendees.Count / evt.Capacity * 100)
            : 0;

        var header = Layout.Vertical().Gap(2)
            | Text.H1(evt.Title)
            | (Layout.Horizontal().Gap(2)
                | new Badge(evt.Category.ToString()).Variant(GetCategoryVariant(evt.Category))
                | (Layout.Horizontal().Gap(1)
                    | Icons.User.ToIcon().Small()
                    | Text.Muted($"Organized by {evt.Organizer}")));

        var details = Layout.Vertical().Gap(3)
            | Text.H3("Event Details")
            | (Layout.Horizontal().Gap(2)
                | Icons.CalendarDays.ToIcon()
                | Text.Block(evt.DateTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt")))
            | (Layout.Horizontal().Gap(2)
                | Icons.MapPin.ToIcon()
                | Text.Block(evt.Location))
            | (Layout.Horizontal().Gap(2)
                | Icons.Users.ToIcon()
                | Text.Block($"{evt.Attendees.Count} / {evt.Capacity} spots taken"))
            | new Progress(percentage);

        var description = Layout.Vertical().Gap(2)
            | Text.H3("About this Event")
            | Text.P(evt.Description);

        var encodedLocation = Uri.EscapeDataString(evt.Location);
        var mapSection = Layout.Vertical().Gap(2)
            | Text.H3("Location")
            | new Html($"<iframe width=\"100%\" height=\"300\" frameborder=\"0\" scrolling=\"no\" src=\"https://maps.google.com/maps?q={encodedLocation}&output=embed\" style=\"border:1px solid #ccc; border-radius:8px;\"></iframe>")
                .DangerouslyAllowScripts();

        object rsvpSection;
        if (isFull)
        {
            rsvpSection = Layout.Vertical().Gap(2)
                | Text.H3("RSVP")
                | Callout.Warning("This event is at full capacity. No more RSVPs available.");
        }
        else
        {
            rsvpSection = Layout.Vertical().Gap(2)
                | Text.H3("RSVP")
                | rsvpForm.ToForm("RSVP Now")
                    .Required(m => m.Name, m => m.Email)
                    .OnSubmit(async model =>
                    {
                        var success = service.Rsvp(eventId, model.Name, model.Email);
                        if (success)
                        {
                            client.Toast("You have successfully RSVP'd!", variant: ToastVariant.Success);
                            rsvpForm.Set(new RsvpFormModel());
                            refreshToken.Set(refreshToken.Value + 1);
                        }
                        else
                        {
                            client.Toast("Could not RSVP. You may already be registered or the event is full.", variant: ToastVariant.Destructive);
                        }
                    });
        }

        object attendeeSection;
        if (evt.Attendees.Count == 0)
        {
            attendeeSection = Layout.Vertical().Gap(2)
                | (Layout.Horizontal().Gap(2)
                    | Text.H3("Attendees")
                    | new Badge("0").Secondary())
                | Text.Muted("No attendees yet. Be the first to RSVP!");
        }
        else
        {
            var tableData = evt.Attendees.Select(a => new
            {
                a.Name,
                a.Email,
                RsvpDate = a.RsvpDate.ToString("MMM dd, yyyy")
            }).ToArray();

            attendeeSection = Layout.Vertical().Gap(2)
                | (Layout.Horizontal().Gap(2)
                    | Text.H3("Attendees")
                    | new Badge($"{evt.Attendees.Count}").Info())
                | tableData.ToTable().Width(Size.Full());
        }

        return Layout.Vertical()
            | header
            | new Separator()
            | details
            | new Separator()
            | description
            | new Separator()
            | mapSection
            | new Separator()
            | rsvpSection
            | new Separator()
            | attendeeSection;
    }

    private static BadgeVariant GetCategoryVariant(EventCategory category) => category switch
    {
        EventCategory.Conference => BadgeVariant.Primary,
        EventCategory.Workshop => BadgeVariant.Info,
        EventCategory.Social => BadgeVariant.Success,
        EventCategory.Sports => BadgeVariant.Warning,
        EventCategory.Music => BadgeVariant.Secondary,
        EventCategory.Food => BadgeVariant.Success,
        EventCategory.Tech => BadgeVariant.Primary,
        EventCategory.Art => BadgeVariant.Secondary,
        _ => BadgeVariant.Outline
    };
}
