namespace NodaTimeExample;

/// <summary>
/// Virender Thakur's Notes:
/// Ivy demo application showing integration with NodaTime.
/// 
/// Features:
/// - Interactive timezone selection using Ivy's DropDownMenu
/// - Display of current UTC and local time for the selected zone
/// - Demonstrates how to connect a third-party .NET library (NodaTime) with Ivy's state + UI
/// </summary>
[App(icon: Icons.Clock, title: "NodaTime Demo")]
public class NodaTimeApp : ViewBase
{
    public override object? Build()
    {

        // State variables:
        // tzState -> holds the currently selected timezone
        // timeState -> stores the formatted result UI fragment
        var tzState = this.UseState<string>("Europe/London");
        var timeState = this.UseState<object?>(
     Text.Muted("Select a timezone to see results...")
 );

        // Helper function: updates the UI whenever a timezone is selected
        void UpdateTime(string tzId)
        {
            // NodaTime provides robust timezone handling (instead of DateTime.Now)
            var tz = DateTimeZoneProviders.Tzdb[tzId];

            // Current UTC instant
            var utcNow = SystemClock.Instance.GetCurrentInstant();

            // Convert instant to local time in selected zone
            var zonedNow = utcNow.InZone(tz);

            // Use NodaTime patterns for nice formatting
            var pattern = LocalDateTimePattern.CreateWithInvariantCulture("dddd, MMM dd yyyy HH:mm:ss");

            // Update Ivy state -> structured UI
            timeState.Value =
                Layout.Vertical().Gap(4).Padding(4)
                    | (Layout.Horizontal().Gap(4)
                        | Icons.Globe
                        | Text.Block($"Selected Timezone: {tzId}")
                      )
                    | (Layout.Horizontal().Gap(4)
                        | Icons.Clock
                        | Text.Block($"UTC Now: {utcNow}")
                      )
                    | (Layout.Horizontal().Gap(4)
                        | Icons.Calendar
                        | Text.Block($"Local Time: {pattern.Format(zonedNow.LocalDateTime)}")
                      );
        }

        // Run once on component load to initialize default display
        UseEffect(() => { UpdateTime(tzState.Value); });

        // Build dropdown menu items from all available timezones
        var tzItems = DateTimeZoneProviders.Tzdb.Ids
            .Select(id => MenuItem.Default(id)
                // HandleSelect is Ivy's way of reacting to user selection
                .HandleSelect(() =>
                {
                    tzState.Value = id;
                    UpdateTime(id);   // recompute instantly -> makes it interactive
                }))
            .ToArray();

        // Trigger button for the dropdown
        var triggerButton = new Button($"Timezone: {tzState.Value}")
            .Icon(Icons.Globe);

        // Ivy DropDownMenu widget
        var dropdown = new DropDownMenu(DropDownMenu.DefaultSelectHandler(), triggerButton)
            .Items(tzItems);

        // Final UI layout
        return Layout.Center()
            | (new Card(
                Layout.Vertical().Gap(12).Padding(3)
                | Text.H2("Demonstration of Timezone Handling with NodaTime")
                | Text.Block("Pick a timezone below — the app will instantly show UTC and local times using NodaTime.")
                | dropdown
                | new Separator()
                // Display the structured time output
                | timeState.Value
            )
            .Width(Size.Units(120).Max(600)));
    }
}
