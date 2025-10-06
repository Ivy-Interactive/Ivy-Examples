using Aspose.OCR;
using System.IO;

namespace AsposeOcrExample.Apps;

[App(icon: Icons.FileImage, path: ["Apps"])]
public class ImageToTextApp : ViewBase
{
    public override object? Build()
    {
        var outputText = this.UseState<string>("");

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
                    using var ms = new MemoryStream(fileBytes.Value);

                    // Create recognition engine
                    var recognitionEngine = new AsposeOcr();

                    // Prepare input (single image) and add uploaded stream
                    using var source = new OcrInput(InputType.SingleImage);
                    source.Add(ms);

                    // Recognize text
                    var results = recognitionEngine.Recognize(source);

                    // Output first result
                    outputText.Value = results.Count > 0 ? results[0].RecognitionText : string.Empty;
                    fileBytes.Set((byte[]?)null); // Clear stored bytes once completed
                }
            }),
            Text.Block("Output Text:"),
            outputText.ToCodeInput()
                .Width(Size.Auto())
                .Height(Size.Auto())
                .Language(Languages.Text)
            ).Align(Align.Center);
    }
}