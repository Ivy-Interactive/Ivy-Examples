namespace UnitsNetExample;

[App(icon: Icons.Scale, title: "UnitsNet")]
public class UnitsNetApp : ViewBase
{
    public override object? Build()
    {
        var quantity = UseState(() => "Temperature");
        var fromUnit = UseState(() => "DegreeCelsius");
        var toUnit = UseState(() => "DegreeFahrenheit");
        var valueText = UseState(() => "70");

        var result = TryConvert(quantity.Value, fromUnit.Value, toUnit.Value, valueText.Value);

        var header = Layout.Vertical().Gap(1)
                     | Text.H2("UnitsNet Converter")
                     | Text.Muted("Enter quantity and unit names from UnitsNet, e.g. Temperature, DegreeCelsius → DegreeFahrenheit");

        var inputs = Layout.Vertical().Gap(2)
                     | Text.Label("Quantity")
                     | quantity.ToAsyncSelectInput(QueryQuantities(), LookupQuantity(), placeholder: "Select Quantity")
                     | Text.Label("From Unit")
                     | fromUnit.ToAsyncSelectInput(QueryUnits(quantity), LookupUnit(quantity), placeholder: "Select From Unit")
                     | Text.Label("To Unit")
                     | toUnit.ToAsyncSelectInput(QueryUnits(quantity), LookupUnit(quantity), placeholder: "Select To Unit")
                     | Text.Label("Value")
                     | valueText.ToInput(placeholder: "e.g. 25");

        var output = Layout.Vertical().Gap(1)
                     | Text.Label("Result")
                     | (result is double v
                         ? Text.H3(v.ToString("G"))
                         : Text.Muted("n/a"));

        return Layout.Center()
               | new Card(Layout.Vertical().Gap(4) | header | inputs | output)
                    .Width(Size.Units(120).Max(720));
    }

    private static double? TryConvert(string quantityName, string fromUnitName, string toUnitName, string valueText)
    {
        try
        {
            if (!double.TryParse(valueText, out var value)) return null;

            var qInfo = Quantity.Infos.First(q => q.Name.Equals(quantityName, StringComparison.OrdinalIgnoreCase));
            var unitEnumType = qInfo.UnitType;

            var fromUnit = Enum.Parse(unitEnumType, fromUnitName, ignoreCase: true);
            var toUnit = Enum.Parse(unitEnumType, toUnitName, ignoreCase: true);

            var quantity = Quantity.From(value, (Enum)fromUnit);
            var converted = quantity.ToUnit((Enum)toUnit);
            return (double)converted.Value;
        }
        catch
        {
            return null;
        }
    }

    private static AsyncSelectQueryDelegate<string> QueryQuantities()
    {
        return async query =>
        {
            await Task.CompletedTask;
            return Quantity.Infos
                .Where(q => string.IsNullOrEmpty(query) || q.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.Name)
                .Select(q => new Option<string>(q.Name, q.Name))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<string> LookupQuantity()
    {
        return async id =>
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(id)) return null;
            var q = Quantity.Infos.FirstOrDefault(q => q.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
            return q == null ? null : new Option<string>(q.Name, q.Name);
        };
    }

    private static AsyncSelectQueryDelegate<string> QueryUnits(IState<string> quantity)
    {
        return async query =>
        {
            await Task.CompletedTask;
            var qInfo = Quantity.Infos.FirstOrDefault(q => q.Name.Equals(quantity.Value, StringComparison.OrdinalIgnoreCase))
                       ?? Quantity.Infos.First();
            return qInfo.UnitInfos
                .Where(u => string.IsNullOrEmpty(query) || u.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.Name)
                .Select(u => new Option<string>(u.Name, u.Name))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<string> LookupUnit(IState<string> quantity)
    {
        return async id =>
        {
            await Task.CompletedTask;
            if (string.IsNullOrEmpty(id)) return null;
            var qInfo = Quantity.Infos.FirstOrDefault(q => q.Name.Equals(quantity.Value, StringComparison.OrdinalIgnoreCase))
                       ?? Quantity.Infos.First();
            var unit = qInfo.UnitInfos.FirstOrDefault(u => u.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
            return unit == null ? null : new Option<string>(unit.Name, unit.Name);
        };
    }
}

