using System.IO;
using System.Text;
using System.Text.Json;

namespace Acme.InternalProject.Apps;

[App]
public class FileUploaderApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Vertical()
            | Text.H1("File Upload & Download Test Suite")
            | Text.P("Comprehensive testing of all upload and download functionality")
            | Layout.Vertical().Gap(4)
                | TestBasicFileUpload()
                | TestMultipleFileUpload()
                | TestDragDropUpload()
                | TestFileValidation()
                | TestUploadStatusFeedback()
                | TestImageUploadWithPreview()
                | TestTextFilePreview()
                | TestDownloadFeatures()
                | TestErrorHandling();
    }

    private object TestBasicFileUpload()
    {
        var client = UseService<IClientProvider>();
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                fileBytes.Set(bytes);
                client.Toast($"Successfully uploaded {bytes.Length} bytes", "Upload Complete");
            },
            "application/octet-stream",
            "uploaded-file"
        );

        return new Card(
            Layout.Vertical()
                | files.ToFileInput(uploadUrl, "Choose a file")
                | (files.Value != null
                    ? Layout.Horizontal().Gap(2)
                        | Text.P($"Selected: {files.Value.Name} ({FormatFileSize(files.Value.Size)})")
                        | new Button("Preview").WithSheet(
                            () => CreateFilePreviewSheet(files.Value, fileBytes.Value),
                            title: $"Preview: {files.Value.Name}",
                            description: $"File size: {FormatFileSize(files.Value.Size)}",
                            width: Size.Fraction(2/3f)
                        )
                    : null)
        ).Title("Basic File Upload")
         .Description("Test single file upload with status feedback");
    }

    private object TestMultipleFileUpload()
    {
        var client = UseService<IClientProvider>();
        var uploadedFiles = UseState(() => new List<(FileInput file, byte[] bytes)>());
        var newFiles = UseState<IEnumerable<FileInput>?>(() => null);
        var fileContents = UseState(() => new Dictionary<string, byte[]>());
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                // Get the current files from newFiles state
                var currentFiles = newFiles.Value?.ToList() ?? new List<FileInput>();
                var uploadedCount = uploadedFiles.Value.Count;
                
                if (uploadedCount < currentFiles.Count)
                {
                    var currentFile = currentFiles[uploadedCount];
                    uploadedFiles.Set(uploadedFiles.Value.Append((currentFile, bytes)).ToList());
                    fileContents.Set(fileContents.Value.Append(new KeyValuePair<string, byte[]>(currentFile.Name, bytes)).ToDictionary());
                    client.Toast($"File uploaded: {currentFile.Name} ({bytes.Length} bytes)", "Upload Complete");
                }
            },
            "application/octet-stream",
            "uploaded-files"
        );

        return new Card(
            Layout.Vertical()
                | Text.P("Upload multiple files to test batch upload and preview functionality")
                | newFiles.ToFileInput(uploadUrl, "Upload Multiple Files")
                | (newFiles.Value != null && newFiles.Value.Any()
                    ? Layout.Vertical()
                        | Text.H3($"Selected Files ({newFiles.Value.Count()}):")
                        | Layout.Vertical().Gap(2)
                            | newFiles.Value.Select((file, index) => 
                                Layout.Vertical().Gap(1)
                                    | Layout.Horizontal().Gap(2)
                                        | GetFileIcon(file.Name)
                                        | Text.P($"{file.Name}")
                                        | Text.Small($"({FormatFileSize(file.Size)})")
                                        | new Button("Preview").WithSheet(
                                            () => CreateFilePreviewSheet(file, fileContents.Value.ContainsKey(file.Name) ? fileContents.Value[file.Name] : null),
                                            title: $"Preview: {file.Name}",
                                            description: $"{GetFileTypeDescription(file.Name)} - {FormatFileSize(file.Size)}",
                                            width: Size.Fraction(2/3f)
                                        )
                            ).ToArray()
                    : null)
                | (uploadedFiles.Value.Any()
                    ? Layout.Vertical()
                        | Text.H3($"Uploaded Files ({uploadedFiles.Value.Count}):")
                        | Layout.Vertical().Gap(2)
                            | uploadedFiles.Value.Select((item, index) => 
                                Layout.Vertical().Gap(1)
                                    | Layout.Horizontal().Gap(2)
                                        | GetFileIcon(item.file.Name)
                                        | Text.P($"{item.file.Name}")
                                        | Text.Small($"({FormatFileSize(item.file.Size)})")
                                        | new Button("Preview Content").WithSheet(
                                            () => CreateFilePreviewSheet(item.file, item.bytes),
                                            title: $"Preview: {item.file.Name}",
                                            description: $"{GetFileTypeDescription(item.file.Name)} - {FormatFileSize(item.file.Size)}",
                                            width: Size.Fraction(2/3f)
                                        )
                            ).ToArray()
                    : null)
        ).Title("Multiple File Upload")
         .Description("Test uploading multiple files at once with individual previews");
    }

    private object TestDragDropUpload()
    {
        var client = UseService<IClientProvider>();
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                fileBytes.Set(bytes);
                client.Toast($"Drag & drop upload: {bytes.Length} bytes", "Upload Complete");
            },
            "application/octet-stream",
            "drag-drop-file"
        );

        return new Card(
            Layout.Vertical()
                | files.ToFileInput(uploadUrl, "Drag files here or click to select")
                    .Placeholder("Drop files here...")
                    .Variant(FileInputs.Drop)
                | (files.Value != null
                    ? Layout.Horizontal().Gap(2)
                        | Text.P($"Dropped: {files.Value.Name} ({FormatFileSize(files.Value.Size)})")
                        | new Button("Preview").WithSheet(
                            () => CreateFilePreviewSheet(files.Value, fileBytes.Value),
                            title: $"Preview: {files.Value.Name}",
                            description: $"File size: {FormatFileSize(files.Value.Size)}",
                            width: Size.Fraction(2/3f)
                        )
                    : null)
        ).Title("Drag & Drop Upload")
         .Description("Test drag and drop file upload interface");
    }

    private object TestFileValidation()
    {
        var client = UseService<IClientProvider>();
        var error = UseState<string?>(() => null);
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                if (bytes.Length > 2 * 1024 * 1024) // 2MB limit
                {
                    error.Set("File size must be less than 2MB");
                    return;
                }
                fileBytes.Set(bytes);
                error.Set((string?)null);
                client.Toast($"Image uploaded successfully ({bytes.Length} bytes)", "Success");
            },
            "image/jpeg",
            "uploaded-image"
        );

        return new Card(
            Layout.Vertical()
                | files.ToFileInput(uploadUrl, "Upload Image (Max 2MB)")
                    .Accept(".jpg,.jpeg,.png")
                | (error.Value != null
                    ? new Callout(error.Value, variant: CalloutVariant.Error)
                    : null)
                | (files.Value != null
                    ? Layout.Horizontal().Gap(2)
                        | Text.P($"Validated: {files.Value.Name} ({FormatFileSize(files.Value.Size)})")
                        | new Button("Preview").WithSheet(
                            () => CreateFilePreviewSheet(files.Value, fileBytes.Value),
                            title: $"Preview: {files.Value.Name}",
                            description: $"File size: {FormatFileSize(files.Value.Size)}",
                            width: Size.Fraction(2/3f)
                        )
                    : null)
        ).Title("File Validation")
         .Description("Test file type and size validation");
    }

    private object TestUploadStatusFeedback()
    {
        var client = UseService<IClientProvider>();
        var isProcessing = UseState(() => false);
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                isProcessing.Set(true);
                fileBytes.Set(bytes);
                // Simulate processing time
                Task.Delay(2000).ContinueWith(_ =>
                {
                    isProcessing.Set(false);
                    client.Toast($"Processing complete: {bytes.Length} bytes", "Success");
                });
            },
            "application/octet-stream",
            "processing-file"
        );

        return new Card(
            Layout.Vertical()
                | files.ToFileInput(uploadUrl, "Upload File for Processing")
                | (isProcessing.Value
                    ? Callout.Info("Processing file... Please wait")
                    : null)
                | (files.Value != null && !isProcessing.Value
                    ? Layout.Horizontal().Gap(2)
                        | Text.P($"Processed: {files.Value.Name}")
                        | new Button("Preview").WithSheet(
                            () => CreateFilePreviewSheet(files.Value, fileBytes.Value),
                            title: $"Preview: {files.Value.Name}",
                            description: $"File size: {FormatFileSize(files.Value.Size)}",
                            width: Size.Fraction(2/3f)
                        )
                    : null)
        ).Title("Upload Status Feedback")
         .Description("Test upload progress and status indicators");
    }

    private object TestTextFilePreview()
    {
        var client = UseService<IClientProvider>();
        var files = UseState<FileInput?>(() => null);
        var fileBytes = UseState<byte[]?>(() => null);
        var textContent = UseState<string?>(() => null);
        var uploadUrl = this.UseUpload(
            bytes =>
            {
                fileBytes.Set(bytes);
                try
                {
                    var content = Encoding.UTF8.GetString(bytes);
                    textContent.Set(content);
                    client.Toast($"Text file uploaded successfully! Content length: {content.Length} characters", "Upload Complete");
                }
                catch (Exception ex)
                {
                    client.Toast($"Error reading text file: {ex.Message}", "Upload Error");
                }
            },
            "text/plain",
            "text-file"
        );

        return new Card(
            Layout.Vertical()
                | Text.P("Upload a text file to test text content storage and preview")
                | files.ToFileInput(uploadUrl, "Upload Text File")
                    .Accept(".txt,.md,.json,.csv,.log,.xml,.html,.css,.js")
                | (files.Value != null
                    ? Layout.Vertical().Gap(2)
                        | Text.H3("File Information:")
                        | Layout.Vertical().Gap(1)
                            | Text.P($"Name: {files.Value.Name}")
                            | Text.P($"Size: {FormatFileSize(files.Value.Size)}")
                            | Text.P($"Last Modified: {files.Value.LastModified:yyyy-MM-dd HH:mm:ss}")
                            | Text.P($"Type: {files.Value.Type}")
                        | Layout.Horizontal().Gap(2)
                            | new Button("Preview in Sheet").WithSheet(
                                () => CreateFilePreviewSheet(files.Value, fileBytes.Value),
                                title: $"Preview: {files.Value.Name}",
                                description: $"File size: {FormatFileSize(files.Value.Size)}",
                                width: Size.Fraction(2/3f)
                            )
                            | new Button("Show Raw Content").HandleClick(_ => 
                                client.Toast($"Raw content preview:\n\n{textContent.Value?.Substring(0, Math.Min(200, textContent.Value?.Length ?? 0))}...", "Text Content"))
                    : null)
                | (textContent.Value != null
                    ? Layout.Vertical().Gap(2)
                        | Text.H3("Text Content Preview:")
                        | Text.Code(textContent.Value.Length > 1000 
                            ? textContent.Value.Substring(0, 1000) + "\n...[truncated - click Preview for full content]"
                            : textContent.Value)
                        | Text.Small($"Total characters: {textContent.Value.Length}")
                    : null)
        ).Title("Text File Preview Test")
         .Description("Test text file upload, storage, and preview functionality");
    }

    private object TestImageUploadWithPreview()
    {
        var client = UseService<IClientProvider>();
        var preview = UseState<string?>(() => null);
        var files = UseState<FileInput?>(() => null);
        var uploadUrl = this.UseUpload(
            fileBytes =>
            {
                // Create preview URL from uploaded bytes
                preview.Set($"data:image/jpeg;base64,{Convert.ToBase64String(fileBytes)}");
                client.Toast($"Image uploaded successfully ({fileBytes.Length} bytes)", "Success");
            },
            "image/jpeg",
            "uploaded-image"
        );

        return new Card(
            Layout.Vertical()
                | files.ToFileInput(uploadUrl, "Upload Image").Accept("image/*")
                | (preview.Value != null
                    ? Layout.Horizontal()
                        | new Image(preview.Value).Width(200).Height(150)
                        | Text.P($"Image preview loaded")
                    : null)
        ).Title("Image Upload with Preview")
         .Description("Test image upload with live preview");
    }

    private object TestDownloadFeatures()
    {
        var client = UseService<IClientProvider>();
        var progress = UseState(0.0);
        var downloadUrl = this.UseDownload(
            () =>
            {
                // Simulate large file generation
                var content = GenerateLargeContent();
                return Encoding.UTF8.GetBytes(content);
            },
            "text/plain",
            $"test-download-{DateTime.Now:yyyy-MM-dd-HH-mm}.txt"
        );

        var csvDownloadUrl = this.UseDownload(
            () =>
            {
                var csvContent = "Name,Email,Age\nJohn,john@example.com,30\nJane,jane@example.com,25\nBob,bob@example.com,35";
                return Encoding.UTF8.GetBytes(csvContent);
            },
            "text/csv",
            $"export-{DateTime.Now:yyyy-MM-dd}.csv"
        );

        var jsonDownloadUrl = this.UseDownload(
            () =>
            {
                var jsonData = new
                {
                    timestamp = DateTime.Now,
                    message = "Test JSON download",
                    data = new[] { "item1", "item2", "item3" }
                };
                var jsonContent = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
                return Encoding.UTF8.GetBytes(jsonContent);
            },
            "application/json",
            $"data-{DateTime.Now:yyyy-MM-dd}.json"
        );

        return new Card(
            Layout.Vertical()
                | Layout.Vertical().Gap(2)
                    | Layout.Horizontal().Gap(2)
                        | (downloadUrl.Value != null 
                            ? new Button("Download Large Text File").Url(downloadUrl.Value)
                            : null)
                        | (csvDownloadUrl.Value != null 
                            ? new Button("Download CSV Export").Url(csvDownloadUrl.Value)
                            : null)
                    | (jsonDownloadUrl.Value != null 
                        ? new Button("Download JSON Data").Url(jsonDownloadUrl.Value)
                        : null)
                | (progress.Value > 0
                    ? Text.P($"Download Progress: {progress.Value:P0}")
                    : null)
        ).Title("Download Features")
         .Description("Test various download scenarios");
    }

    private object TestErrorHandling()
    {
        var client = UseService<IClientProvider>();
        var error = UseState<string?>(() => null);
        var files = UseState<FileInput?>(() => null);
        var uploadUrl = this.UseUpload(
            fileBytes =>
            {
                try
                {
                    // Simulate error condition
                    if (fileBytes.Length == 0)
                    {
                        throw new Exception("Empty file not allowed");
                    }
                    if (fileBytes.Length > 5 * 1024 * 1024) // 5MB limit
                    {
                        throw new Exception("File too large (max 5MB)");
                    }
                    error.Set((string?)null);
                    client.Toast($"File uploaded successfully ({fileBytes.Length} bytes)", "Success");
                }
                catch (Exception ex)
                {
                    error.Set(ex.Message);
                    client.Toast(ex.Message, "Upload Error");
                }
            },
            "application/octet-stream",
            "error-test-file"
        );

        var errorDownloadUrl = this.UseDownload(
            () => Task.FromException<byte[]>(new Exception("Simulated download error")),
            "text/plain",
            "error-test.txt"
        );

        return new Card(
            Layout.Vertical()
                | Layout.Vertical().Gap(2)
                    | files.ToFileInput(uploadUrl, "Upload File (Test Error Handling)")
                    | (error.Value != null
                        ? new Callout(error.Value, variant: CalloutVariant.Error)
                        : null)
                    | (errorDownloadUrl.Value != null 
                        ? new Button("Test Download Error").Url(errorDownloadUrl.Value)
                        : null)
        ).Title("Error Handling")
         .Description("Test error handling for uploads and downloads");
    }

    private object GetFileIcon(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => Text.P("IMG"),
            ".txt" or ".md" or ".json" or ".csv" or ".log" or ".xml" or ".html" or ".css" or ".js" => Text.P("TXT"),
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => Text.P("VID"),
            ".pdf" => Text.P("PDF"),
            ".zip" or ".rar" or ".7z" => Text.P("ZIP"),
            ".exe" or ".msi" => Text.P("EXE"),
            _ => Text.P("FILE")
        };
    }

    private string GetFileTypeDescription(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "Image File",
            ".txt" or ".md" or ".json" or ".csv" or ".log" or ".xml" or ".html" or ".css" or ".js" => "Text File",
            ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => "Video File",
            ".pdf" => "PDF Document",
            ".zip" or ".rar" or ".7z" => "Archive File",
            ".exe" or ".msi" => "Executable File",
            _ => "Binary File"
        };
    }

    private string GetFileExtensionFromBytes(byte[] bytes)
    {
        if (bytes.Length == 0) return ".bin";
        
        // Check for common file signatures (magic numbers)
        if (bytes.Length >= 4)
        {
            // JPEG files
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
            
            // PNG files
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";
            
            // GIF files
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return ".gif";
            
            // PDF files
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return ".pdf";
            
            // ZIP files
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04) return ".zip";
            
            // MP4 files
            if (bytes.Length >= 8 && 
                ((bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70) || // ftyp
                 (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x18))) return ".mp4";
        }
        
        // Check for UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return ".txt";
        
        // Check if it's mostly ASCII text (for text files)
        var sampleSize = Math.Min(200, bytes.Length);
        var asciiCount = bytes.Take(sampleSize).Count(b => b >= 32 && b <= 126 || b == 9 || b == 10 || b == 13);
        var asciiRatio = (double)asciiCount / sampleSize;
        
        if (asciiRatio > 0.8)
        {
            // Try to detect specific text formats
            var textStart = Encoding.UTF8.GetString(bytes.Take(Math.Min(100, bytes.Length)).ToArray());
            
            if (textStart.Contains("{") && textStart.Contains("}")) return ".json";
            if (textStart.Contains("<") && textStart.Contains(">")) return ".html";
            if (textStart.Contains(",") && textStart.Contains("\n")) return ".csv";
            if (textStart.Contains("#")) return ".md";
            
            return ".txt";
        }
        
        return ".bin";
    }

    private string GetMimeTypeFromExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private object CreateFilePreviewSheet(FileInput file, byte[]? fileBytes)
    {
        var extension = System.IO.Path.GetExtension(file.Name)?.ToLower();
        var isImage = extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp";
        var isText = extension is ".txt" or ".md" or ".json" or ".csv" or ".log" or ".xml" or ".html" or ".css" or ".js";
        var isVideo = extension is ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm";

        // Try to get file content from FileInput if no bytes provided
        byte[]? contentBytes = fileBytes;

        return Layout.Vertical().Gap(3)
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.H3("File Information")
                    | Layout.Vertical().Gap(1)
                        | Text.P($"Name: {file.Name}")
                        | Text.P($"Size: {FormatFileSize(file.Size)}")
                        | Text.P($"Type: {extension ?? "Unknown"}")
                        | Text.P($"Last Modified: {file.LastModified:yyyy-MM-dd HH:mm:ss}")
                        | Text.P($"MIME Type: {file.Type}")
            ).Title("File Details")
            | (isImage && contentBytes != null
                ? new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Image Preview")
                        | new Image($"data:image/{extension?.TrimStart('.')};base64,{Convert.ToBase64String(contentBytes)}")
                            .Width(Size.Fraction(1f))
                            .Height(Size.Units(300))
                ).Title("Image")
                : null)
            | (isText && contentBytes != null
                ? new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Text Content")
                        | Text.Code(Encoding.UTF8.GetString(contentBytes).Length > 5000 
                            ? Encoding.UTF8.GetString(contentBytes).Substring(0, 5000) + "\n...[truncated]"
                            : Encoding.UTF8.GetString(contentBytes))
                ).Title("Text")
                : null)
            | (isVideo && contentBytes != null
                ? new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Video Preview")
                        | Text.P("Video file detected. Preview not available in this demo.")
                        | Text.P($"Video size: {FormatFileSize(file.Size)}")
                ).Title("Video")
                : null)
            | (!isImage && !isText && !isVideo && contentBytes != null
                ? new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Binary File Content")
                        | Text.P($"This is a binary file ({extension ?? "unknown type"})")
                        | Text.P($"File size: {FormatFileSize(file.Size)}")
                        | Text.P($"First 100 bytes (hex): {BitConverter.ToString(contentBytes.Take(100).ToArray())}")
                ).Title("Binary File")
                : null)
            | (contentBytes == null
                ? new Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("File Content Not Available")
                        | Text.P("File content is not available for preview.")
                        | Text.P("This might be because the file hasn't been uploaded yet.")
                ).Title("No Content")
                : null);
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

    private string GenerateLargeContent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Large Test File Content");
        sb.AppendLine($"Generated at: {DateTime.Now}");
        sb.AppendLine();

        for (int i = 1; i <= 1000; i++)
        {
            sb.AppendLine($"Line {i}: This is test content for line number {i}. " +
                         $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                         $"Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");
        }

        return sb.ToString();
    }
}
