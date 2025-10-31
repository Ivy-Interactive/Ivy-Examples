namespace MimeMappingExample;

[App(icon: Icons.FileText, title: "MimeMapping")]
public class MimeMappingApp : ViewBase
{
    private enum InputMethod { UploadFile, EnterFileName }
    
    public override object? Build()
    {
        var selectedTab = this.UseState(0);
        var inputMethod = this.UseState(InputMethod.UploadFile);
        var fileInput = this.UseState<string>();
        var fileUpload = this.UseState<FileInput?>(() => null);
        var mimeTypeInput = this.UseState<string>();
        var searchQuery = this.UseState<string>();
        
        var uploadUrl = this.UseUpload(
            uploadedBytes => { }, // No action needed for file upload
            "*/*",
            "uploaded-file"
        );
        
        var currentFileName = inputMethod.Value == InputMethod.UploadFile ? fileUpload.Value?.Name : fileInput.Value;
        var detectedMimeType = currentFileName != null 
            ? MimeUtility.GetMimeMapping(currentFileName) 
            : null;
            
        var extensions = !string.IsNullOrEmpty(mimeTypeInput.Value) 
            ? MimeUtility.GetExtensions(mimeTypeInput.Value) 
            : null;

        var filteredTypes = string.IsNullOrEmpty(searchQuery.Value)
            ? MimeUtility.TypeMap.Take(50)
            : MimeUtility.TypeMap.Where(kvp => 
                kvp.Key.Contains(searchQuery.Value, StringComparison.OrdinalIgnoreCase) ||
                kvp.Value?.Contains(searchQuery.Value, StringComparison.OrdinalIgnoreCase) == true)
              .Take(100);

        return Layout.Vertical()
            | Text.H2("MimeMapping Library Demo")
            | Text.Muted("Detect MIME types from file extensions, search and browse all supported types, and perform reverse lookup to find file extensions by MIME type. Upload files or enter file names to see real-time detection.")

            // Tab navigation
            | Layout.Tabs(
                new Tab("Detect MIME Type", BuildFileInputDemo(inputMethod, fileInput, fileUpload, uploadUrl, currentFileName, detectedMimeType)),
                new Tab("Browse Types", BuildBrowseTypesDemo(searchQuery, filteredTypes)),
                new Tab("Reverse Lookup", BuildReverseLookupDemo(mimeTypeInput, extensions))
            )
            .Variant(TabsVariant.Tabs);
    }

    private object BuildFileInputDemo(IState<InputMethod> inputMethod, IState<string> fileInput, IState<FileInput?> fileUpload, IState<string?> uploadUrl, string? currentFileName, string? detectedMimeType)
    {
        object inputSection = inputMethod.Value == InputMethod.UploadFile
            ? Layout.Vertical()
                | Text.Label("Choose File")
                | fileUpload.ToFileInput(uploadUrl)
            : Layout.Vertical()
                | Text.Label("Enter File Name")
                | fileInput.ToInput(placeholder: "e.g., image.jpg, document.pdf, archive.zip");

        return Layout.Horizontal().Gap(5)
            | new Card(
            Layout.Vertical().Gap(5)
            | Text.H3("Detect MIME Type")
            | Text.Muted("Upload a file or enter a file name to detect the MIME type")
            | (Layout.Vertical().Gap(5)
                | Text.Label("Select input method:")
                | inputMethod.ToSelectInput(typeof(InputMethod).ToOptions())
                | inputSection)
            )

            | new Card(
            Layout.Vertical().Gap(5)
            | (detectedMimeType != null ?
                new Card(
                    Layout.Vertical()
                    | Text.H4("Detection Result:")
                    | Text.Block($"File: {currentFileName}")
                    | Text.Block($"MIME Type: {detectedMimeType}")
                    | (detectedMimeType == MimeUtility.UnknownMimeType ?
                        Text.Muted("Unknown file type - returns default application/octet-stream") :
                        Text.Success(" Known file type detected"))
                ) :
                Text.Muted("Enter a file name or select a file above to see the MIME type detection")
            ));
    }

    private object BuildBrowseTypesDemo(IState<string> searchQuery, IEnumerable<KeyValuePair<string, string?>> filteredTypes)
    {
        return Layout.Vertical().Gap(3)
            | Text.H3("Browse Available MIME Types")
            | Text.Block($"Showing {MimeUtility.TypeMap.Count} total MIME types. Search to filter:")
            | searchQuery.ToInput(placeholder: "Search by extension or MIME type...")
            | new Card(
                Layout.Vertical().Gap(2)
                | filteredTypes.ToTable()
            )
            | Text.Muted($"Showing {filteredTypes.Count()} of {MimeUtility.TypeMap.Count} types");
    }

    private object BuildReverseLookupDemo(IState<string> mimeTypeInput, string[]? extensions)
    {
        return Layout.Vertical().Gap(3)
            | Text.H3("MIME Type to Extensions Lookup")
            | Text.Block("Enter a MIME type to find all associated file extensions:")
            | mimeTypeInput.ToInput(placeholder: "e.g., image/jpeg, application/pdf, text/html")
            | (extensions != null && extensions.Length > 0 ? 
                new Card(
                    Layout.Vertical().Gap(2)
                    | Text.H4("Associated Extensions:")
                    | Text.Block($"MIME Type: {mimeTypeInput.Value}")
                    | Layout.Horizontal().Gap(1).Wrap()
                        | extensions.Select(ext => new Badge(ext)).ToArray()
                    | Text.Muted($"Found {extensions.Length} extension(s)")
                ) :
                !string.IsNullOrEmpty(mimeTypeInput.Value) ?
                new Card(
                    Layout.Vertical().Gap(2)
                    | Text.H4("No Extensions Found")
                    | Text.Block($"MIME Type: {mimeTypeInput.Value}")
                    | Text.Muted("This MIME type is not recognized or has no associated extensions")
                ) :
                Text.Muted("Enter a MIME type above to find associated extensions"))
            | new Separator()
            | Text.H4("Try these examples:")
            | Layout.Grid().Columns(2).Gap(1)
                | new Button("image/jpeg", onClick: () => mimeTypeInput.Set("image/jpeg"))
                | new Button("application/pdf", onClick: () => mimeTypeInput.Set("application/pdf"))
                | new Button("text/html", onClick: () => mimeTypeInput.Set("text/html"))
                | new Button("application/json", onClick: () => mimeTypeInput.Set("application/json"))
                | new Button("video/mp4", onClick: () => mimeTypeInput.Set("video/mp4"))
                | new Button("application/zip", onClick: () => mimeTypeInput.Set("application/zip"));
    }
}
