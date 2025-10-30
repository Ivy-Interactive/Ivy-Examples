namespace MimeMappingExample;

[App(icon: Icons.FileText, title: "MimeMapping Demo")]
public class MimeMappingApp : ViewBase
{
    public override object? Build()
    {
        var selectedTab = this.UseState(0);
        var fileInput = this.UseState<string>();
        var mimeTypeInput = this.UseState<string>();
        var searchQuery = this.UseState<string>();
        
        var detectedMimeType = fileInput.Value != null 
            ? MimeUtility.GetMimeMapping(fileInput.Value) 
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

        return Layout.Vertical().Gap(2).Padding(2)
            | Text.H1("MimeMapping Library Demo")
            | Text.Muted("Interactive demonstration of file extension to MIME type mapping capabilities")
            | new Separator()
            
            // Tab navigation
            | Layout.Horizontal().Gap(1)
                | new Button("File Input Demo", onClick: () => selectedTab.Set(0))
                    .Variant(selectedTab.Value == 0 ? ButtonVariant.Primary : ButtonVariant.Secondary)
                | new Button("Browse Types", onClick: () => selectedTab.Set(1))
                    .Variant(selectedTab.Value == 1 ? ButtonVariant.Primary : ButtonVariant.Secondary)
                | new Button("Reverse Lookup", onClick: () => selectedTab.Set(2))
                    .Variant(selectedTab.Value == 2 ? ButtonVariant.Primary : ButtonVariant.Secondary)
            
            // Tab content with proper scrolling
            | new Card(
                (selectedTab.Value switch
                {
                    0 => BuildFileInputDemo(fileInput, detectedMimeType),
                    1 => BuildBrowseTypesDemo(searchQuery, filteredTypes),
                    2 => BuildReverseLookupDemo(mimeTypeInput, extensions),
                    _ => Text.Block("Select a tab above")
                })
            ).Height(Size.Full()).Width(Size.Full());
    }

    private object BuildFileInputDemo(IState<string> fileInput, string? detectedMimeType)
    {
        return Layout.Vertical().Gap(3)
            | Text.H3("File Extension to MIME Type Detection")
            | Text.Block("Enter a file name, extension, or full path to detect its MIME type:")
            | fileInput.ToInput(placeholder: "e.g., image.jpg, document.pdf, archive.zip")
            | (detectedMimeType != null ? 
                new Card(
                    Layout.Vertical().Gap(2)
                    | Text.H4("Detection Result:")
                    | Text.Block($"File: {fileInput.Value}")
                    | Text.Block($"MIME Type: {detectedMimeType}")
                    | (detectedMimeType == MimeUtility.UnknownMimeType ? 
                        Text.Muted("Unknown file type - returns default application/octet-stream") :
                        Text.Success(" Known file type detected"))
                ) :
                Text.Muted("Enter a file name above to see the MIME type detection"))
            | new Separator()
            | Text.H4("Try these examples:")
            | Layout.Grid().Columns(2).Gap(1)
                | new Button("image.png", onClick: () => fileInput.Set("image.png"))
                | new Button("document.pdf", onClick: () => fileInput.Set("document.pdf"))
                | new Button("archive.zip", onClick: () => fileInput.Set("archive.zip"))
                | new Button("data.json", onClick: () => fileInput.Set("data.json"))
                | new Button("video.mp4", onClick: () => fileInput.Set("video.mp4"))
                | new Button("unknown.xyz", onClick: () => fileInput.Set("unknown.xyz"));
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
                    .Width(Size.Full())
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
