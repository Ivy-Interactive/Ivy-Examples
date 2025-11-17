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

        // Try parse cron
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

        var userCard = new Card(
            Layout.Vertical(
                Text.H3("Cronos"),
                inputTimeZone
                    .ToSelectInput(timeZones
                        .Select(tz => new Option<string>(tz.Name, tz.Id))
                        .ToList())
                    .WithLabel("Select a time zone"),

                inputCronExpression
                    .ToTextInput()
                    .Placeholder("Cron expression (e.g. \"*/5 * * * *\")")
                    .WithLabel("Enter a cron expression"),

                selectedSchedule
                    .ToSelectInput(scheduleOptions)
                    .Placeholder("Choose a schedule template...")
                    .WithLabel("Predefined Examples"),

                includeSeconds
                    .ToBoolInput(variant: BoolInputs.Checkbox)
                    .Label("Include seconds"),

                new Button("Try parse", onClick: TryParseCron)
                    .Disabled(string.IsNullOrWhiteSpace(inputCronExpression.Value))
                    .Icon(Icons.Clock),

                Text.Markdown($"### Next occurrence: `{dateString}`")
            )
        ).Height(Size.Fit().Min(Size.Full()));

        var helpCard = new Card(
            Layout.Vertical()
                | Text.Markdown(
                    "### Quick Guide\n\n" +
                    "1. Select a time zone.\n" +
                    "2. Enter a cron expression or choose a template.\n" +
                    "3. Optionally enable 'Include seconds' for 6-field crons.\n" +
                    "4. Click 'Try parse' to validate and compute the next run.\n\n" +
                    "Notes:\n" +
                    "- Next occurrence is shown for the selected time zone.\n" +
                    "- Common operators: `*` any, `/` every, `-` range, `,` list."
                )
                | new Spacer()
                | Text.Small("This demo uses Cronos for parsing and validating Cron expressions.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Cronos](https://github.com/HangfireIO/Cronos)")
        ).Height(Size.Fit().Min(Size.Full()));

        return Layout.Horizontal().Gap(6)
            | userCard.Width(Size.Fraction(0.60f))
            | helpCard.Width(Size.Fraction(0.40f));
    }
}