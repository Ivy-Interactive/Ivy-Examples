using Ivy;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace PDF.Compressor.Apps;

[App(icon: Icons.FileDown)]
public class CompressorApp : ViewBase
{
    public override object? Build()
    {
        var fileState = UseState<FileUpload<byte[]>?>();
        var quality = UseState("medium");
        var compressing = UseState(false);
        var compressedBytes = UseState<byte[]?>();
        var errorMessage = UseState<string?>();
        var uploadCtx = UseUpload(MemoryStreamUploadHandler.Create(fileState));
        var downloadUrl = UseDownload(
            factory: () => compressedBytes.Value ?? Array.Empty<byte>(),
            mimeType: "application/pdf",
            fileName: GetCompressedFileName(fileState.Value?.FileName)
        );

        var upload = uploadCtx
            .Accept(".pdf")
            .MaxFileSize(FileSize.FromMegabytes(30));

        var qualitySelect = quality.ToSelectInput(new IAnyOption[]
        {
            new Option<string>("Low — Best compression", "low"),
            new Option<string>("Medium — Balanced", "medium"),
            new Option<string>("High — Best quality", "high"),
        }).WithField().Label("Compression Level");

        var fileInput = fileState.ToFileInput(upload).Placeholder("Drop your PDF here");

        var hasFile = fileState.Value?.Status == FileUploadStatus.Finished && fileState.Value.Content != null;
        var hasResult = compressedBytes.Value != null;


        var content = Layout.Vertical()
            | Text.H1("PDF Compressor")
            | Text.Muted("Upload a PDF file and compress it to reduce file size.")
            | new Separator()
            | fileInput
            | qualitySelect
            | new Button("Compress PDF", onClick: async () =>
            {
                if (!hasFile) return;
                compressing.Set(true);
                errorMessage.Set(null);
                compressedBytes.Set(null);
                try
                {
                    var result = CompressPdf(fileState.Value!.Content!, quality.Value);
                    compressedBytes.Set(result);
                }
                catch (Exception ex)
                {
                    errorMessage.Set($"Compression failed: {ex.Message}");
                }
                finally
                {
                    compressing.Set(false);
                }
            }).Icon(Icons.FileDown).Disabled(!hasFile || compressing.Value).Loading(compressing.Value);

        if (errorMessage.Value is { } err)
        {
            content |= Callout.Error(err);
        }

        if (hasResult && fileState.Value != null)
        {
            var originalSize = fileState.Value.Content!.Length;
            var compressedSize = compressedBytes.Value!.Length;
            var reduction = originalSize > 0
                ? (1.0 - (double)compressedSize / originalSize) * 100
                : 0;

            content |= new Separator();
            content |= Callout.Success($"Compressed from {FormatSize(originalSize)} to {FormatSize(compressedSize)} — {reduction:F1}% reduction", "Compression Complete");
            content |= new Button("Download Compressed PDF", icon: Icons.Download)
                .Url(downloadUrl.Value!)
                .Success();
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | content);
    }

    private static byte[] CompressPdf(byte[] pdfBytes, string quality)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var outputStream = new MemoryStream();

        var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
        var outputDoc = new PdfDocument();

        foreach (var page in document.Pages)
        {
            outputDoc.AddPage(page);
        }

        // PdfSharpCore compression options based on quality
        outputDoc.Options.FlateEncodeMode = quality switch
        {
            "low" => PdfFlateEncodeMode.BestCompression,
            "high" => PdfFlateEncodeMode.BestSpeed,
            _ => PdfFlateEncodeMode.Default,
        };

        outputDoc.Save(outputStream);
        return outputStream.ToArray();
    }

    private static string GetCompressedFileName(string? originalName)
    {
        if (string.IsNullOrEmpty(originalName)) return "compressed.pdf";
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalName);
        return $"{nameWithoutExt}_compressed.pdf";
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes / 1_024.0:F1} KB",
            _ => $"{bytes} B",
        };
    }
}
