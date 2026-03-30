using Ivy;

namespace ASCIIArtGenerator.Apps;

public class TextToAsciiView : ViewBase
{
    public override object? Build()
    {
        var text = UseState("Hello World");
        var selectedFont = UseState("Standard");
        var service = UseService<AsciiArtService>();

        var fonts = new[]
        {
            "Standard", "Banner", "Big", "Slant", "Small", "Block",
            "Lean", "Mini", "Script", "Shadow", "ThreePoint", "Doom",
            "Ivrit", "Ogre", "Rectangles", "StarWars"
        };

        var result = service.ConvertTextToAscii(text.Value, selectedFont.Value);

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H3("Text to ASCII Art")
                | text.ToTextInput().Placeholder("Enter text to convert").WithField().Label("Text")
                | selectedFont.ToSelectInput(fonts.ToOptions()).WithField().Label("Font")
                | new CodeBlock(result).Language(Languages.Text).ShowCopyButton().WrapLines()
            );
    }
}
