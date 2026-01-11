using Ivy;
using Ivy.Apps;
using SkiaSharp;
using Svg.Skia;
using Path = System.IO.Path;
using System.IO;

namespace SvgSkiaDemo.Apps;

// Define an Ivy App with icon and title
[App(icon: Icons.PartyPopper, title: "Svg Skia Demo")]
public class SvgSkiaDemoApp : ViewBase
{
    // Stores the loaded SVG as a SkiaSharp picture
    private SKPicture? _svgPicture;

    // Path to the wwwroot folder (relative to project output)
    private static readonly string WwwRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\wwwroot"));

    // Full path to the SVG and PNG files
    private readonly string _svgPath = Path.Combine(WwwRoot, "input.svg");
    private readonly string _pngPath = Path.Combine(WwwRoot, "output.png");

    // Ivy state to track PNG export status
    private IState<string>? _pngStatus;

    // Constructor runs once when the app starts
    public SvgSkiaDemoApp()
    {
        // Ensure wwwroot folder exists
        Directory.CreateDirectory(WwwRoot);

        // Load the SVG into memory for rendering
        LoadSvg();
    }

    // Build defines the Ivy UI for this app
    public override object? Build()
    {
        // Initialize the status state if null
        _pngStatus ??= this.UseState<string>(
            File.Exists(_pngPath) ? $"✅ Saved: {_pngPath}" : "Click button to export PNG"
        );

        // Layout the UI vertically
        return Layout.Vertical().Gap(12).Padding(16)
            | Text.H2("SVG → PNG Demo")               // Header
            | new Image(RenderSvgToDataUrl()).Width(100) // Render SVG to an image
            | new Button("Convert to PNG", ConvertSvgToPng) // Button to export PNG
            | Text.Small(_pngStatus.Value);          // Show status text
    }

    // Load the SVG file into a SkiaSharp SKPicture
    private void LoadSvg()
    {
        var svg = new SKSvg();  // Create a new SVG loader
        svg.Load(_svgPath);     // Load the SVG file
        _svgPicture = svg.Picture; // Store the rendered picture
    }

    // Render the SVG into a Base64 data URL for Ivy Image
    private string RenderSvgToDataUrl()
    {
        if (_svgPicture == null) return ""; // Return empty string if no SVG loaded

        const int width = 400, height = 400;   // Output canvas size

        // Create a bitmap and canvas to draw the SVG
        using var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);           // Clear canvas to white

        // Scale the SVG to fit inside the canvas
        var rect = _svgPicture.CullRect;        // Get SVG bounds
        var scale = Math.Min(width / rect.Width, height / rect.Height);

        canvas.Save();                          // Save current canvas state
        canvas.Translate(width / 2f, height / 2f); // Move origin to center
        canvas.Scale(scale);                     // Scale to fit canvas
        canvas.Translate(-rect.MidX, -rect.MidY); // Center the SVG
        canvas.DrawPicture(_svgPicture);        // Draw the SVG
        canvas.Restore();                        // Restore canvas state

        // Encode bitmap as PNG and convert to Base64 for Ivy Image
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return $"data:image/png;base64,{Convert.ToBase64String(data.ToArray())}";
    }

    // Convert the SVG to a PNG file on disk
    private void ConvertSvgToPng()
    {
        if (_svgPicture == null) return;  // Nothing to convert if SVG not loaded

        const int width = 400, height = 400;  // PNG output size

        // Create a Skia surface for drawing
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);          // Clear canvas to white

        // Scale and center the SVG on the canvas
        var rect = _svgPicture.CullRect;
        var scale = Math.Min(width / rect.Width, height / rect.Height);

        canvas.Save();
        canvas.Translate(width / 2f, height / 2f);
        canvas.Scale(scale);
        canvas.Translate(-rect.MidX, -rect.MidY);
        canvas.DrawPicture(_svgPicture);      // Draw the SVG
        canvas.Restore();

        // Save the canvas to PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(_pngPath, data.ToArray()); // Write PNG file

        // Update the status state to notify the user
        _pngStatus.Value = $"✅ Saved: {_pngPath}";
    }

  
}
