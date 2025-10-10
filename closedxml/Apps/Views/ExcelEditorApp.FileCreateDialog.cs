namespace ClosedXmlExample.Apps.Views
{
    /// <summary>
    /// Dialog view enabling creation, selection, and deletion of workbook files.
    /// </summary>

    /// <summary>
    /// Initializes a new instance of the FileCreateDialog with a refresh token.
    /// </summary>
    /// <param name="refreshToken">Token used to trigger UI refresh after file operations.</param>

    /// <summary>
    /// Builds the toolbar UI for file management.
    /// </summary>
    /// <returns>The composed UI object representing the dialog toolbar.</returns>

    /// <summary>
    /// Adds a new file to the repository and updates selection and state.
    /// </summary>
    /// <param name="workbookRepository">Repository handling workbook file persistence.</param>
    /// <param name="fileName">State containing the new file name entered by the user.</param>
    /// <param name="existingFiles">State list of existing file names.</param>
    /// <param name="selectedFile">State representing the currently selected file name.</param>

    /// <summary>
    /// Deletes the currently selected file (if present) and updates selection and state.
    /// </summary>
    /// <param name="workbookRepository">Repository handling workbook file persistence.</param>
    /// <param name="existingFiles">State list of existing file names.</param>
    /// <param name="selectedFile">State representing the currently selected file name.</param>

    /// <summary>
    /// Retrieves all file names from the repository.
    /// </summary>
    /// <param name="workbookRepository">Repository to query for available files.</param>
    /// <returns>A list of workbook file names.</returns>
    public class FileCreateDialog : ViewBase
    {
        public RefreshToken RefreshToken { get; }

        public FileCreateDialog(RefreshToken refreshToken)
        {
            RefreshToken = refreshToken;
        }

        public override object? Build()
        {
            var workbookConnection = UseService<WorkbookConnection>();
            var workbookRepository = workbookConnection.GetWorkbookRepository();

            var fileName = UseState((string?)null);
            var selectedCountry = this.UseState<string?>(default(string));
            var client = UseService<IClientProvider>();
            var existingFiles = UseState(GetFilesFromRepo(workbookRepository));
            var selectedFile = UseState("");

            UseEffect(() =>
            {
                try
                {
                    workbookRepository.SetCurrentFile(selectedFile.Value);
                    //call Refresh so that ExcelFileView will redraw with the newly selected file
                    RefreshToken.Refresh();
                }
                catch (Exception e)
                {
                    client.Toast(e);
                }
            }, [selectedFile]);

            var toolbar =
                Layout.Horizontal().Gap(4)
                    | fileName.ToTextInput(placeholder: "File name")
                    | new Button("Add file")
                        .Icon(Icons.Plus)
                        .Variant(ButtonVariant.Primary)
                        .HandleClick(() => HandleAddFile(workbookRepository, fileName, existingFiles, selectedFile, client))
                    | selectedFile
                    .ToSelectInput(existingFiles.Value.ToOptions(), placeholder: "Selected file")
                    .Variant(SelectInputs.Select)
                    | new Button("Delete file",
                    () => HandleDeleteFile(workbookRepository, existingFiles, selectedFile))
                    .Destructive();

            return toolbar;
        }

        private void HandleAddFile(WorkbookRepository workbookRepository, IState<string> fileName, IState<List<string>> existingFiles, IState<string> selectedFile, IClientProvider client)
        {
            if (string.IsNullOrWhiteSpace(fileName.Value))
            {
                client.Toast("❌ File name cannot be empty!");
                return;
            }

            try
            {
                workbookRepository.AddNewFile(fileName.Value);
                existingFiles.Set(GetFilesFromRepo(workbookRepository));
                if (existingFiles.Value.Count == 1)
                {
                    selectedFile.Set(existingFiles.Value[0]);
                    workbookRepository.SetCurrentFile(existingFiles.Value[0]);
                }
                selectedFile.Set(fileName.Value);
                fileName.Set(""); // Clear the input after successful add

                RefreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast($"❌ Error: {ex.Message}");
            }
        }

        private void HandleDeleteFile(WorkbookRepository workbookRepository, IState<List<string>> existingFiles, IState<string> selectedFile)
        {
            if (existingFiles.Value.Contains(selectedFile.Value))
            {
                workbookRepository.RemoveFile(selectedFile.Value);
                existingFiles.Set(GetFilesFromRepo(workbookRepository));
            }

            if (existingFiles.Value.Count >= 1)
            {
                selectedFile.Set(existingFiles.Value[0]);
                workbookRepository.SetCurrentFile(existingFiles.Value[0]);
            }
            RefreshToken.Refresh();
        }

        public List<string> GetFilesFromRepo(WorkbookRepository workbookRepository)
        {
            return workbookRepository.GetFiles().Select(f => f.FileName).ToList();
        }
    }
}