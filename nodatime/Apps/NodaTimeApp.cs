using Ivy.Shared;
using NodaTime;
using NodaTime.Text;

namespace Ivy.nodatime.Sample.Apps;

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
        // timeState -> holds the formatted result string shown to the user
        var tzState = this.UseState<string>("Europe/London");
        var timeState = this.UseState<string>("");

        // Helper function: updates the display string whenever a timezone is selected
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

            // Update Ivy state -> triggers UI re-render
            timeState.Value =
                $"🌍 Selected Timezone: {tzId}\n" +
                $"🕒 UTC Now: {utcNow}\n" +
                $"🕓 Local Time: {pattern.Format(zonedNow.LocalDateTime)}";
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
                Layout.Vertical().Gap(10).Padding(3)
                | Text.H2("Demonstation of timezone using NodaTime")
                | Text.Block("Pick a timezone below — time updates instantly using NodaTime.")
                | dropdown
                | new Separator()
                // This is where NodaTime results show up
                | Text.Block(timeState.Value)
            )
            .Width(Size.Units(120).Max(600)));
    }
}
