namespace IvyClosedXml.Apps.Views
{
    /// <summary>
    /// View that constructs a tabbed header interface for every worksheet in the currently opened Excel workbook.
    /// </summary>
    //// <remarks>
    /// Each worksheet becomes a tab, initialized with a <c>WorksheetHeaderEditor</c> tied to the current table context.
    /// Returns null when no workbook/file is currently active.
    /// </remarks>

    /// <summary>
    /// Builds the tabbed layout for all worksheets in the active workbook.
    /// </summary>
    /// <returns>
    /// A tabs layout object containing one tab per worksheet, or null if no workbook is loaded.
    /// </returns>
    public class ExcelFileSheets() : ViewBase
    {
        public override object? Build()
        {
            var workbookConnection = UseService<WorkbookConnection>();
            var workbookRepository = workbookConnection.GetWorkbookRepository();

            var currentFile = workbookRepository.GetCurrentFile();

            if (currentFile == null)
                return null;

            var tabs = new List<Tab>();

            foreach (var worksheet in currentFile.Workbook.Worksheets)
            {
                var tab = new Tab(worksheet.Name, new WorksheetHeaderEditor(workbookRepository.GetCurrentTable()));
                tabs.Add(tab);
            }

            return
            Layout.Tabs([.. tabs])
            .Variant(TabsVariant.Tabs);
        }
    }
}