using Figgle.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixImage = SixLabors.ImageSharp.Image;

namespace ASCIIArtGenerator.Apps;

public class AsciiArtService
{
    private const string AsciiGradient = " .:-=+*#%@";

    public string ConvertTextToAscii(string text, string fontName)
    {
        var font = GetFiggleFont(fontName);
        return font.Render(text);
    }

    public string ConvertImageToAscii(byte[] imageData, int width, bool invert)
    {
        var gradient = invert
            ? new string(AsciiGradient.Reverse().ToArray())
            : AsciiGradient;

        using var image = SixImage.Load<Rgba32>(imageData);

        
        var aspectRatio = (double)image.Height / image.Width;
        var height = (int)(width * aspectRatio * 0.5);

        image.Mutate(x => x.Resize(width, height));

        var sb = new System.Text.StringBuilder();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                var brightness = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B) / 255.0;
                var index = (int)(brightness * (gradient.Length - 1));
                sb.Append(gradient[index]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static Figgle.FiggleFont GetFiggleFont(string fontName)
    {
        // Try built-in name lookup first
        var font = FiggleFonts.TryGetByName(fontName);
        if (font is not null)
            return font;

        // Fallback: reflection-based property lookup (case-insensitive)
        var property = typeof(FiggleFonts)
            .GetProperty(fontName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);

        if (property?.GetValue(null) is Figgle.FiggleFont resolved)
            return resolved;

        return FiggleFonts.Standard;
    }
}
