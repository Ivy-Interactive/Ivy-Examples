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
        var isFirstRender = UseState(() => true);
        var lastQuantity = UseState(() => quantity.Value);

        // Search term state
        var quantitySearchTerm = UseState(() => "Temperature");

        // Clear fromUnit and toUnit when quantity changes (but not on first render)
        UseEffect(() =>
        {
            if (!isFirstRender.Value && lastQuantity.Value != quantity.Value)
            {
                fromUnit.Set("");
                toUnit.Set("");
            }
            isFirstRender.Set(false);
            lastQuantity.Set(quantity.Value);
        }, quantity);

        var result = TryConvert(quantity.Value, fromUnit.Value, toUnit.Value, valueText.Value);

        // Header Card
        var headerCard = new Card(
            Layout.Vertical().Gap(1)
            | Text.H2("UnitsNet Converter")
            | Text.Muted("Convert between different units of measurement using the UnitsNet library")
        );
        var footerCard = new Card(
            Layout.Vertical().Gap(1)
            | Text.Small("This demo uses UnitsNet library to convert between different units of measurement.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [UnitsNet](https://github.com/angularsen/UnitsNet)")
                
        );

        // Get filtered quantity list items
        var allQuantities = Quantity.Infos.OrderBy(q => q.Name).ToArray();
        var filteredQuantities = string.IsNullOrEmpty(quantitySearchTerm.Value)
            ? allQuantities
            : allQuantities
                .Where(q => q.Name.Contains(quantitySearchTerm.Value, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        
        var quantityListItems = filteredQuantities
            .Select(q => new ListItem(
                title: q.Name,
                subtitle: "Quantity type",
                onClick: () => quantity.Set(q.Name)
            ))
            .ToArray();
        
        // Quantity Selection Card
        var quantityCard = new Card(
            Layout.Vertical().Gap(2)
            | Text.H4("Select Quantity Type")
            | (string.IsNullOrEmpty(quantity.Value)
                ? Text.Muted("Select a quantity type from the list below")
                : Text.Muted($"Selected: {quantity.Value}"))
            | quantitySearchTerm.ToSearchInput().Placeholder("Search Quantity Types")
            | (Layout.Vertical().Gap(2).Height(Size.Units(30))
            | new List(quantityListItems))
        );

        // Get unit list items
        var qInfo = Quantity.Infos.FirstOrDefault(q => q.Name.Equals(quantity.Value, StringComparison.OrdinalIgnoreCase))
                   ?? Quantity.Infos.First();
        
        var fromUnitListItems = qInfo.UnitInfos
            .OrderBy(u => u.Name)
            .Select(u => new ListItem(
                title: u.Name,
                subtitle: $"Unit of {quantity.Value}",
                onClick: () => fromUnit.Set(u.Name)
            ))
            .ToArray();
        
        var toUnitListItems = qInfo.UnitInfos
            .OrderBy(u => u.Name)
            .Select(u => new ListItem(
                title: u.Name,
                subtitle: $"Unit of {quantity.Value}",
                onClick: () => toUnit.Set(u.Name)
            ))
            .ToArray();

        // Determine conversion info
        var hasFromUnit = !string.IsNullOrEmpty(fromUnit.Value);
        var hasToUnit = !string.IsNullOrEmpty(toUnit.Value);

        // From Unit Card
        var fromUnitCard = new Card(
            Layout.Vertical().Gap(3)
            | Text.H4(hasFromUnit ? $"From: {fromUnit.Value}" : "Select a source unit from the list below")
            | Text.Muted("Select a source unit from the list below and enter a value to convert")
            | valueText.ToInput(placeholder: "e.g. 25")
            | (Layout.Vertical().Gap(2).Height(Size.Fit().Max(Size.Units(30)))
            | new List(fromUnitListItems))
            
        );

        // To Unit Card
        var toUnitCard = new Card(
            Layout.Vertical().Gap(3)
            | Text.H4(hasToUnit ? $"To: {toUnit.Value}" : "Select a target unit from the list below")
            | Text.Muted("Select a target unit from the list below and enter a value to convert")
            | (hasFromUnit && hasToUnit
                ? Layout.Vertical().Gap(2)
                    | (result is double v
                        ? Text.Code($"{v.ToString("G")}")
                        : Text.Code("Enter a valid number to convert"))
                : Text.Code("Select both units to see the result"))
            | (Layout.Vertical().Gap(2).Height(Size.Fit().Max(Size.Units(30)))
            | new List(toUnitListItems))
            
        );

        // Horizontal layout for unit cards
        var unitCardsRow = Layout.Vertical()
            | (Layout.Horizontal().Gap(3)
            | fromUnitCard
            | toUnitCard);

        // Input Card
        var inputCard = new Card(
            Layout.Vertical().Gap(3)
            
        ).Title("Input");

        // Result Card
        var resultCard = new Card(
            Layout.Vertical().Gap(3)
            
        ).Title("Result");

        // Conversion Cards Row
        var conversionCardsRow =
        Layout.Vertical()
        | new Card(
            Layout.Horizontal().Gap(3)
            | inputCard
            | resultCard);

        // Main vertical layout
        return Layout.Vertical().Align(Align.TopCenter).Gap(3)
            | headerCard.Width(Size.Fraction(0.7f))
            | quantityCard.Width(Size.Fraction(0.7f))
            | unitCardsRow.Width(Size.Fraction(0.7f))
            | footerCard.Width(Size.Fraction(0.7f));
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

}

