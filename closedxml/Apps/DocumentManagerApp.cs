using ClosedXmlExample.Models;
using ClosedXmlExample.Services;

namespace ClosedXmlExample.Apps;

/// <summary>
/// Document Manager - manage Excel documents metadata
/// </summary>
[App(icon: Icons.Database, title: "Document Manager")]
public class DocumentManagerApp : ViewBase
{
    public override object? Build()
    {
        var documents = UseState(() => new List<Document>());
        var searchTerm = UseState(() => "");
        var selectedDocument = UseState(() => (Document?)null);
        var loading = UseState(() => false);
        var showCreateDialog = UseState(() => false);
        
        // Form fields for creating/editing
        var fileName = UseState(() => "");
        var description = UseState(() => "");
        var author = UseState(() => "");
        var sheetCount = UseState(() => 1);
        var fileSize = UseState(() => 0L);

        // Load documents on startup
        UseEffect(() =>
        {
            LoadDocuments();
        }, []);

        void LoadDocuments()
        {
            loading.Value = true;
            try
            {
                documents.Value = DocumentStorage.GetAll();
            }
            finally
            {
                loading.Value = false;
            }
        }

        void SearchDocuments()
        {
            loading.Value = true;
            try
            {
                documents.Value = DocumentStorage.Search(searchTerm.Value);
            }
            finally
            {
                loading.Value = false;
            }
        }

        void CreateDocument()
        {
            if (string.IsNullOrWhiteSpace(fileName.Value))
            {
                return;
            }

            loading.Value = true;
            try
            {
                var newDoc = new Document
                {
                    FileName = fileName.Value,
                    Description = description.Value,
                    Author = author.Value,
                    SheetCount = sheetCount.Value,
                    FileSize = fileSize.Value
                };

                DocumentStorage.Create(newDoc);
                LoadDocuments();
                
                // Reset form
                fileName.Value = "";
                description.Value = "";
                author.Value = "";
                sheetCount.Value = 1;
                fileSize.Value = 0;
                showCreateDialog.Value = false;
            }
            finally
            {
                loading.Value = false;
            }
        }

        void DeleteDocument(Document doc)
        {
            loading.Value = true;
            try
            {
                var success = DocumentStorage.Delete(doc.Id);
                if (success)
                {
                    LoadDocuments();
                    if (selectedDocument.Value?.Id == doc.Id)
                    {
                        selectedDocument.Value = null;
                    }
                }
            }
            finally
            {
                loading.Value = false;
            }
        }

        void ViewDocument(Document doc)
        {
            DocumentStorage.UpdateLastAccessed(doc.Id);
            selectedDocument.Value = doc;
        }

        // Create dialog
        var createDialog = showCreateDialog.Value ? 
            new Card(
                Layout.Vertical().Gap(4).Padding(4)
                | Text.H3("Create New Document")
                | fileName.ToTextInput(placeholder: "File Name")
                | description.ToTextInput(placeholder: "Description")
                | author.ToTextInput(placeholder: "Author")
                | Layout.Horizontal().Gap(2)
                    | new Button("Create").HandleClick(() => CreateDocument())
                    | new Button("Cancel").Variant(ButtonVariant.Secondary)
                        .HandleClick(() => showCreateDialog.Value = false)
            )
            : null;

        // Document list
        var documentListView = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H3($"Documents ({DocumentStorage.Count()})")
            | Layout.Horizontal().Gap(2)
                | searchTerm.ToTextInput(placeholder: "Search...")
                | new Button("Search").HandleClick(() => SearchDocuments())
                | new Button("Refresh").HandleClick(() => LoadDocuments())
                | new Button("+ New").Variant(ButtonVariant.Primary)
                    .HandleClick(() => showCreateDialog.Value = true)
            | (createDialog != null ? createDialog : (object)Text.Small(""))
            | (documents.Value.Any() ?
                Layout.Vertical().Gap(2)
                | documents.Value.Select(doc => 
                    new Card(
                        Layout.Horizontal().Gap(4).Padding(2)
                        | Layout.Vertical().Gap(1).Width(Size.Full())
                            | Text.Label(doc.FileName)
                            | Text.Small($"{doc.Description}")
                            | Text.Small($"Author: {doc.Author} | Sheets: {doc.SheetCount} | Size: {doc.FileSizeFormatted}")
                            | Text.Small($"Created: {doc.CreatedAt:yyyy-MM-dd HH:mm}")
                        | Layout.Horizontal().Gap(2)
                            | new Button("View").HandleClick(() => ViewDocument(doc))
                            | new Button("Delete").Variant(ButtonVariant.Destructive)
                                .HandleClick(() => DeleteDocument(doc))
                    ).Width(Size.Full())
                ).ToArray()
                :
                Text.Block("No documents found. Click '+ New' to create one.")
            )
        ).Height(Size.Full());

        // Document detail view
        var detailView = selectedDocument.Value != null ?
            new Card(
                Layout.Vertical().Gap(4).Padding(4)
                | Text.H3(selectedDocument.Value.FileName)
                | Layout.Vertical().Gap(2)
                    | Text.Block($"📄 File Name: {selectedDocument.Value.FileName}")
                    | Text.Block($"📝 Description: {selectedDocument.Value.Description}")
                    | Text.Block($"👤 Author: {selectedDocument.Value.Author}")
                    | Text.Block($"📊 Sheet Count: {selectedDocument.Value.SheetCount}")
                    | Text.Block($"💾 File Size: {selectedDocument.Value.FileSizeFormatted}")
                    | Text.Block($"🕒 Created: {selectedDocument.Value.CreatedAt:yyyy-MM-dd HH:mm}")
                    | Text.Block($"🔄 Updated: {selectedDocument.Value.UpdatedAt:yyyy-MM-dd HH:mm}")
                    | (selectedDocument.Value.LastAccessedAt.HasValue 
                        ? Text.Block($"👁️ Last Accessed: {selectedDocument.Value.LastAccessedAt:yyyy-MM-dd HH:mm}")
                        : (object)Text.Small(""))
                | new Button("Close").HandleClick(() => selectedDocument.Value = null)
            ).Height(Size.Full())
            :
            new Card(
                Layout.Vertical().Gap(4).Padding(4)
                | Layout.Center()
                | Text.H3("No document selected")
                | Text.Block("Select a document from the list to view details.")
            ).Height(Size.Full());

        return new SidebarLayout(
            mainContent: detailView,
            sidebarContent: documentListView,
            sidebarHeader: Layout.Vertical().Gap(1)
                | Text.Lead("Document Manager")
                | Text.Small("Simple in-memory database for Excel documents")
        );
    }
}

