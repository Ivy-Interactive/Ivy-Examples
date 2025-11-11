namespace SuperpowerExample;

[App(icon: Icons.PartyPopper, title: "Superpower")]
public class SuperpowerApp
    : ViewBase
{
    public override object? Build()
    {
        JsonParserView jsonParserView = new();
        IntegerCalculatorView integerCalculatorView = new();
        DateTimeParserView dateTimeParserView = new();

        return Layout.Vertical().Gap(3).Padding(3)
            | Text.H1("Superpower Parser Examples")
            | Text.Muted("Demonstrating parsers built with Superpower library")
            | Layout.Tabs(
                new Tab("JSON Parser",
                    Layout.Vertical().Gap(4).Padding(3)
                        | new Card(
                            Layout.Vertical().Padding(3).Width(Size.Fraction(0.5f))
                            | Text.H3("JSON Parser")
                            | Text.Muted("Complete JSON parser implementing the json.org specification. Demonstrates building an efficient parser with quality error handling using Superpower")
                        )
                        | jsonParserView
                ),
                new Tab("Expression Parser",
                    Layout.Vertical().Gap(4).Padding(3)
                        | new Card(
                            Layout.Vertical().Padding(3).Width(Size.Fraction(0.5f))
                            | Text.H3("Arithmetic Expression Parser")
                            | Text.Muted("Simple arithmetic expression parser (integer calculator). Supports addition, subtraction, multiplication and division with proper operator precedence")
                        )
                        | integerCalculatorView
                ),
                new Tab("DateTime Parser",
                    Layout.Vertical().Gap(4).Padding(3)
                        | new Card(
                            Layout.Vertical().Padding(3).Width(Size.Fraction(0.5f))
                            | Text.H3("Date Time Text Parser")
                            | Text.Muted("Date and time parser for ISO-8601 format"))
                        | dateTimeParserView
                )
            ).Variant(TabsVariant.Tabs);
    }

}
