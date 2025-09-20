using Cronos;

namespace CronosExample.Apps;

[App(icon: Icons.TimerReset, title: "Cronos")]
public class CronosApp : ViewBase
{
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
        
        void SetPredefinedCron(string cronExpr, string description)
        {
            inputCronExpression.Value = cronExpr;
            client.Toast($"Set: {description}");
        }

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
                Layout.Horizontal(
                    new Button("Every minute", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 * * * * *" 
                            : "* * * * *", "Every minute")),
                    
                    new Button("Every 5 min", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 */5 * * * *" 
                            : "*/5 * * * *", "Every 5 minutes")),
                    
                    new Button("Daily at 09:00", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 0 9 * * *" 
                            : "0 9 * * *", "Daily at 09:00")),
                    
                    new Button("Weekdays at noon", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 0 12 * * 1-5" 
                            : "0 12 * * 1-5", "Weekdays at noon")),
                    
                    new Button("Monthly (1st, 09:00)", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 0 9 1 * *" 
                            : "0 9 1 * *", "Monthly on 1st at 09:00")),
                    
                    new Button("Every 30 sec", 
                            onClick: () => SetPredefinedCron("*/30 * * * * *", "Every 30 seconds"))
                        .Disabled(!includeSeconds.Value),
                    
                    new Button("Complex example", 
                        onClick: () => SetPredefinedCron(includeSeconds.Value 
                            ? "0 15,45 8-17 * * 1-5" 
                            : "15,45 8-17 * * 1-5", "15 and 45 min past hour, 8-17h, Mon-Fri"))
                ).Wrap(),
                
                // Results
                Text.H3($"Next occurrence: {nextOccurrence.Value?.ToString("dd.MM.yyyy HH:mm:ss zzz") ?? "—"}")
            ).Width(Size.Units(170))
            .Padding(20, 0, 20, 20);
    }
}