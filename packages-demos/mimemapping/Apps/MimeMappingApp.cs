namespace MimeMappingExample;

[App(icon: Icons.FileText, title: "MimeMapping")]
public class MimeMappingApp : ViewBase
{
    private enum InputMethod { UploadFile, EnterFileName }

    public override object? Build()
    {
        var inputMethod = this.UseState(InputMethod.UploadFile);
        var fileInput = this.UseState<string>();
        var uploadState = this.UseState<FileUpload<byte[]>?>();
        var uploadContext = this.UseUpload(MemoryStreamUploadHandler.Create(uploadState))
            .Accept("*/*")
            .MaxFileSize(50 * 1024 * 1024);
        var mimeTypeInput = this.UseState<string>();
        var searchQuery = this.UseState<string>();
        var currentPage = this.UseState(1);
        const int itemsPerPage = 8;

        // Reset to page 1 when search query changes
        UseEffect(() =>
        {
            currentPage.Set(1);
        }, [searchQuery]);

        var currentFileName = inputMethod.Value == InputMethod.UploadFile ? uploadState.Value?.FileName : fileInput.Value;
        var detectedMimeType = currentFileName != null
            ? MimeUtility.GetMimeMapping(currentFileName)
            : null;

        var extensions = !string.IsNullOrEmpty(mimeTypeInput.Value)
            ? MimeUtility.GetExtensions(mimeTypeInput.Value)
            : null;

        // Get all filtered types
        var allFilteredTypes = string.IsNullOrEmpty(searchQuery.Value)
            ? MimeUtility.TypeMap.ToList()
            : MimeUtility.TypeMap.Where(kvp =>
                kvp.Key.Contains(searchQuery.Value, StringComparison.OrdinalIgnoreCase) ||
                kvp.Value?.Contains(searchQuery.Value, StringComparison.OrdinalIgnoreCase) == true).ToList();

        // Get paginated types
        var totalItems = allFilteredTypes.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
        
        // Ensure current page is valid
        if (currentPage.Value < 1)
        {
            currentPage.Set(1);
        }
        else if (totalPages > 0 && currentPage.Value > totalPages)
        {
            currentPage.Set(totalPages);
        }
        
        var filteredTypes = allFilteredTypes.Skip((currentPage.Value - 1) * itemsPerPage).Take(itemsPerPage);

        return Layout.Vertical()
            | Text.H2("MimeMapping Library Demo")
            | Text.Muted("Detect MIME types from file extensions, search and browse all supported types, and perform reverse lookup to find file extensions by MIME type. Upload files or enter file names to see real-time detection.")

            // Tab navigation
            | Layout.Tabs(
                new Tab("Detect Type", BuildFileInputDemo(inputMethod, fileInput, uploadState, uploadContext, currentFileName, detectedMimeType)),
                new Tab("Browse Types", BuildBrowseTypesDemo(searchQuery, filteredTypes, currentPage, totalPages, totalItems)),
                new Tab("Reverse Lookup", BuildReverseLookupDemo(mimeTypeInput, extensions))
            ).Variant(TabsVariant.Tabs)

            | new Spacer()
            | Text.Small("This demo uses MimeMapping library for detecting MIME types from file extensions.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [MimeMapping](https://github.com/zone117x/MimeMapping)")
            ;
    }

    private object BuildFileInputDemo(IState<InputMethod> inputMethod, IState<string> fileInput, IState<FileUpload<byte[]>?> uploadState, IState<UploadContext> uploadContext, string? currentFileName, string? detectedMimeType)
    {
        // Validation
        string? fileInputError = null;
        string? fileUploadError = null;
        
        if (inputMethod.Value == InputMethod.UploadFile && uploadState.Value != null)
        {
            var detected = MimeUtility.GetMimeMapping(uploadState.Value.FileName);
            if (detected == MimeUtility.UnknownMimeType)
            {
                fileUploadError = "Unknown file type - returns default application/octet-stream";
            }
        }
        else if (inputMethod.Value == InputMethod.EnterFileName && !string.IsNullOrEmpty(fileInput.Value))
        {
            var detected = MimeUtility.GetMimeMapping(fileInput.Value);
            if (detected == MimeUtility.UnknownMimeType)
            {
                fileInputError = "Unknown file type - returns default application/octet-stream";
            }
        }
        
        // Check if inputs have values
        bool hasUploadValue = inputMethod.Value == InputMethod.UploadFile && uploadState.Value != null;
        bool hasInputValue = inputMethod.Value == InputMethod.EnterFileName && !string.IsNullOrEmpty(fileInput.Value);
        
        object inputSection = inputMethod.Value == InputMethod.UploadFile
            ? Layout.Vertical()
                | Text.Label("Choose File")
                | uploadState.ToFileInput(uploadContext)
                    .Invalid(fileUploadError)
            : Layout.Vertical()
                | Text.Label("Enter File Name")
                | fileInput.ToInput(placeholder: "e.g., image.jpg, document.pdf, archive.zip")
                    .Invalid(fileInputError);

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
            | Text.H3("Detection Result")
            | (detectedMimeType != null && currentFileName != null && (hasUploadValue || hasInputValue) && fileUploadError == null && fileInputError == null
                ? Layout.Vertical()
                | Text.Muted("To detect a different file, simply select or enter a new file name")
                | new Card(
                    new { File = currentFileName, MimeType = detectedMimeType }
                        .ToDetails()
                        .Builder(x => x.MimeType, b => b.CopyToClipboard())
                        .Builder(x => x.File, b => b.CopyToClipboard())
                )
                : Text.Muted("Enter a file name or select a file above to see the MIME type detection")
            ));
    }

    private object BuildBrowseTypesDemo(IState<string> searchQuery, IEnumerable<KeyValuePair<string, string?>> filteredTypes, IState<int> currentPage, int totalPages, int totalItems)
    {
        return Layout.Vertical().Gap(3)
            | Text.H3("Browse Available MIME Types")
            | Text.Muted($"Showing {filteredTypes.Count()} of {totalItems} types")
            | searchQuery.ToInput(placeholder: "Search by extension or MIME type...")
            | new Card(filteredTypes.ToTable().Width(Size.Full()))
            | (totalPages > 1
                ? new Pagination(currentPage.Value, totalPages, evt => currentPage.Set(evt.Value))
                : null);
            
    }

    private object BuildReverseLookupDemo(IState<string> mimeTypeInput, string[]? extensions)
    {
        // Validation
        string? mimeTypeError = null;
        bool isValidInput = !string.IsNullOrEmpty(mimeTypeInput.Value);
        bool hasValidExtensions = extensions != null && extensions.Length > 0;

        if (isValidInput)
        {
            // Validate MIME type format (should contain "/")
            if (!mimeTypeInput.Value.Contains('/'))
            {
                mimeTypeError = "Invalid MIME type format. Expected format: type/subtype (e.g., image/jpeg)";
            }
            else if (isValidInput && mimeTypeError == null && !hasValidExtensions)
            {
                mimeTypeError = "This MIME type is not recognized or has no associated extensions";
            }
        }
        
        // Determine if we should show results
        bool showResults = isValidInput && mimeTypeError == null && hasValidExtensions;

        return Layout.Horizontal().Gap(5)
            | new Card(
                Layout.Vertical().Gap(5)
                | Text.H3("MIME Type to Extensions Lookup")
                | Text.Muted("Enter a MIME type to find all associated file extensions")
                | mimeTypeInput.ToInput(placeholder: "e.g., image/jpeg, application/pdf, text/html")
                    .Invalid(mimeTypeError)
                )
            
            | new Card(
                Layout.Vertical().Gap(5)
                | Text.H3("Lookup Result")
                | Text.Muted("Results will appear here when a valid MIME type is entered")
                | (showResults
                    ? new Card(
                        Layout.Vertical()
                        | Text.Muted($"Found {extensions.Length} extension(s)")
                        | (Layout.Horizontal().Gap(1)
                            | extensions!.Select(ext => new Badge(ext)).ToArray())
                        ) : null
                )
            );
    }
}
