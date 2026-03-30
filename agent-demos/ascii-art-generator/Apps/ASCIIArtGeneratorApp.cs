using Ivy;

namespace ASCIIArtGenerator.Apps;

[App(icon: Icons.Terminal)]
public class ASCIIArtGeneratorApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Tabs(
            new Tab("Text to ASCII", new TextToAsciiView()).Icon(Icons.Type),
            new Tab("Image to ASCII", new ImageToAsciiView()).Icon(Icons.Image)
        );
    }
}
