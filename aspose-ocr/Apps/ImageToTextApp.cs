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

        var leftCard = new Card(
            Layout.Vertical().Gap(6).Padding(3)
            | Text.H2("Input")
            | Text.Muted("Upload an image and run OCR")
            | (error.Value != null ? new Callout(error.Value, variant: CalloutVariant.Error) : null)
            | files.ToFileInput(uploadUrl, "Upload Image").Accept("image/*")
            | new Button("Recognize").Primary().Icon(Icons.Eye)
                .HandleClick(() =>
                {
                    if (error.Value == null && fileBytes.Value != null)
                    {
                        using var ms = new MemoryStream(fileBytes.Value);

                        var recognitionEngine = new AsposeOcr();
                        using var source = new OcrInput(InputType.SingleImage);
                        source.Add(ms);

                        var results = recognitionEngine.Recognize(source);
                        outputText.Value = results.Count > 0 ? results[0].RecognitionText : string.Empty;
                        fileBytes.Set((byte[]?)null);
                    }
                })
            | Text.Small("This demo uses Aspose.OCR for .NET to recognize text.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Aspose.OCR for .NET](https://products.aspose.com/ocr/net/)")
        ).Width(Size.Fraction(0.45f)).Height(130);

        var rightCardBody = Layout.Vertical().Gap(4)
            | Text.H2("Recognized Text")
            | Text.Muted("Output")
            | outputText.ToCodeInput()
                .Width(Size.Full())
                .Height(Size.Units(70))
                .Language(Languages.Text);

        var rightCard = new Card(rightCardBody).Width(Size.Fraction(0.45f)).Height(130);

        return Layout.Horizontal().Gap(6).Align(Align.Center)
            | leftCard
            | rightCard;
    }
}