namespace MagickNet;

[App(title: "Magick.NET")]
public class MagickNetApp : ViewBase
{
    public override object? Build()
    {
        // State management
        var resultState = UseState("Welcome to Magick.NET Image Studio! Upload an image to start creating amazing effects.");
        var uploadState = UseState<FileUpload<byte[]>?>();
        var uploadContext = this.UseUpload(MemoryStreamUploadHandler.Create(uploadState))
            .Accept("image/*")
            .MaxFileSize(50 * 1024 * 1024);
        var uploadedImageBytes = UseState<byte[]?>(() => null);
        var processedImageBytes = UseState<byte[]?>(() => null);
        var processedImageDataUri = UseState<string?>(() => null);
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

        var client = this.UseService<IClientProvider>();
        
        // When a file is uploaded, process it
        UseEffect(() =>
        {
            if (uploadState.Value?.Content is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    uploadedImageBytes.Value = bytes;
                    processedImageBytes.Value = null;
                    processedImageDataUri.Value = null;

                    using var image = new MagickImage(bytes);
                    var originalSize = $"{image.Width}x{image.Height}";
                    var originalFormat = image.Format.ToString();
                    var fileSize = bytes.Length / 1024.0;

                    resultState.Value = $"Image uploaded successfully!\n" +
                                      $"Original: {originalSize} ({originalFormat})\n" +
                                      $"Size: {fileSize:F1} KB\n" +
                                      $"Choose an effect and click 'Magic Image' to transform your image!";
                }
                catch (Exception ex)
                {
                    client.Toast($"Error uploading image: {ex.Message}", "Upload Error");
                    uploadedImageBytes.Value = null;
                }
            }
        }, [uploadState]);

        // Function to process image with selected effect
        var processImage = () =>
        {
            try
            {
                if (uploadedImageBytes.Value == null)
                {
                    client.Toast("Please upload an image first.", "Validation Error");
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

                // Create data URI for processed image display
                var processedBase64 = Convert.ToBase64String(processedImageBytes.Value);
                var processedMimeType = $"image/{selectedFormat.Value}";
                processedImageDataUri.Value = $"data:{processedMimeType};base64,{processedBase64}";

                resultState.Value = $"Image processed successfully!\n" +
                                  $"Original: {originalSize} ({originalFormat})\n" +
                                  $"Processed: {processedSize} ({selectedFormat.Value.ToUpper()})\n" +
                                  $"Output size: {outputSize:F1} KB\n" +
                                  $"Effect: {selectedEffect.Value.Replace("_", " ")}\n" +
                                  $"Download will start automatically.";
            }
            catch (Exception ex)
            {
                client.Toast($"Error processing image: {ex.Message}", "Processing Error");
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

        // Left Card - Controls Panel
        var leftCard = new Card(
                       Layout.Vertical()
                       | Text.H3("Digital Image Alchemy")
                       | Text.Muted("Transform your images with powerful effects and filters!")

                       // File upload section
                       | Text.H4("Upload Image")
                       | uploadState.ToFileInput(uploadContext)
                         .Placeholder("Choose image file to upload")

                       // Effect selection
                       | Text.H4("Choose Effect")

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
                            new Option<string>("Flip", "flip")
                        })

                       // Effect parameters
                       | Text.H4("Effect Parameters")
                       | (selectedEffect.Value == "resize"
                           ? Layout.Vertical()
                             | (Layout.Horizontal().Gap(4)
                               | Text.Block("Width:")
                               | new NumberInput<int>(widthState)
                               | Text.Block("Height:")
                               | new NumberInput<int>(heightState))
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
                             | new NumberInput<double>(hue).Min(0).Max(180).Step(1)
                           : selectedEffect.Value == "rotate"
                           ? Layout.Horizontal().Gap(4)
                             | Text.Block("Rotation (degrees):")
                             | new NumberInput<double>(rotation).Min(-360).Max(360).Step(1)
                           : selectedEffect.Value == "flip"
                           ? Layout.Vertical().Gap(2)
                             | flipHorizontal.ToBoolInput(variant: BoolInputs.Checkbox).Label("Flip horizontally")
                             | flipVertical.ToBoolInput(variant: BoolInputs.Checkbox).Label("Flip vertically")
                           : Text.Block("No additional parameters needed for this effect."))
                       // Output format
                       | Text.H4("Output Format")
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
                           
                       | (Layout.Horizontal().Gap(4)
                       | (uploadedImageBytes.Value != null
                           ? new Button("Magic Image", onClick: () => processImage())
                           : new Button("Magic Image").Disabled())
                       | new Button("Download")
                           .Primary()
                           .Icon(Icons.Download)
                           .Url(downloadUrl.Value)
                           .Disabled(processedImageBytes.Value == null))
                        | new Spacer().Height(Size.Units(5))
                        | Text.Small("This demo uses Magick.NET to process images with powerful effects and filters.")
                        | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Magick.NET](https://github.com/dlemstra/Magick.NET)")
                     );

        // Right Card - Image Display Panel
        var rightCard = new Card(
                       Layout.Vertical()
                       | Text.H3("Image Preview")
                       | Text.Muted(resultState.Value)
                       | new Spacer().Height(Size.Units(10))
                       | (Layout.Horizontal().Align(Align.Center)
                       | new Image(processedImageDataUri.Value))
                       
                     );

        return Layout.Vertical().Padding(3)
               | Layout.Center()
               | (Layout.Horizontal().Gap(10)
                  | leftCard
                  | rightCard);
    }
}