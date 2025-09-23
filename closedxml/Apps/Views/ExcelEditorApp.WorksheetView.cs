using System.Data;

namespace IvyClosedXml.Apps.Views
{
    /// <summary>
    /// Represents a view for rendering a worksheet file as a table, constructed from a provided DataTable.
    /// This class inherits from ViewBase and overrides the Build method to generate a Table object
    /// containing headers derived from the DataTable's column names and rows populated with the data.
    /// </summary>
    public class WorksheetFileView : ViewBase
    {
        public WorksheetFileView(DataTable table)
        {
            Table = table;
        }

        public DataTable Table { get; }

        public override object? Build()
        {
            var rows = Table.AsEnumerable().ToList();
            var tableColumns = Table.Columns.Cast<DataColumn>().ToList();
            var listOfTableRows = new List<TableRow>();
            var cols = new List<TableCell>();

            foreach (var col in tableColumns)
                cols.Add(new TableCell(col.ColumnName).IsHeader());

            listOfTableRows.Add(new TableRow([.. cols]));

            foreach (var row in rows)
            {
                var listOfTableCells = new List<TableCell>();
                foreach (var rowValue in row.ItemArray)
                {
                    listOfTableCells.Add(new TableCell(rowValue.ToString()));
                }

                listOfTableRows.Add(new TableRow([.. listOfTableCells]));
            }
            return new Table([.. listOfTableRows]);
        }
    }
}