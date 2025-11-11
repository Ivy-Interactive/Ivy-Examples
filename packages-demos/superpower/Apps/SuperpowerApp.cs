namespace SuperpowerExample;

[App(icon: Icons.PartyPopper, title: "Superpower Example")]
public class SuperpowerApp
    : ViewBase
{
    private const string JsonParserSection = "json-parser";
    private const string IntCalculatorSection = "int-calculator";
    private const string DateTimeTextParserSection = "date-time-text-parser";

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var currentSection = UseState(JsonParserSection);

        JsonParserView jsonParserView = new();
        IntegerCalculatorView integerCalculatorView = new();
        DateTimeParserView dateTimeParserView = new();

        var navHeader = Layout.Horizontal().Gap(3).Padding(2)
            | new Button("📄 JSON Parser")
                .Variant(currentSection.Value == JsonParserSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => currentSection.Value = JsonParserSection)
            | new Button("🧮 Calculator")
                .Variant(currentSection.Value == IntCalculatorSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => currentSection.Value = IntCalculatorSection)
            | new Button("📅 DateTime Parser")
                .Variant(currentSection.Value == DateTimeTextParserSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                .HandleClick(_ => currentSection.Value = DateTimeTextParserSection);

        object GetSectionContent()
        {
            return currentSection.Value switch
            {
                JsonParserSection => Layout.Vertical().Gap(4).Padding(3)
                    | new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("JSON Parser")
                        | Text.P("Complete JSON parser implementing the json.org specification")
                        | Text.Muted("Demonstrates building an efficient parser with quality error handling using Superpower")
                    )
                    | jsonParserView,

                IntCalculatorSection => Layout.Vertical().Gap(4).Padding(3)
                    | new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Arithmetic Expression Parser")
                        | Text.P("Simple arithmetic expression parser (integer calculator)")
                        | Text.Muted("Supports addition, subtraction, multiplication and division with proper operator precedence")
                    )
                    | integerCalculatorView,

                DateTimeTextParserSection => Layout.Vertical().Gap(4).Padding(3)
                    | new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Date Time Text Parser")
                        | Text.P("Date and time parser for ISO-8601 format")
                        | Text.Muted("Examples: 2017-01-01, 2017-01-01 05:28:10, 2017-01-01T05:28:10")
                    )
                    | dateTimeParserView,

                _ => Text.P("Section not found")
            };
        }

        return Layout.Vertical().Gap(3).Padding(3)
            | Text.H1("Superpower Parser Examples")
            | Text.Muted("Demonstrating parsers built with Superpower library")
            | new Card(navHeader)
            | GetSectionContent();
    }

}
