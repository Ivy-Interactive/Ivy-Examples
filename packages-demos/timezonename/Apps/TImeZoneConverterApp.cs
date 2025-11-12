namespace TimeZoneConverterExample;

using TimeZoneConverter;

[App(icon: Icons.Clock, title: "Time Zone Converter")]
public class TimeZoneConverterApp : ViewBase
{
    public override object? Build()
    {
        var ianaZoneState = UseState<string?>(default(string?));
        var windowsZoneState = UseState<string?>(default(string?));
        var railsZoneState = UseState<string?>(default(string?));
        var currentTimeState = UseState(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Initialize time zone lists
        var allIanaZones = TZConvert.KnownIanaTimeZoneNames.OrderBy(x => x).ToArray();
        var allWindowsZones = TZConvert.KnownWindowsTimeZoneIds.OrderBy(x => x).ToArray();
        var allRailsZones = TZConvert.KnownRailsTimeZoneNames.OrderBy(x => x).ToArray();

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

        // Async query functions for each time zone type
        Task<Option<string>[]> QueryIanaZones(string query)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(query))
                    return allIanaZones.Take(20).Select(z => new Option<string>(z)).ToArray();

                return allIanaZones
                    .Where(z => z.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(z => new Option<string>(z))
                    .ToArray();
            });
        }

        Task<Option<string>[]> QueryWindowsZones(string query)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(query))
                    return allWindowsZones.Take(20).Select(z => new Option<string>(z)).ToArray();

                return allWindowsZones
                    .Where(z => z.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(z => new Option<string>(z))
                    .ToArray();
            });
        }

        Task<Option<string>[]> QueryRailsZones(string query)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(query))
                    return allRailsZones.Take(20).Select(z => new Option<string>(z)).ToArray();

                return allRailsZones
                    .Where(z => z.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(z => new Option<string>(z))
                    .ToArray();
            });
        }

        // Lookup functions
        Task<Option<string>?> LookupIanaZone(string zone)
        {
            if (string.IsNullOrEmpty(zone)) 
                return Task.FromResult<Option<string>?>(null);
            return Task.FromResult<Option<string>?>(new Option<string>(zone));
        }

        Task<Option<string>?> LookupWindowsZone(string zone)
        {
            if (string.IsNullOrEmpty(zone)) 
                return Task.FromResult<Option<string>?>(null);
            return Task.FromResult<Option<string>?>(new Option<string>(zone));
        }

        Task<Option<string>?> LookupRailsZone(string zone)
        {
            if (string.IsNullOrEmpty(zone)) 
                return Task.FromResult<Option<string>?>(null);
            return Task.FromResult<Option<string>?>(new Option<string>(zone));
        }

        // Handle IANA zone selection
        UseEffect(() =>
        {
            try
            {
                var windowsZone = TZConvert.IanaToWindows(ianaZoneState.Value);
                windowsZoneState.Set(windowsZone);
                
                var railsZones = TZConvert.IanaToRails(ianaZoneState.Value);
                if (railsZones.Any())
                {
                    railsZoneState.Set(railsZones[0]);
                }
            }
            catch { }
            updateTime();
        }, [ianaZoneState]);

        // Handle Windows zone selection
        UseEffect(() =>
        {
            try
            {
                var ianaZone = TZConvert.WindowsToIana(windowsZoneState.Value);
                if (ianaZone != ianaZoneState.Value)
                {
                    ianaZoneState.Set(ianaZone);
                }
            }
            catch { }
        }, [windowsZoneState]);

        // Handle Rails zone selection
        UseEffect(() =>
        {
            try
            {
                var ianaZone = TZConvert.RailsToIana(railsZoneState.Value);
                if (ianaZone != ianaZoneState.Value)
                {
                    ianaZoneState.Set(ianaZone);
                }
            }
            catch { }
        }, [railsZoneState]);

        // Left card - User input data
        var leftCardContent = Layout.Vertical().Gap(4)
            | Text.H3("Selected Time Zones")
            | Text.Muted("Select a time zone in any format (IANA, Windows, or Rails). All formats will be automatically synchronized.")
            | ianaZoneState.ToAsyncSelectInput(QueryIanaZones, LookupIanaZone, "Search IANA zones...")
                .WithField()
                .Label("IANA Time Zone")
            | windowsZoneState.ToAsyncSelectInput(QueryWindowsZones, LookupWindowsZone, "Search Windows zones...")
                .WithField()
                .Label("Windows Time Zone")
            | railsZoneState.ToAsyncSelectInput(QueryRailsZones, LookupRailsZone, "Search Rails zones...")
                .WithField()
                .Label("Rails Time Zone");

        var leftCard = new Card(leftCardContent).Width(Size.Fraction(0.5f));

        // Right card - Result
        var rightCardContent = Layout.Vertical().Gap(4)
            | Text.H3("Current Time")
            | Text.Muted("Displays the current time in the selected time zone.")
            | new
            {
                CurrentTime = currentTimeState.Value,
                IANA = ianaZoneState.Value ?? "Not selected",
                Windows = windowsZoneState.Value ?? "Not selected",
                Rails = railsZoneState.Value ?? "Not selected"
            }.ToDetails();

        var rightCard = new Card(rightCardContent).Width(Size.Fraction(0.5f));

        return Layout.Vertical()
            | Text.Block("Time Zone Converter")
            | (Layout.Horizontal().Gap(4)
                | leftCard
                | rightCard)
        ;
    }
}