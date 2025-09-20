using System.Data;

namespace IvyClosedXml.Apps.Views
{
    /// <summary>
    /// UI component for editing a worksheet: add columns, edit rows, and persist changes.
    /// </summary>

    /// <summary>
    /// Initializes a new instance bound to the provided DataTable.
    /// </summary>
    /// <param name="table">The data table being edited.</param>

    /// <summary>
    /// Gets the DataTable backing the editor.
    /// </summary>

    /// <summary>
    /// Builds the worksheet header editor view (column add controls + row editor + file view).
    /// </summary>
    /// <returns>The composed UI object for the editor.</returns>

    /// <summary>
    /// Maps a simple type keyword to a CLR Type supported for new columns.
    /// </summary>
    /// <param name="selectedType">The textual type identifier (e.g. int, string).</param>
    /// <returns>The corresponding CLR Type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the type keyword is unsupported.</exception>
    public class WorksheetHeaderEditor : ViewBase
    {
        public WorksheetHeaderEditor(DataTable table)
        {
            Table = table;
        }

        private DataTable Table { get; }

        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            var workbookConnection = UseService<WorkbookConnection>();
            var workbookRepository = workbookConnection.GetWorkbookRepository();

            var columnName = UseState((string?)null);

            var columnTypes = new string[] { "int", "double", "decimal", "long", "string" };
            var selectedType = UseState("int");
            var refreshToken = this.UseRefreshToken();

            var addColumnButton = new Button("Add column", _ =>
            {
                Table.Columns.Add(columnName.Value, GetColumnTypeFromString(selectedType.Value));
                refreshToken.Refresh();
            });

            var saveTable = new Button("Save table", _ =>
            {
                workbookRepository.Save(Table);
                client.Toast("Changes saved!");
            });

            var viewToReturn = Layout.Vertical()
                    | Text.Label("Select column type")
                    | selectedType.ToSelectInput(columnTypes.ToOptions()).Variant(SelectInputs.Select)
                    | new TextInput(columnName, placeholder: "New column name")
                    | Layout.Horizontal(
                        [
                            addColumnButton,
                        saveTable
                        ]);

            return new HeaderLayout(
                 header: new Card(viewToReturn)
                     .Title("Worksheet editor"),
                 content: Layout.Vertical()
                 | new RowEditorView(Table, refreshToken)
                 | new Card(new WorksheetFileView(Table)));
        }


        private static Type GetColumnTypeFromString(string selectedType)
        {
            switch (selectedType)
            {
                case "string":
                    return typeof(string);
                case "int":
                    return typeof(int);
                case "double":
                    return typeof(double);
                case "decimal":
                    return typeof(decimal);
                case "long":
                    return typeof(long);
                default:
                    throw new ArgumentOutOfRangeException(selectedType, "Column type not supported!");
            }
        }
    }
}