namespace NodaTimeExample;

[App(icon: Icons.Clock, title: "NodaTime")]
public class NodaTimeApp : ViewBase
{
    public override object? Build()
    {
        var tzState = this.UseState<string>();
        var timeState = this.UseState<object?>(Text.Muted("Select a timezone to see results..."));
        
        // Update time whenever timezone selection changes
        UseEffect(() => { UpdateTime(tzState.Value); }, tzState);
        
        // Helper function: updates the UI whenever a timezone is selected
        void UpdateTime(string tzId)
        {
            if (string.IsNullOrEmpty(tzId))
            {
                return;
            }

            try
            {
                // NodaTime provides robust timezone handling (instead of DateTime.Now)
                var tz = DateTimeZoneProviders.Tzdb[tzId];

                // Current UTC instant
                var utcNow = SystemClock.Instance.GetCurrentInstant();

                // Convert instant to local time in selected zone
                var zonedNow = utcNow.InZone(tz);

                // Get system's local timezone
                var systemTz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
                var systemZonedNow = utcNow.InZone(systemTz);

                // Use NodaTime patterns for nice formatting with current culture
                var pattern = LocalDateTimePattern.CreateWithCurrentCulture("dddd, MMM dd yyyy HH:mm:ss");

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
                            | Text.Block($"Selected Zone Time: {pattern.Format(zonedNow.LocalDateTime)}")
                          )
                        | (Layout.Horizontal().Gap(4)
                            | Icons.Monitor
                            | Text.Block($"System Local Time: {pattern.Format(systemZonedNow.LocalDateTime)} ({systemTz.Id})")
                          );
            }
            catch
            {
                // Invalid timezone - show error message
                timeState.Value = Text.Muted("Invalid timezone selected");
            }
        }

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
                | new Spacer()
				| Text.Small("This demo uses NodaTime to handle timezones and display UTC and local times.")
			    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [NodaTime](https://github.com/nodatime/nodatime)")
            )
            .Width(Size.Fit().Min(100).Max(600)));
    }
}
