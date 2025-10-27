namespace MagickNet;

[App(title: "Magick.NET Demo")]
public class MagickNetApp : ViewBase
{
    public override object? Build()
    {
        var resultState = UseState("Upload an image and set dimensions, then click 'Process & Download' to resize!");
        var widthState = UseState(300);
        var heightState = UseState(200);
        var fileInputState = UseState<FileInput?>((FileInput?)null);
        var uploadedImageBytes = UseState<byte[]?>((byte[]?)null);
        var processedImageBytes = UseState<byte[]?>((byte[]?)null);

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

                    resultState.Value = $"Image uploaded successfully!\n" +
                                      $"Original: {originalSize} ({originalFormat})\n" +
                                      $"Set your desired dimensions and click 'Process & Download' to resize.";
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

        // Function to process and download image
        var processAndDownload = () =>
        {
            try
            {
                if (uploadedImageBytes.Value == null)
                {
                    resultState.Value = "❌ Please upload an image first.";
                    return;
                }

                if (widthState.Value <= 0)
                {
                    resultState.Value = "❌ Please enter a valid width.";
                    return;
                }

                if (heightState.Value <= 0)
                {
                    resultState.Value = "❌ Please enter a valid height.";
                    return;
                }

                int targetWidth = widthState.Value;
                int targetHeight = heightState.Value;

                // Process uploaded image with Magick.NET
                using var image = new MagickImage(uploadedImageBytes.Value);
                var originalSize = $"{image.Width}x{image.Height}";
                var originalFormat = image.Format.ToString();

                image.Resize(new MagickGeometry((uint)targetWidth, (uint)targetHeight) { IgnoreAspectRatio = true });
                var resizedSize = $"{image.Width}x{image.Height}";

                processedImageBytes.Value = image.ToByteArray();

                resultState.Value = $"Image processed successfully!\n" +
                                  $"Original: {originalSize} ({originalFormat})\n" +
                                  $"Resized: {resizedSize}\n" +
                                  $"Download will start automatically.";
            }
            catch (Exception ex)
            {
                resultState.Value = $"Error processing image: {ex.Message}";
                processedImageBytes.Value = null;
            }
        };

        // Download handler - provides processed image for download
        var downloadUrl = this.UseDownload(
            () =>
            {
                processAndDownload();
                return processedImageBytes.Value ?? [];
            },
            "image/png",
            $"resized-image.png"
        );


        return Layout.Vertical().Padding(3)
               | Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(4).Padding(3)
                   | Text.H2("Magick.NET-Q16-AnyCPU Demo")
                   | Text.Block("Upload images and resize them to custom dimensions.")
                   | new Separator()

                   // Resize dimensions
                   | Text.H3("Resize Dimensions")
                   | Layout.Horizontal().Gap(4)
                     | Text.Block("Width:")
                     | new NumberInput<int>(widthState)
                     | Text.Block("Height:")
                     | new NumberInput<int>(heightState)

                   | new Separator()

                   // File upload section
                   | Text.H3("Upload & Resize Image")
                   | fileInputState.ToFileInput(uploadUrl, "Choose image file to upload and resize")
                     .Accept("image/*")

                   | new Separator()

                   // Process & Download section
                   | Text.H3("Process & Download")
                   | (uploadedImageBytes.Value != null && downloadUrl.Value != null
                       ? new Button("Process & Download Resized Image").Url(downloadUrl.Value)
                       : Text.Block("Upload an image and set dimensions first"))

                   | new Separator()

                   // Status/Results
                   | Text.Block(resultState.Value)
                 ));
    }
}