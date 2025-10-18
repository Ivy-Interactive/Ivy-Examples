namespace FluentDateTimeExample;

[App(icon: Icons.Calendar, title: "FluentDateTime")]
public class FluentDateTimeApp : ViewBase
{
    public override object? Build()
    {
        var operation = UseState<string?>(() => "Add");
        var unit = UseState<string?>(() => "Days");
        var amountText = UseState<string>(() => "30");

        _ = int.TryParse(amountText.Value, out var amountInt);
        if (amountInt < 0) amountInt = -amountInt;
        var today = DateTime.Today;
        var resultDate = ComputeDate(today, operation.Value, unit.Value, amountInt);

        return Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(4).Padding(1)
                   | Text.H3("Date Calculator")
                   | Text.Block($"Today: {today:yyyy-MM-dd}")
                   | Text.Block("Add or subtract an amount of time from today.")
                   | new Separator()
                   | Text.H4("Settings")
                    | (Layout.Horizontal().Gap(10)
                      | Text.Block("Operation:")
                      | new Button(operation.Value ?? "Add").Outline().Width(78)
                          .WithDropDown(
                              Operations
                                  .Select(o => MenuItem.Default(o.Label).HandleSelect(() => operation.Set(o.Label)))
                                  .ToArray()
                          )
                    )
                    | (Layout.Horizontal().Gap(19)
                      | Text.Block("Unit:")
                      | new Button(unit.Value ?? "Days").Outline().Width(78)
                          .WithDropDown(
                              TimeUnits
                                  .Select(o => MenuItem.Default(o.Label).HandleSelect(() => unit.Set(o.Label)))
                                  .ToArray()
                          )
                    )
                   | (Layout.Horizontal().Gap(13).Align(Align.Left)
                     | Text.Block("Amount:")
                     | amountText.ToInput(placeholder: "Enter amount (e.g. 3)")
                   )
                   | new Separator()
                   | Text.H4("Result")
                   | Text.Block($"Computed date: {resultDate:yyyy-MM-dd}")
                 )
                 .Width(Size.Units(120).Max(700))
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


