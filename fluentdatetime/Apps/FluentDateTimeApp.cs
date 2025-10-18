namespace FluentDateTimeExample;

[App(icon: Icons.Calendar, title: "FluentDateTime")]
public class FluentDateTimeApp : ViewBase
{
    public override object? Build()
    {
        var operation = UseState<string?>(() => "Add");
        var unit = UseState<string?>(() => "Days");
        var amount = UseState<double>(() => 30);
        var baseDateTime = UseState<DateTime>(() => DateTime.Now);
        var showResult = UseState<bool>(() => false);

        var amountInt = (int)Math.Abs(amount.Value);
        var resultDate = ComputeDate(baseDateTime.Value, operation.Value, unit.Value, amountInt);

        return Layout.Vertical().Align(Align.TopCenter)
               | (new Card(
                   Layout.Vertical().Gap(4)
                   | Text.H2("Date Calculator")
                   | Text.Muted("Add or subtract an amount of time from a specific date and time.")
                   | Layout.Vertical()
                       | Text.Label("Base Date & Time")
                       | baseDateTime.ToDateTimeInput()
                           .Variant(DateTimeInputs.DateTime)
                       | Text.Label("Operation")
                       | operation.ToSelectInput(Operations).Variant(SelectInputs.Select)
                       | Text.Label("Time Unit")
                       | unit.ToSelectInput(TimeUnits).Variant(SelectInputs.Select)
                       | Text.Label("Amount")
                       | new NumberInput<double>(amount)
                         .Min(1)
                         .Max(9999)
                       | (Layout.Horizontal().Gap(4)
                         | new Button("Calculate")
                             .HandleClick(() => showResult.Set(true))
                         | new Button("Clear")
                             .Secondary()
                             .HandleClick(() => {
                                 showResult.Set(false);
                                 operation.Set("Add");
                                 unit.Set("Days");
                                 amount.Set(30);
                                 baseDateTime.Set(DateTime.Now);
                             }))
                          | (showResult.Value ? 
                              new Card(
                                  Layout.Vertical()
                                  | Text.Large("Result")
                                  | Text.Markdown($"**Computed date**: `{resultDate:yyyy-MM-dd HH:mm}`")
                                  | Text.Markdown($"**Difference**: `{(int)(resultDate - baseDateTime.Value).TotalDays} days`")
                              ) : 
                              Text.Muted("Click 'Calculate' to see the result")
                            )
                         | new Spacer()
                         | Text.Small("This demo uses the FluentDateTime NuGet package for cleaner DateTime operations.")
                         | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [FluentDateTime](https://github.com/FluentDateTime/FluentDateTime)")
                  ).Width(Size.Fraction(0.4f))
               );
    }

    private static readonly Option<string?>[] Operations =
    [
        new Option<string?>("Add", "Add"),
        new Option<string?>("Subtract", "Subtract"),
    ];

    private static readonly Option<string?>[] TimeUnits =
    [
        new Option<string?>("Minutes", "Minutes"),
        new Option<string?>("Hours", "Hours"),
        new Option<string?>("Days", "Days"),
        new Option<string?>("Weeks", "Weeks"),
        new Option<string?>("Months", "Months"),
        new Option<string?>("Years", "Years"),
    ];

    

    private static DateTime ComputeDate(DateTime baseDate, string? operation, string? unit, int amount)
    {
        var isSubtract = string.Equals(operation, "Subtract", StringComparison.OrdinalIgnoreCase);
    if (unit == "Months") return baseDate.AddMonths(isSubtract ? -amount : amount);
        if (unit == "Years") return baseDate.AddYears(isSubtract ? -amount : amount);

        var span = unit switch
        {
            "Minutes" => TimeSpan.FromMinutes(amount),
            "Hours" => TimeSpan.FromHours(amount),
            "Days" => TimeSpan.FromDays(amount),
            "Weeks" => TimeSpan.FromDays(amount * 7),
            _ => TimeSpan.Zero
        };

        if (span == TimeSpan.Zero) return baseDate;
        return isSubtract ? span.Before(baseDate) : span.From(baseDate);
    }
}


