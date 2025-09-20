namespace FluentDateTimeDemo.Apps;

[App(icon: Icons.Calendar, title: "FluentDateTime Demo")]
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
                   | Text.H2("Date Calculator")
                   | Text.Block("Add or subtract an amount of time from today.")
                   | new Separator()
                   | Text.H3("Settings")
                   | (Layout.Horizontal().Gap(2)
                       | Text.Block("Operation")
                       | new Button(operation.Value ?? "Add").Outline()
                           .WithDropDown(
                               MenuItem.Default("Add").HandleSelect(() => operation.Set("Add")),
                               MenuItem.Default("Subtract").HandleSelect(() => operation.Set("Subtract"))
                           )
                     )
                   | (Layout.Horizontal().Gap(2)
                       | Text.Block("Unit")
                       | new Button(unit.Value ?? "Days").Outline()
                           .WithDropDown(
                               MenuItem.Default("Minutes").HandleSelect(() => unit.Set("Minutes")),
                               MenuItem.Default("Hours").HandleSelect(() => unit.Set("Hours")),
                               MenuItem.Default("Days").HandleSelect(() => unit.Set("Days")),
                               MenuItem.Default("Weeks").HandleSelect(() => unit.Set("Weeks")),
                               MenuItem.Default("Months").HandleSelect(() => unit.Set("Months")),
                               MenuItem.Default("Years").HandleSelect(() => unit.Set("Years"))
                           )
                     )
                   | Text.Block("Amount")
                   | amountText.ToInput(placeholder: "Enter amount (e.g. 3)")
                   | new Separator()
                   | Text.H3("Result")
                   | Text.Block($"Today: {today:yyyy-MM-dd}")
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

    private static AsyncSelectQueryDelegate<T?> QueryFromList<T>(IReadOnlyList<Option<T?>> options)
    {
        return async query => await Task.FromResult(options
            .Where(o => string.IsNullOrEmpty(query) || o.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray());
    }

    private static AsyncSelectLookupDelegate<T?> LookupFromList<T>(IReadOnlyList<Option<T?>> options)
    {
        return async id => await Task.FromResult(options.FirstOrDefault(o => object.Equals(o.Value, id)));
    }

    private static DateTime ComputeDate(DateTime baseDate, string? operation, string? unit, int amount)
    {
        var signed = string.Equals(operation, "Subtract", StringComparison.OrdinalIgnoreCase) ? -amount : amount;
        return unit switch
        {
            "Minutes" => baseDate.AddMinutes(signed),
            "Hours" => baseDate.AddHours(signed),
            "Days" => baseDate.AddDays(signed),
            "Weeks" => baseDate.AddDays(signed * 7),
            "Months" => baseDate.AddMonths(signed),
            "Years" => baseDate.AddYears(signed),
            _ => baseDate
        };
    }
}


