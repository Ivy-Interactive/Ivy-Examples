namespace MagickNetExample.Apps;

[App(title:"Magick.NET Demo")]
public class MagickApp : ViewBase
{
    public override object? Build()
    {
        var resultState = UseState("Upload an image to resize it with custom dimensions!");
        var widthState = UseState("300");
        var heightState = UseState("200");
        var fileInputState = UseState<FileInput?>((FileInput?)null);
        var processedImageBytes = UseState<byte[]?>((byte[]?)null);

        var uploadUrl = this.UseUpload(
            fileBytes => {
                try
                {
                    if (!int.TryParse(widthState.Value, out int targetWidth) || targetWidth <= 0)
                    {
                        resultState.Value = "❌ Please enter a valid width before uploading.";
                        return;
                    }

                    if (!int.TryParse(heightState.Value, out int targetHeight) || targetHeight <= 0)
                    {
                        resultState.Value = "❌ Please enter a valid height before uploading.";
                        return;
                    }

                    // Process uploaded image with Magick.NET
                    using var image = new MagickImage(fileBytes);
                    var originalSize = $"{image.Width}x{image.Height}";
                    var originalFormat = image.Format.ToString();

                    image.Resize(targetWidth, targetHeight);
                    var resizedSize = $"{image.Width}x{image.Height}";

                    // Convert back to bytes for download
                    processedImageBytes.Value = image.ToByteArray();

                    resultState.Value = $"Image Processed Successfully!\n" +
                                      $"Original: {originalSize} ({originalFormat})\n" +
                                      $"Resized: {resizedSize}\n" +
                                      $"Ready for download! Click 'Download Resized Image' below.";
                }
                catch (Exception ex)
                {
                    resultState.Value = $"Error processing image: {ex.Message}";
                    processedImageBytes.Value = null;
                }
            },
            "image/*",
            "uploaded-image"
        );

        // Download handler - provides processed image for download
        var downloadUrl = this.UseDownload(
            () => processedImageBytes.Value ?? [],
            "image/png",
            $"resized-image-{widthState.Value}x{heightState.Value}.png"
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
                     | widthState.ToInput(placeholder: "300")
                     | Text.Block("Height:")
                     | heightState.ToInput(placeholder: "200")

                   | new Separator()

                   // File upload section
                   | Text.H3("Upload & Resize Image")
                   | fileInputState.ToFileInput(uploadUrl, "Choose image file to upload and resize")
                     .Accept("image/*")

                   | new Separator()

                   // Download section
                   | Text.H3("Download Processed Image")
                   | (processedImageBytes.Value != null && downloadUrl.Value != null
                       ? new Button("Download Resized Image").Url(downloadUrl.Value)
                       : Text.Block("Process an image first to enable download"))

                   | new Separator()

                   // Status/Results
                   | Text.Block(resultState.Value)
                 ));
    }
}