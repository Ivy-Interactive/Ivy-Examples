using Ivy.Client;
using Ivy.Core.Hooks;
using Ivy.Views.Forms;

namespace EEPlusSoftware.Apps;

[App(icon: Icons.Box, title: "EEPlus Software Demo")]
public class EEPlusDemo : ViewBase
{
    public override object? Build()
    {
        var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "books.xlsx");
        var client = UseService<IClientProvider>();

        // Ensure file exists
        if (!File.Exists(filePath))
        {
            using var package = new ExcelPackage();
            package.Workbook.Worksheets.Add("Books");
            package.SaveAs(new FileInfo(filePath));
        }

        var booksState = UseState<List<Book>>(() => ExcelManipulation.ReadExcel());
        booksState.Value = ExcelManipulation.ReadExcel();


        var downloadUrl = this.UseDownload(
        async () =>
        {
            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "books.xlsx");

            // Read the file into memory
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            // Return as bytes
            return fileBytes;
        },
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    $"books-{DateTime.UtcNow:yyyy-MM-dd}.xlsx"
       );

        var downloadBtn = new Button("Download Excel")
            .Primary().Icon(Icons.Download)
            .Url(downloadUrl.Value);

        var book = UseState(() => new Book());
        var formBuilder = book.ToForm().Remove(x => x.ID)
            .Label(m => m.Title, "Title")
            .Builder(m => m.Title, s => s.ToTextInput().Placeholder("Insert title..."))
            .Builder(m => m.Author, s => s.ToTextInput().Placeholder("Insert author..."))
            .Builder(m => m.Year, s => s.ToNumberInput().Placeholder("Insert year...").Min(0))
            .Required(m => m.Title, m => m.Author, m => m.Year);

        var (onSubmit, formView, validationView, loading) = formBuilder.UseForm(this.Context);

        return Layout.Vertical()
            | Text.H2("EEPlus Software Demo") |
                   new Button("Generate Excel File").HandleClick(async _ => ExcelManipulation.WriteExcel(booksState)).Loading(loading).Disabled(loading)
            | booksState.Value.ToTable()
               .Width(Size.Full())
                .Builder(p => p.Title, f => f.Text())
                .Builder(p => p.Author, f => f.Text())
                .Builder(p => p.Year, f => f.Default())
           | downloadBtn | new Button("Delete All Records").HandleClick(async _ => HandleDelete(booksState, filePath, client))
            .Loading(loading).Disabled(loading)
           | formView | new Button("Save Book").HandleClick(_ => HandleSubmit(booksState, client, book, onSubmit))
                    .Loading(loading).Disabled(loading)
                | validationView;


    }

    #region Handle
    async ValueTask HandleSubmit(IState<List<Book>> booksState, IClientProvider client, IState<Book> book, Func<Task<bool>> onSubmit)
    {
        if (await onSubmit())
        {
            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "books.xlsx");

            // Ensure file exists
            if (!File.Exists(filePath))
            {
                using var package = new ExcelPackage();
                package.Workbook.Worksheets.Add("Books");
                package.SaveAs(new FileInfo(filePath));
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var ws = package.Workbook.Worksheets[0];

                // Calculate next row
                int nextRow;
                if (ws.Dimension == null)
                {
                    // Sheet is empty → start at row 2 (row 1 is header)
                    nextRow = 2;
                }
                else
                {
                    nextRow = ws.Dimension.End.Row + 1;
                }
                var bookRecord = new Book(book.Value.Title, book.Value.Author, book.Value.Year);

                ws.Cells[nextRow, 1].Value = (nextRow - 1).ToString(); // ID
                ws.Cells[nextRow, 2].Value = bookRecord.Title;
                ws.Cells[nextRow, 3].Value = bookRecord.Author;
                ws.Cells[nextRow, 4].Value = bookRecord.Year;

                ws.Cells[1, 1, nextRow, 4].AutoFitColumns();

                package.Save();
            }
        }

        // refresh reactive books list
        booksState.Value = ExcelManipulation.ReadExcel();

        // clear the form state (reset book model)
        book.Value = new Book();
        client.Toast("Book added!");
    }
    async ValueTask HandleDelete(IState<List<Book>> booksState, string filePath, IClientProvider client)
    {

        if (!File.Exists(filePath))
        {
            // Nothing to clear
            client.Toast("No Excel file found to clear.");
            return;
        }


        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws == null)
        {
            client.Toast("Worksheet not found.");
            return;
        }

        // If there is no data (only headers or empty)
        var lastRow = ws.Dimension?.End.Row ?? 0;
        if (lastRow <= 1)
        {
            client.Toast("No records to clear.");
            return;
        }

        // Clear rows 2..lastRow (keep header in row 1)
        ws.DeleteRow(2, lastRow - 1);

        //autosize columns after deletion
        ws.Cells[1, 1, 1, 4].AutoFitColumns();

        package.Save();

        client.Toast("All records cleared.");
        // refresh reactive books list
        booksState.Value = ExcelManipulation.ReadExcel();
    }
    #endregion Handle

}