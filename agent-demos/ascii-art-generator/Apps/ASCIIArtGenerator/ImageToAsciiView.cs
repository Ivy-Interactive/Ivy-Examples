using Ivy;

namespace ASCIIArtGenerator.Apps;

public class ImageToAsciiView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<AsciiArtService>();

        var fileState = UseState<FileUpload<byte[]>?>();
        var width = UseState(80);
        var invert = UseState(false);

        var upload = UseUpload(MemoryStreamUploadHandler.Create(fileState));
        upload.Accept(".png,.jpg,.jpeg,.gif,.bmp");

        var fileInput = fileState.ToFileInput(upload, placeholder: "Upload an image");

        object content;
        if (fileState.Value?.Content is { Length: > 0 } imageData)
        {
            var asciiArt = service.ConvertImageToAscii(imageData, width.Value, invert.Value);
            content = new CodeBlock(asciiArt, Languages.Text).ShowCopyButton().WrapLines();
        }
        else
        {
            content = Text.Muted("Upload an image to generate ASCII art.");
        }

        return Layout.TopCenter()
            | (Layout.Vertical()
                .Width(Size.Full().Max(200))
                .Margin(10)
                | fileInput
                | width.ToNumberInput().WithField().Label("Output Width")
                | invert.ToBoolInput().WithField().Label("Invert Brightness")
                | content);
    }
}
