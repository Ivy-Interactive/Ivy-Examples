namespace NodaTimeExample;

[App(icon: Icons.Clock, title: "NodaTime")]
public class NodaTimeApp : ViewBase
{
    public override object? Build()
    {
        var tzState = this.UseState<string>();
        var timeState = this.UseState<object?>(Text.Muted("Select a timezone to see results..."));

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
                Layout.Vertical()
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

        // Update time whenever timezone selection changes
        UseEffect(() => { UpdateTime(tzState.Value); }, tzState);

        // Build SearchSelect options from all available timezones
        var tzOptions = DateTimeZoneProviders.Tzdb.Ids
            .Select(id => new Option<string>(id, id))
            .ToList();

        // Ivy SearchSelect
        var tzSelect = tzState
            .ToSelectInput(tzOptions)
            .Variant(SelectInputs.Select)
            .Placeholder("Search timezone...")
            .WithLabel("Timezone");

        // Final UI layout
        return Layout.Center()
            | (new Card(
                Layout.Vertical()
                | Text.H2("Timezone Demo")
                | Text.Muted("Pick a timezone below — the app will instantly show UTC and local times using NodaTime.")
                | tzSelect
                // Display the structured time output
                | new Card(timeState.Value)
            )
            .Width(Size.Units(120).Max(600)));
    }
}
