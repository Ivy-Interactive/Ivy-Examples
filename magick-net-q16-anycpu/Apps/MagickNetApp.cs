namespace MagickNet;

[App(title: "Magick.NET Image Studio")]
public class MagickNetApp : ViewBase
{
    public override object? Build()
    {
        // State management
        var resultState = UseState("Welcome to Magick.NET Image Studio! Upload an image to start creating amazing effects.");
        var fileInputState = UseState<FileInput?>(() => null);
        var uploadedImageBytes = UseState<byte[]?>(() => null);
        var processedImageBytes = UseState<byte[]?>(() => null);
        var selectedEffect = UseState("resize");
        var selectedFormat = UseState("png");
        
        // Resize parameters
        var widthState = UseState(400);
        var heightState = UseState(300);
        var maintainAspectRatio = UseState(true);
        
        // Effect parameters
        var blurRadius = UseState(5.0);
        var sharpenRadius = UseState(1.0);
        var brightness = UseState(0.0);
        var contrast = UseState(1.0);
        var saturation = UseState(1.0);
        var hue = UseState(0.0);
        var rotation = UseState(0.0);
        var flipHorizontal = UseState(false);
        var flipVertical = UseState(false);
        var quality = UseState(90);

        var uploadUrl = this.UseUpload(
            fileBytes =>
            {
                try
                {
                    uploadedImageBytes.Value = fileBytes;
                    processedImageBytes.Value = null;

                    using var image = new MagickImage(fileBytes);
                    var originalSize = $"{image.Width}x{image.Height}";
                    var originalFormat = image.Format.ToString();
                    var fileSize = fileBytes.Length / 1024.0;

                    resultState.Value = $"Image uploaded successfully!\n" +
                                      $"Original: {originalSize} ({originalFormat})\n" +
                                      $"Size: {fileSize:F1} KB\n" +
                                      $"Choose an effect and click 'Process & Download' to transform your image!";
                }
                catch (Exception ex)
                {
                    resultState.Value = $"Error uploading image: {ex.Message}";
                    uploadedImageBytes.Value = null;
                }
            },
            "image/*",
            "uploaded-image"
        );

        // Function to process image with selected effect
        var processImage = () =>
        {
            try
            {
                if (uploadedImageBytes.Value == null)
                {
                    resultState.Value = "Please upload an image first.";
                    return;
                }

                using var image = new MagickImage(uploadedImageBytes.Value);
                var originalSize = $"{image.Width}x{image.Height}";
                var originalFormat = image.Format.ToString();

                // Apply selected effect
                switch (selectedEffect.Value)
                {
                    case "resize":
                        var geometry = maintainAspectRatio.Value 
                            ? new MagickGeometry((uint)widthState.Value, (uint)heightState.Value) { IgnoreAspectRatio = false }
                            : new MagickGeometry((uint)widthState.Value, (uint)heightState.Value) { IgnoreAspectRatio = true };
                        image.Resize(geometry);
                        break;

                    case "blur":
                        image.Blur(blurRadius.Value, blurRadius.Value);
                        break;

                    case "sharpen":
                        image.Sharpen(sharpenRadius.Value, sharpenRadius.Value);
                        break;

                    case "brightness":
                        image.BrightnessContrast(new Percentage(brightness.Value), new Percentage(0));
                        break;

                    case "contrast":
                        image.BrightnessContrast(new Percentage(0), new Percentage((contrast.Value - 1) * 100));
                        break;

                    case "saturation":
                        image.Modulate(new Percentage(100), new Percentage(saturation.Value * 100), new Percentage(100));
                        break;

                    case "hue":
                        image.Modulate(new Percentage(100), new Percentage(100), new Percentage(hue.Value));
                        break;

                    case "rotate":
                        image.Rotate(rotation.Value);
                        break;

                    case "flip":
                        if (flipHorizontal.Value) image.Flop();
                        if (flipVertical.Value) image.Flip();
                        break;

                    case "grayscale":
                        image.Grayscale(PixelIntensityMethod.Average);
                        break;

                    case "sepia":
                        image.SepiaTone();
                        break;

                    case "oil_painting":
                        image.OilPaint();
                        break;

                    case "charcoal":
                        image.Charcoal();
                        break;

                    case "emboss":
                        image.Emboss();
                        break;

                    case "edge":
                        image.Edge(1.0);
                        break;

                    case "negate":
                        image.Negate();
                        break;

                    case "solarize":
                        image.Solarize();
                        break;
                }

                // Set output format and quality
                switch (selectedFormat.Value)
                {
                    case "jpeg":
                        image.Format = MagickFormat.Jpeg;
                        image.Quality = (uint)quality.Value;
                        break;
                    case "png":
                        image.Format = MagickFormat.Png;
                        break;
                    case "webp":
                        image.Format = MagickFormat.WebP;
                        image.Quality = (uint)quality.Value;
                        break;
                    case "bmp":
                        image.Format = MagickFormat.Bmp;
                        break;
                    case "gif":
                        image.Format = MagickFormat.Gif;
                        break;
                }

                var processedSize = $"{image.Width}x{image.Height}";
                processedImageBytes.Value = image.ToByteArray();
                var outputSize = processedImageBytes.Value.Length / 1024.0;

                resultState.Value = $"Image processed successfully!\n" +
                                  $"Original: {originalSize} ({originalFormat})\n" +
                                  $"Processed: {processedSize} ({selectedFormat.Value.ToUpper()})\n" +
                                  $"Output size: {outputSize:F1} KB\n" +
                                  $"Effect: {selectedEffect.Value.Replace("_", " ")}\n" +
                                  $"Download will start automatically.";
            }
            catch (Exception ex)
            {
                resultState.Value = $"Error processing image: {ex.Message}";
                processedImageBytes.Value = null;
            }
        };

        // Download handler
        var downloadUrl = this.UseDownload(
            () =>
            {
                processImage();
                return processedImageBytes.Value ?? [];
            },
            $"image/{selectedFormat.Value}",
            $"processed-image.{selectedFormat.Value}"
        );

        return Layout.Vertical().Padding(3)
               | Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(4).Padding(3)
                   | Text.H2("Magick.NET Image Studio")
                   | Text.Block("Transform your images with powerful effects and filters!")
                   | new Separator()

                   // File upload section
                   | Text.H3("Upload Image")
                   | fileInputState.ToFileInput(uploadUrl, "Choose image file to upload")
                     .Accept("image/*")

                   | new Separator()

                   // Effect selection
                   | Text.H3("Choose Effect")
                   | Layout.Horizontal().Gap(2)
                     | selectedEffect.ToSelectInput(new[]
                       {
                           new Option<string>("Resize", "resize"),
                           new Option<string>("Blur", "blur"),
                           new Option<string>("Sharpen", "sharpen"),
                           new Option<string>("Brightness", "brightness"),
                           new Option<string>("Contrast", "contrast"),
                           new Option<string>("Saturation", "saturation"),
                           new Option<string>("Hue Shift", "hue"),
                           new Option<string>("Rotate", "rotate"),
                           new Option<string>("Flip", "flip"),
                           new Option<string>("Grayscale", "grayscale"),
                           new Option<string>("Sepia", "sepia"),
                           new Option<string>("Oil Painting", "oil_painting"),
                           new Option<string>("Charcoal", "charcoal"),
                           new Option<string>("Emboss", "emboss"),
                           new Option<string>("Edge Detection", "edge"),
                           new Option<string>("Negate", "negate"),
                           new Option<string>("Solarize", "solarize")
                       })

                   | new Separator()

                   // Effect parameters
                   | Text.H3("Effect Parameters")
                   | (selectedEffect.Value == "resize" 
                       ? Layout.Vertical().Gap(2)
                         | Layout.Horizontal().Gap(4)
                           | Text.Block("Width:")
                           | new NumberInput<int>(widthState)
                           | Text.Block("Height:")
                           | new NumberInput<int>(heightState)
                         | maintainAspectRatio.ToBoolInput(variant: BoolInputs.Checkbox).Label("Maintain aspect ratio")
                       : selectedEffect.Value == "blur"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Blur Radius:")
                         | new NumberInput<double>(blurRadius).Min(0).Max(50).Step(0.5)
                       : selectedEffect.Value == "sharpen"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Sharpen Radius:")
                         | new NumberInput<double>(sharpenRadius).Min(0).Max(10).Step(0.1)
                       : selectedEffect.Value == "brightness"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Brightness:")
                         | new NumberInput<double>(brightness).Min(-100).Max(100).Step(1)
                       : selectedEffect.Value == "contrast"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Contrast:")
                         | new NumberInput<double>(contrast).Min(0).Max(3).Step(0.1)
                       : selectedEffect.Value == "saturation"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Saturation:")
                         | new NumberInput<double>(saturation).Min(0).Max(3).Step(0.1)
                       : selectedEffect.Value == "hue"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Hue Shift:")
                         | new NumberInput<double>(hue).Min(-180).Max(180).Step(1)
                       : selectedEffect.Value == "rotate"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Rotation (degrees):")
                         | new NumberInput<double>(rotation).Min(-360).Max(360).Step(1)
                       : selectedEffect.Value == "flip"
                       ? Layout.Vertical().Gap(2)
                         | flipHorizontal.ToBoolInput(variant: BoolInputs.Checkbox).Label("Flip horizontally")
                         | flipVertical.ToBoolInput(variant: BoolInputs.Checkbox).Label("Flip vertically")
                       : Text.Block("No additional parameters needed for this effect."))

                   | new Separator()

                   // Output format
                   | Text.H3("Output Format")
                   | Layout.Horizontal().Gap(2)
                     | selectedFormat.ToSelectInput(new[]
                       {
                           new Option<string>("PNG", "png"),
                           new Option<string>("JPEG", "jpeg"),
                           new Option<string>("WebP", "webp"),
                           new Option<string>("BMP", "bmp"),
                           new Option<string>("GIF", "gif")
                       })

                   | (selectedFormat.Value == "jpeg" || selectedFormat.Value == "webp"
                       ? Layout.Horizontal().Gap(4)
                         | Text.Block("Quality:")
                         | new NumberInput<int>(quality).Min(1).Max(100).Step(1)
                       : Text.Block(""))

                   | new Separator()

                   // Process & Download section
                   | Text.H3("Process & Download")
                   | (uploadedImageBytes.Value != null && downloadUrl.Value != null
                       ? new Button($"Process & Download ({selectedEffect.Value.Replace("_", " ")})").Url(downloadUrl.Value)
                       : Text.Block("Upload an image first to start processing"))

                   | new Separator()

                   // Status/Results
                   | Text.Block(resultState.Value)
                 ));
    }
}