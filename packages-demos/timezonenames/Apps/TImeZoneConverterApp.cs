namespace TimeZoneConverterExample;

using TimeZoneConverter;

[App(icon: Icons.Clock, title: "TimeZoneNames")]
public class TimeZoneConverterApp : ViewBase
{
    public override object? Build()
    {
        var ianaZoneState = UseState<string>("America/New_York");
        var windowsZoneState = UseState<string>("Eastern Standard Time");
        var railsZoneState = UseState<string>("Eastern Time (US & Canada)");
        var currentTimeState = UseState(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Search type selector
        var searchTypeState = UseState<string>("IANA");

        // Search term states
        var ianaSearchTerm = UseState<string>("");
        var windowsSearchTerm = UseState<string>("");
        var railsSearchTerm = UseState<string>("");

        // Initialize time zone lists
        var allIanaZones = TZConvert.KnownIanaTimeZoneNames.OrderBy(x => x).ToArray();
        var allWindowsZones = TZConvert.KnownWindowsTimeZoneIds.OrderBy(x => x).ToArray();
        var allRailsZones = TZConvert.KnownRailsTimeZoneNames.OrderBy(x => x).ToArray();

        // Filtered zones based on search
        var filteredIanaZones = string.IsNullOrEmpty(ianaSearchTerm.Value)
            ? allIanaZones
            : allIanaZones
                .Where(z => z.Contains(ianaSearchTerm.Value, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        var filteredWindowsZones = string.IsNullOrEmpty(windowsSearchTerm.Value)
            ? allWindowsZones
            : allWindowsZones
                .Where(z => z.Contains(windowsSearchTerm.Value, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        var filteredRailsZones = string.IsNullOrEmpty(railsSearchTerm.Value)
            ? allRailsZones
            : allRailsZones
                .Where(z => z.Contains(railsSearchTerm.Value, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        // Update time on mount and when IANA zone changes
        UseEffect(() =>
        {
            try
            {
                var timeZoneInfo = TZConvert.GetTimeZoneInfo(ianaZoneState.Value);
                currentTimeState.Set(TimeZoneInfo.ConvertTime(DateTime.Now, timeZoneInfo)
                    .ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                currentTimeState.Set("Invalid time zone");
            }
        }, [ianaZoneState]);

        // Update time function
        var updateTime = () =>
        {
            try
            {
                var timeZoneInfo = TZConvert.GetTimeZoneInfo(ianaZoneState.Value);
                currentTimeState.Set(TimeZoneInfo.ConvertTime(DateTime.Now, timeZoneInfo)
                    .ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                currentTimeState.Set("Invalid time zone");
            }
        };

        // Create searchable lists
        var ianaListItems = filteredIanaZones.Select(zone => new ListItem(zone, onClick: _ =>
        {
            ianaZoneState.Set(zone);
            try
            {
                var windowsZone = TZConvert.IanaToWindows(zone);
                windowsZoneState.Set(windowsZone);
                windowsSearchTerm.Set(windowsZone);
                
                var railsZones = TZConvert.IanaToRails(zone);
                if (railsZones.Any())
                {
                    railsZoneState.Set(railsZones[0]);
                    railsSearchTerm.Set(railsZones[0]);
                }
            }
            catch { }
            updateTime();
        }));

        var windowsListItems = filteredWindowsZones.Select(zone => new ListItem(zone, onClick: _ =>
        {
            windowsZoneState.Set(zone);
            try
            {
                var ianaZone = TZConvert.WindowsToIana(zone);
                ianaZoneState.Set(ianaZone);
                ianaSearchTerm.Set(ianaZone);
                
                var railsZones = TZConvert.WindowsToRails(zone);
                if (railsZones.Any())
                {
                    railsZoneState.Set(railsZones[0]);
                    railsSearchTerm.Set(railsZones[0]);
                }
            }
            catch { }
            updateTime();
        }));

        var railsListItems = filteredRailsZones.Select(zone => new ListItem(zone, onClick: _ =>
        {
            railsZoneState.Set(zone);
            try
            {
                var ianaZone = TZConvert.RailsToIana(zone);
                ianaZoneState.Set(ianaZone);
                ianaSearchTerm.Set(ianaZone);
                
                var windowsZone = TZConvert.RailsToWindows(zone);
                windowsZoneState.Set(windowsZone);
                windowsSearchTerm.Set(windowsZone);
            }
            catch { }
            updateTime();
        }));

        // Build search content based on selected type
        object BuildSearchContent()
        {
            return searchTypeState.Value switch
            {
                "IANA" => Layout.Vertical().Gap(4)
                    | ianaSearchTerm.ToTextInput(ianaZoneState.Value)
                        .Variant(TextInputs.Search)
                        .WithField()
                        .Label("Search IANA Time Zones")
                    | Layout.Vertical(new List(ianaListItems.ToArray())).Height(Size.Units(70)),
                
                "Windows" => Layout.Vertical().Gap(4)
                    | windowsSearchTerm.ToTextInput(windowsZoneState.Value)
                        .Variant(TextInputs.Search)
                        .WithField()
                        .Label("Search Windows Time Zones")
                    | Layout.Vertical(new List(windowsListItems.ToArray())).Height(Size.Units(70)),
                
                "Rails" => Layout.Vertical().Gap(4)
                    | railsSearchTerm.ToTextInput(railsZoneState.Value)
                        .Variant(TextInputs.Search)
                        .WithField()
                        .Label("Search Rails Time Zones")
                    | Layout.Vertical(new List(railsListItems.ToArray())).Height(Size.Units(70)),
                
                _ => Layout.Vertical().Gap(4)
                    | Text.Muted("Select a time zone type")
            };
        }

        // Left card - Search
        var searchCard = new Card(
            Layout.Vertical().Gap(4)
                | Text.H3("Search Time Zone")
                | Text.Muted("Select a time zone type and search for a specific zone.")
                | searchTypeState.ToSelectInput(new[] { "IANA", "Windows", "Rails" }.ToOptions())
                    .WithField()
                    .Label("Time Zone Type")
                | BuildSearchContent()
        ).Width(Size.Fraction(0.5f));

        // Right card - Results
        var resultCard = new Card(
            Layout.Vertical().Gap(4)
                | Text.H3("Current Time & Selected Zones")
                | Text.Muted("Current time in the selected time zone and all format conversions.")
                | new
                {
                    CurrentTime = currentTimeState.Value,
                    IANA = ianaZoneState.Value,
                    Windows = windowsZoneState.Value,
                    Rails = railsZoneState.Value
                }.ToDetails()
                ).Width(Size.Fraction(0.5f));

        return Layout.Vertical().Gap(4)
            | (Layout.Vertical().Width(Size.Fraction(0.7f))
            | Text.H1("TimeZoneNames")
            | Text.Muted("A simple library that provides localized time zone names using CLDR and TZDB sources. " +
                        "This demo uses TimeZoneConverter to convert between IANA, Windows, and Rails time zone formats. " +
                        "Select a time zone type, search and click on any time zone. All three formats will be synchronized automatically. ")
                        )
            | (Layout.Horizontal().Gap(4)
                | searchCard
                | resultCard)
            | Text.Small("This demo uses TimeZoneNames library to convert between IANA, Windows, and Rails time zone formats.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [TimeZoneNames](https://github.com/mattjohnsonpint/TimeZoneNames)")
        
        ;
    }
}