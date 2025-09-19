namespace TimeZoneConverterDemo.Apps;

using TimeZoneConverter;

[App(icon: Icons.Clock, title: "Time Zone Converter")]
public class TimeZoneConverterDemoApp : ViewBase
{
    public override object? Build()
    {
        var ianaZoneState = UseState("America/New_York");
        var windowsZoneState = UseState("Eastern Standard Time");
        var railsZoneState = UseState("Eastern Time (US & Canada)");
        var currentTimeState = UseState(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Initialize time zone lists
        var ianaZones = TZConvert.KnownIanaTimeZoneNames.OrderBy(x => x).ToList().ToOptions();
        var windowsZones = TZConvert.KnownWindowsTimeZoneIds.OrderBy(x => x).ToList().ToOptions();
        var railsZones = TZConvert.KnownRailsTimeZoneNames.OrderBy(x => x).ToList().ToOptions();

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

        // Helper function for conversions
        var tryConvert = (Func<string> conversion) =>
        {
            try
            {
                return conversion();
            }
            catch
            {
                return "Not available";
            }
        };

        var ianaSelect = new SelectInput<string>(
            value: ianaZoneState.Value,
            onChange: e =>
            {
                ianaZoneState.Set(e.Value);
                try
                {
                    windowsZoneState.Set(TZConvert.IanaToWindows(e.Value));
                    var railsZones = TZConvert.IanaToRails(e.Value);
                    if (railsZones.Any())
                        railsZoneState.Set(railsZones[0]);
                }
                catch { }
                updateTime();
            },
            ianaZones);

        var windowsSelect = new SelectInput<string>(
            value: windowsZoneState.Value,
            onChange: e =>
            {
                windowsZoneState.Set(e.Value);
                try
                {
                    ianaZoneState.Set(TZConvert.WindowsToIana(e.Value));
                    var railsZones = TZConvert.WindowsToRails(e.Value);
                    if (railsZones.Any())
                        railsZoneState.Set(railsZones[0]);
                }
                catch { }
                updateTime();
            },
            windowsZones);

        var railsSelect = new SelectInput<string>(
            value: railsZoneState.Value,
            onChange: e =>
            {
                railsZoneState.Set(e.Value);
                try
                {
                    ianaZoneState.Set(TZConvert.RailsToIana(e.Value));
                    windowsZoneState.Set(TZConvert.RailsToWindows(e.Value));
                }
                catch { }
                updateTime();
            },
            railsZones);

        return Layout.Vertical()
            | Text.Block("Time Zone Converter")
            | Text.Block($"Current Time: {currentTimeState.Value}")
            | Text.Block("IANA Time Zone")
            | ianaSelect
            | Text.Block("Windows Time Zone")
            | windowsSelect
            | Text.Block("Rails Time Zone")
            | railsSelect
            | Text.Block("Conversion Results:")
            | Text.Block($"IANA → Windows: {tryConvert(() => TZConvert.IanaToWindows(ianaZoneState.Value))}")
            | Text.Block($"IANA → Rails: {tryConvert(() => string.Join(", ", TZConvert.IanaToRails(ianaZoneState.Value)))}")
            | Text.Block($"Windows → IANA: {tryConvert(() => TZConvert.WindowsToIana(windowsZoneState.Value))}")
            | Text.Block($"Windows → Rails: {tryConvert(() => string.Join(", ", TZConvert.WindowsToRails(windowsZoneState.Value)))}")
            | Text.Block($"Rails → IANA: {tryConvert(() => TZConvert.RailsToIana(railsZoneState.Value))}")
            | Text.Block($"Rails → Windows: {tryConvert(() => TZConvert.RailsToWindows(railsZoneState.Value))}")
        ;
    }
}