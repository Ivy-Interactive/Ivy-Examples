using Ivy;

namespace Lovable.Event.Planner.Apps.EventPlanner;

public class EventBrowseView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<EventService>();
        var client = UseService<IClientProvider>();
        var blades = UseContext<IBladeService>();

        var search = UseState("");
        var categoryFilter = UseState("All");
        var formState = UseState(() => new EventFormModel());
        var isSheetOpen = UseState(false);

        var allEvents = service.GetAll();

        var filtered = allEvents.Where(e =>
        {
            var matchesSearch = string.IsNullOrWhiteSpace(search.Value) ||
                e.Title.Contains(search.Value, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search.Value, StringComparison.OrdinalIgnoreCase);

            var matchesCategory = categoryFilter.Value == "All" ||
                e.Category.ToString() == categoryFilter.Value;

            return matchesSearch && matchesCategory;
        }).ToList();

        var headerRow = Layout.Horizontal()
            | Text.H1("Events")
            | new Badge($"{allEvents.Count} events").Secondary()
            | new Spacer()
            | new Button("Create Event", icon: Icons.CalendarPlus).Primary()
                .OnClick(() => isSheetOpen.Set(true));

        var categoryOptions = new[] { "All", "Conference", "Workshop", "Social", "Sports", "Music", "Food", "Tech", "Art" }.ToOptions();

        var filterRow = Layout.Horizontal()
            | search.ToTextInput().Placeholder("Search events...")
            | categoryFilter.ToSelectInput(categoryOptions).Placeholder("All Categories");

        var sheet = formState.ToForm("Create Event")
            .Required(m => m.Title, m => m.Location, m => m.Organizer)
            .OnSubmit(async model =>
            {
                service.AddEvent(model);
                formState.Set(new EventFormModel());
                client.Toast("Event created successfully!", variant: ToastVariant.Success);
            })
            .ToSheet(isSheetOpen, "Create Event", "Fill in the event details");

        object content;
        if (filtered.Count == 0)
        {
            content = Layout.Vertical().Center().Padding(20)
                | Icons.Search.ToIcon().Large()
                | Text.H3("No events found")
                | Text.Muted("Try adjusting your search or filter criteria");
        }
        else
        {
            var grid = Layout.Grid().Columns(3);
            foreach (var evt in filtered)
            {
                var percentage = evt.Capacity > 0
                    ? (int)((double)evt.Attendees.Count / evt.Capacity * 100)
                    : 0;

                var card = new Card(
                    Layout.Vertical()
                        | Text.H3(evt.Title)
                        | new Badge(evt.Category.ToString()).Variant(GetCategoryVariant(evt.Category))
                        | (Layout.Horizontal().Gap(2)
                            | Icons.CalendarDays.ToIcon().Small()
                            | Text.Muted(evt.DateTime.ToString("MMM dd, yyyy h:mm tt")))
                        | (Layout.Horizontal().Gap(2)
                            | Icons.MapPin.ToIcon().Small()
                            | Text.Muted(evt.Location))
                        | new Progress(percentage).Goal($"{evt.Attendees.Count}/{evt.Capacity} spots")
                        | new Button("View Details").Outline()
                            .OnClick(() => blades.Push(this, new EventDetailView(evt.Id), "Event Details"))
                );

                grid = grid | card;
            }
            content = grid;
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | headerRow
                | filterRow
                | content
                | sheet);
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
