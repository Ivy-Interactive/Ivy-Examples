using System.IO;
using System.Text;

namespace Acme.InternalProject.Apps;

[App]
public class FileConverterApp : ViewBase
{
    public override object? Build()
    {
        var uploadedFile = UseState<FileInput?>(() => null);
        var isProcessing = UseState(() => false);
        var convertedContent = UseState<string?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);

        var uploadUrl = this.UseUpload(
            bytes =>
            {
                isProcessing.Set(true);
                fileBytes.Set(bytes);
                Task.Delay(100).ContinueWith(_ => isProcessing.Set(false));
            },
            "application/octet-stream",
            "file-upload"
        );

        var content = Layout.Vertical()
                | Text.H1("📄 File Converter")
                | Text.P("Upload a file to convert it to different formats")
                | uploadedFile.ToFileInput(uploadUrl, "Choose File")
                    .Placeholder("Drag and drop or click to select")
                    .Accept(".txt,.csv,.json,.md,.log")
                    .Variant(FileInputs.Drop);

        if (isProcessing.Value)
        {
            content |= Callout.Info("Uploading file...");
        }

        if (uploadedFile.Value != null && !isProcessing.Value && fileBytes.Value != null)
        {
            content |= new Card()
                | Text.H3($"📁 {uploadedFile.Value.Name}")
                | Text.P($"Size: {FormatFileSize(uploadedFile.Value.Size)}")
                | Text.H2("Conversion Options")
                | Layout.Vertical().Gap(2)
                    | Layout.Horizontal().Gap(2)
                        | new Button("🔤 Uppercase").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "text-uppercase", convertedContent))
                        | new Button("🔤 Lowercase").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "text-lowercase", convertedContent))
                    | Layout.Horizontal().Gap(2)
                        | new Button("123 Line Numbers").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "text-add-numbers", convertedContent))
                        | new Button("🔄 Reverse").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "text-reverse", convertedContent))
                    | Layout.Horizontal().Gap(2)
                        | new Button("📊 Format JSON").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "json-format", convertedContent))
                        | new Button("📋 CSV to MD").HandleClick(_ => ConvertFile(fileBytes.Value, uploadedFile.Value.Name, "csv-to-markdown", convertedContent));
        }
        
        if (convertedContent.Value != null && uploadedFile.Value != null)
        {
            var displayText = convertedContent.Value.Length > 5000 
                ? convertedContent.Value.Substring(0, 5000) + "\n...[truncated]" 
                : convertedContent.Value;
            
            content |= Text.H2("Converted Content");
            
            // Create download URL for the converted content
            var originalFileName = uploadedFile.Value.Name;
            var extension = System.IO.Path.GetExtension(originalFileName)?.ToLower() ?? ".txt";
            var downloadFilename = System.IO.Path.GetFileNameWithoutExtension(originalFileName) + "-converted" + extension;
            
            var dlUrl = this.UseDownload(
                () => Task.FromResult(Encoding.UTF8.GetBytes(convertedContent.Value)),
                "text/plain",
                downloadFilename
            );
            
            if (dlUrl.Value != null)
            {
                content |= new Button("⬇️ Download Converted File")
                    .Url(dlUrl.Value);
            }
            
            content |= Text.Code(displayText);
            
            if (convertedContent.Value.Length > 5000)
            {
                content |= Callout.Warning("Showing first 5,000 characters. Click download for full content.");
            }
        }

        return content;
    }
    
    private void ConvertFile(byte[] fileBytes, string? fileName, string conversionType, 
        IState<string?> convertedContent)
    {
        var result = ProcessFile(fileBytes, fileName, conversionType);
        convertedContent.Set(result);
    }
    
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private string ProcessFile(byte[] fileBytes, string? fileName, string conversionType)
    {
        var content = Encoding.UTF8.GetString(fileBytes);
        var extension = System.IO.Path.GetExtension(fileName)?.ToLower();

        return conversionType switch
        {
            "text-uppercase" => content.ToUpper(),
            "text-lowercase" => content.ToLower(),
            "text-reverse" => new string(content.Reverse().ToArray()),
            "text-add-numbers" => string.Join("\n", content.Split('\n').Select((line, index) => $"{index + 1}: {line}")),
            "csv-to-markdown" when extension == ".csv" => ConvertCsvToMarkdown(content),
            "json-format" => FormatJson(content),
            _ => content
        };
    }

    private string ConvertCsvToMarkdown(string csvContent)
    {
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return "";

        var result = new StringBuilder();
        var headers = lines[0].Split(',');
        result.AppendLine("| " + string.Join(" | ", headers.Select(h => h.Trim())) + " |");
        result.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");

        for (int i = 1; i < lines.Length; i++)
        {
            var cells = lines[i].Split(',');
            result.AppendLine("| " + string.Join(" | ", cells.Select(c => c.Trim())) + " |");
        }

        return result.ToString();
    }

    private string FormatJson(string json)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            return System.Text.Json.JsonSerializer.Serialize(jsonElement, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }
}
