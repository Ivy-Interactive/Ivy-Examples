namespace TimeZoneConverterExample;

using TimeZoneConverter;

[App(icon: Icons.Clock, title: "Time Zone Converter")]
public class TimeZoneConverterApp : ViewBase
{
    public override object? Build()
    {
        var ianaZoneState = UseState("America/New_York");
        var windowsZoneState = UseState("Eastern Standard Time");
        var railsZoneState = UseState("Eastern Time (US & Canada)");
        var currentTimeState = UseState(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Initialize time zone lists
        var allIanaZones = TZConvert.KnownIanaTimeZoneNames.OrderBy(x => x).ToArray();
        var allWindowsZones = TZConvert.KnownWindowsTimeZoneIds.OrderBy(x => x).ToArray();
        var allRailsZones = TZConvert.KnownRailsTimeZoneNames.OrderBy(x => x).ToArray();

        // Search states
        var ianaSearchTerm = UseState("");
        var windowsSearchTerm = UseState("");
        var railsSearchTerm = UseState("");

        // Filtered lists
        var filteredIanaZones = UseState(allIanaZones);
        var filteredWindowsZones = UseState(allWindowsZones);
        var filteredRailsZones = UseState(allRailsZones);

        // Filter effects
        UseEffect(() =>
        {
            var filtered = allIanaZones.Where(zone =>
                zone.Contains(ianaSearchTerm.Value, StringComparison.OrdinalIgnoreCase)).ToArray();
            filteredIanaZones.Set(filtered);
        }, [ianaSearchTerm]);

        UseEffect(() =>
        {
            var filtered = allWindowsZones.Where(zone =>
                zone.Contains(windowsSearchTerm.Value, StringComparison.OrdinalIgnoreCase)).ToArray();
            filteredWindowsZones.Set(filtered);
        }, [windowsSearchTerm]);

        UseEffect(() =>
        {
            var filtered = allRailsZones.Where(zone =>
                zone.Contains(railsSearchTerm.Value, StringComparison.OrdinalIgnoreCase)).ToArray();
            filteredRailsZones.Set(filtered);
        }, [railsSearchTerm]);

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
        var ianaListItems = filteredIanaZones.Value.Select(zone => new ListItem(zone, onClick: () =>
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

        var windowsListItems = filteredWindowsZones.Value.Select(zone => new ListItem(zone, onClick: () =>
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

        var railsListItems = filteredRailsZones.Value.Select(zone => new ListItem(zone, onClick: () =>
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

        return Layout.Vertical().Gap(2)
            | Text.Block("Time Zone Converter")
            | Text.Block($"Current Time: {currentTimeState.Value}")
            | Text.Block($"Selected: IANA: {ianaZoneState.Value}   |   Windows: {windowsZoneState.Value}   |   Rails: {railsZoneState.Value}")
            | (Layout.Horizontal().Gap(2)
                | (Layout.Vertical().Gap(1)
                    | Text.Block("IANA Time Zone")
                    | ianaSearchTerm.ToSearchInput().Placeholder("Search IANA zones...")
                    | new List(ianaListItems).Height(400))
                | (Layout.Vertical().Gap(1)
                    | Text.Block("Windows Time Zone")
                    | windowsSearchTerm.ToSearchInput().Placeholder("Search Windows zones...")
                    | new List(windowsListItems).Height(400))
                | (Layout.Vertical().Gap(1)
                    | Text.Block("Rails Time Zone")
                    | railsSearchTerm.ToSearchInput().Placeholder("Search Rails zones...")
                    | new List(railsListItems).Height(400)))
        ;
    }
}