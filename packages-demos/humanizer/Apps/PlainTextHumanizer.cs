namespace HumanizerExample;

[App(icon: Icons.Box, title: "Humanizer")]
public class PlainTextHumanizerApp : ViewBase
{

    public override object? Build()
    {
        var inputText = this.UseState("");
        var humanizedTexts = this.UseState(ImmutableArray.Create<string>());
        var truncateValue = this.UseState(5);
        var selectedTransformation = this.UseState("");
        PlainTextOptions options = new(inputText, humanizedTexts, truncateValue, selectedTransformation);

        return Layout.Horizontal().Gap(15).Width(Size.Full()).Align(Align.TopCenter)
            // Left Card - Input Section
            | new Card(
                Layout.Vertical().Gap(4)
                    | Text.H3("Text Humanizer")
                    | Text.Muted("This app transforms text using various humanization techniques like PascalCase, camelCase, kebab-case, and more.")
                    | new Spacer()
                    | inputText.ToTextInput(placeholder: "Enter text here...")
                        .Size(Size.Full())
                        .WithLabel("Enter your text to transform:")
                    | new NumberInput<int>(truncateValue)
                        .Min(0)
                        .Max(100)
                        .Disabled(!selectedTransformation.Value.Contains("Truncate"))
                        .WithLabel("Truncate Value:")
                    | options
                    | Text.Small("This demo uses Humanizer library for transforming text using various humanization techniques.")
                    | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Humanizer](https://github.com/Humanizr/Humanizer)")
            ).Width(Size.Fraction(0.4f)).Height(Size.Fit().Min(Size.Full()))
            // Right Card - Results Section  
            | new Card(
                Layout.Vertical().Gap(4)
                    | Text.H3("Transformed Results")
                    | (humanizedTexts.Value.Any() 
                        ? Layout.Vertical().Gap(2)
                            | Text.Muted("You can use these transformed texts in your code, documentation, or anywhere you need formatted text.")
                            | new Spacer()
                            | humanizedTexts.Value.Reverse().Select(text => 
                                Text.Code(text)
                            ).ToArray()
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("No transformations yet. Enter some text, select a transformation, and click 'Transform' to see results here.")
                            | Text.Muted("Try typing something like: 'hello_world_test' or 'ThisIsATest'")
                    )
            ).Width(Size.Fraction(0.4f)).Height(Size.Fit().Min(Size.Full()));
    }
}