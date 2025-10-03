using Superpower.Apps;

namespace SuperpowerExample.Apps;

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

        var navHeader = new Card(
            Layout.Horizontal().Gap(3)
                | new Button("JSON Parser")
                    .Variant(currentSection.Value == JsonParserSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                    .HandleClick(_ => {
                        currentSection.Value = JsonParserSection;
                        client.Toast("Navigated to JSON Parser");
                    })
                | new Button("Integer Calculator")
                    .Variant(currentSection.Value == IntCalculatorSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                    .HandleClick(_ => {
                        currentSection.Value = IntCalculatorSection;
                        client.Toast("Navigated to Integer Calculator");
                    })
                | new Button("Date Time Text Parser")
                    .Variant(currentSection.Value == DateTimeTextParserSection ? ButtonVariant.Primary : ButtonVariant.Ghost)
                    .HandleClick(_ => {
                        currentSection.Value = DateTimeTextParserSection;
                        client.Toast("Navigated to Date Time Text Parser");
                    })
        );

        JsonParserView jsonParserView = new();
        IntegerCalculatorView integerCalculatorView = new();
        DateTimeParserView dateTimeParserView = new();

        object GetSectionContent()
        {
            return currentSection.Value switch
            {
                JsonParserSection => Layout.Vertical().Gap(4)
                    | Text.Label("JSON Parser")
                    | Text.P("This is an example JSON parser")
                    | new Card("This parser correctly and completely implements the language spec at https://json.org (or should), " +
                    "but the goal isn't to use this \"for rea\" - there are no tests, after all! :-)\n" +
                    "The goal of the example is to demonstrate how a reasonably-efficient parser " +
                    "with end-user-quality error reporting can be built using Superpower")
                    | jsonParserView,

                IntCalculatorSection => Layout.Vertical().Gap(4)
                    | Text.Label("Arithmetic Expression Parser")
                    | Text.P("This is a simple arithmetic expression parser (integer calculator)")
                    | new Card("Demonstrates the use of Superpower for parsing of arithmetic expressions. Supports addition, subtraction, multiplication, and division")
                    | integerCalculatorView,

                DateTimeTextParserSection => Layout.Vertical().Gap(4)
                    | Text.Label("Date Time Text Parser")
                    | Text.P("This is a simple arithmetic expression parser (integer calculator)")
                    | new Card("Demonstrates how Superpower's text parsers work, parsing ISO-8601 date-times")
                    | Text.Code("Example formats:\r\n2017-01-01\r\n2017-01-01 05:28:10\r\n2017-01-01 05:28\r\n2017-01-01T05:28:10\r\n2017-01-01T05:28")
                    | dateTimeParserView,

                _ => Text.P("Section not found")
            };
        }

        return new HeaderLayout(navHeader, GetSectionContent());
    }

}
