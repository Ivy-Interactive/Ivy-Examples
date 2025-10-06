using AsposeOcrExample.Connections.OCR;
using System.IO;

namespace AsposeOcrExample.Apps;

[App(icon: Icons.FileImage, path: ["Apps"])]
public class ImageToTextApp : ViewBase
{
    public override object? Build()
    {
        var outputText = this.UseState<string>("");
        var ocrService = UseService<IOCRService>();

        var error = UseState<string?>(() => null);
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null); // Store the actual file bytes

        var uploadUrl = this.UseUpload(
            uploadedBytes =>
            {
                if (uploadedBytes.Length > 1 * 1024 * 1024) // 1MB limit
                {
                    error.Set("File size must be less than 1MB");
                    fileBytes.Set((byte[]?)null); // Clear stored bytes on error
                    return;
                }

                error.Set((string?)null);
                fileBytes.Set(uploadedBytes); // Store the file bytes for later use
            },
            "image/jpeg",
            "uploaded-image"
        );

        return Layout.Vertical(
            Text.H1("Convert Image to text online").Color(Colors.Green),
            Text.Block("Free OCR software to convert images or screenshots to text online"),
            error.Value != null
                ? new Callout(error.Value, variant: CalloutVariant.Error)
                : null,
            files.ToFileInput(uploadUrl, "Upload Image").Accept("image/*"),
            new Button("Recognize", _ =>
            {
                if (error.Value == null && fileBytes.Value != null)
                {
                    // Use the stored file bytes instead of file.Content
                    using var ms = new MemoryStream(fileBytes.Value);
                    outputText.Value = ocrService.ExtractText(ms);
                    fileBytes.Set((byte[]?)null); // Clear stored bytes once completed
                }
            }),
            Text.Block("Output Text:"),
            new ObservableView<string>(outputText)
            ).Align(Align.Center);
    }
}