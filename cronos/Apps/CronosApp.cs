using Cronos;

namespace CronosExample.Apps;

[App(icon: Icons.TimerReset, title: "Cronos")]
public class CronosApp : ViewBase
{
    private enum CronScheduleType
    {
        EveryMinute,
        Every5Minutes,
        DailyAt9AM,
        WeekdaysAtNoon,
        MonthlyFirstDay9AM,
        Every30Seconds,
        ComplexExample
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // Basic cron expression
        var inputCronExpression = UseState((string?)null);

        // Time zones
        var timeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new { Id = tz.Id, Name = tz.DisplayName })
            .ToList();
        var inputTimeZone = UseState(timeZones[0].Id);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(inputTimeZone.Value);

        // Results
        var nextOccurrence = UseState<DateTime?>();

        // Options for Cron format
        var includeSeconds = UseState(false);
        var cronFormat = includeSeconds.Value ? CronFormat.IncludeSeconds : CronFormat.Standard;

        // Selected schedule type
        var selectedSchedule = UseState<CronScheduleType?>(default(CronScheduleType?));

        // Formatting
        var dateFormat = includeSeconds.Value ? "dd.MM.yyyy HH:mm:ss zzz" : "dd.MM.yyyy HH:mm zzz";
        var dateString = nextOccurrence.Value?.ToString(dateFormat) ?? "—";

        void TryParseCron()
        {
            if (!string.IsNullOrWhiteSpace(inputCronExpression.Value))
            {
                try
                {
                    var cronExpression = CronExpression.Parse(inputCronExpression.Value, cronFormat);
                    var next = cronExpression.GetNextOccurrence(DateTimeOffset.Now, timeZone);
                    nextOccurrence.Value = next?.DateTime;
                }
                catch (CronFormatException ex)
                {
                    client.Toast($"Invalid CRON: {ex.Message}");
                }
            }
        }
        
        string GetCronExpression(CronScheduleType scheduleType)
        {
            return scheduleType switch
            {
                CronScheduleType.EveryMinute => includeSeconds.Value ? "0 * * * * *" : "* * * * *",
                CronScheduleType.Every5Minutes => includeSeconds.Value ? "0 */5 * * * *" : "*/5 * * * *",
                CronScheduleType.DailyAt9AM => includeSeconds.Value ? "0 0 9 * * *" : "0 9 * * *",
                CronScheduleType.WeekdaysAtNoon => includeSeconds.Value ? "0 0 12 * * 1-5" : "0 12 * * 1-5",
                CronScheduleType.MonthlyFirstDay9AM => includeSeconds.Value ? "0 0 9 1 * *" : "0 9 1 * *",
                CronScheduleType.Every30Seconds => "*/30 * * * * *",
                CronScheduleType.ComplexExample => includeSeconds.Value ? "0 15,45 8-17 * * 1-5" : "15,45 8-17 * * 1-5",
                _ => ""
            };
        }

        string GetDescription(CronScheduleType scheduleType)
        {
            return scheduleType switch
            {
                CronScheduleType.EveryMinute => "Every minute",
                CronScheduleType.Every5Minutes => "Every 5 minutes",
                CronScheduleType.DailyAt9AM => "Daily at 09:00",
                CronScheduleType.WeekdaysAtNoon => "Weekdays at noon",
                CronScheduleType.MonthlyFirstDay9AM => "Monthly on 1st at 09:00",
                CronScheduleType.Every30Seconds => "Every 30 seconds",
                CronScheduleType.ComplexExample => "15 and 45 min past hour, 8-17h, Mon-Fri",
                _ => ""
            };
        }

        var scheduleOptions = typeof(CronScheduleType).ToOptions();

        // Automatically apply selected schedule
        UseEffect(() =>
        {
            if (selectedSchedule.Value.HasValue)
            {
                inputCronExpression.Value = GetCronExpression(selectedSchedule.Value.Value);
                client.Toast($"Applied: {GetDescription(selectedSchedule.Value.Value)}");
            }
        }, selectedSchedule);

        return Layout.Vertical(
                Text.H1("Cronos"),
                
                inputTimeZone
                    .ToSelectInput(timeZones
                        .Select(tz => new Option<string>(tz.Name, tz.Id))
                        .ToList())
                    .WithLabel("Select a time zone"),
                
                inputCronExpression
                    .ToTextInput()
                    .Placeholder("Cron expression (e.g. \"*/5 * * * *\")")
                    .WithLabel("Enter a cron expression"),
                
                includeSeconds
                    .ToBoolInput(variant: BoolInputs.Checkbox)
                    .Label("Include seconds"),
                    
                    new Button("Try parse", onClick: TryParseCron)
                        .Disabled(string.IsNullOrWhiteSpace(inputCronExpression.Value)),
                
                // Predefined examples
                Text.H3("Predefined Examples:"),
                selectedSchedule
                    .ToSelectInput(scheduleOptions)
                    .Placeholder("Choose a schedule template...")
                    .WithLabel("Select schedule type"),
                
                // Results
                Text.H3($"Next occurrence: {dateString}")
            ).Width(Size.Units(170))
            .Padding(20, 0, 20, 20);
    }
}