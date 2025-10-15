using Ivy.Client;
using Ivy.Core.Hooks;
using Ivy.Views.Forms;

namespace EEPlusSoftware.Apps;

[App(icon: Icons.Box, title: "EEPlus Software Demo")]
public class EEPlusDemo : ViewBase
{
    private static string GetExcelFilePath() =>
       System.IO.Path.Combine(System.IO.Path.GetTempPath(), "books.xlsx");

    private static void EnsureExcelFileExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            using var package = new ExcelPackage();
            package.Workbook.Worksheets.Add("Books");
            package.SaveAs(new FileInfo(filePath));
        }
    }
    public override object? Build()
    {
        var filePath = GetExcelFilePath();
        var client = UseService<IClientProvider>();

        // Ensure file exists
        EnsureExcelFileExists(filePath);

        var booksState = UseState<List<Book>>(() => ExcelManipulation.ReadExcel());


        var downloadUrl = this.UseDownload(
        async () =>
        {
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
                   new Button("Generate Excel File").HandleClick(_ => ExcelManipulation.WriteExcel(booksState)).Loading(loading).Disabled(loading)
            | booksState.Value.ToTable()
               .Width(Size.Full())
                .Builder(p => p.Title, f => f.Text())
                .Builder(p => p.Author, f => f.Text())
                .Builder(p => p.Year, f => f.Default())
           | downloadBtn | new Button("Delete All Records").HandleClick(_ => HandleDeleteAsync(booksState, filePath, client))
            .Loading(loading).Disabled(loading)
           | formView | new Button("Save Book").HandleClick(async _ => await HandleSubmitAsync(booksState, client, book, onSubmit))
                    .Loading(loading).Disabled(loading)
                | validationView;


    }

    #region Handle
    async ValueTask HandleSubmitAsync(IState<List<Book>> booksState, IClientProvider client, IState<Book> book, Func<Task<bool>> onSubmit)
    {
        try
        {
            if (await onSubmit())
            {
                var filePath = GetExcelFilePath();
                EnsureExcelFileExists(filePath);

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets[0];
                    int nextRow = ws.Dimension == null ? 2 : ws.Dimension.End.Row + 1;
                    var bookRecord = new Book(book.Value.Title, book.Value.Author, book.Value.Year);

                    ws.Cells[nextRow, 1].Value = (nextRow - 1).ToString();
                    ws.Cells[nextRow, 2].Value = bookRecord.Title;
                    ws.Cells[nextRow, 3].Value = bookRecord.Author;
                    ws.Cells[nextRow, 4].Value = bookRecord.Year;
                    ws.Cells[1, 1, nextRow, 4].AutoFitColumns();

                    package.Save();
                }

                booksState.Value = ExcelManipulation.ReadExcel();
                book.Value = new Book();
                client.Toast("Book added!");
            }
        }
        catch (IOException ex)
        {
            client.Toast($"File access error. Please try again. Technical error: {ex.Message}");
            // Log ex.Message or ex.ToString() as needed
        }
        catch (Exception ex)
        {
            client.Toast($"An unexpected error occurred. Technical error: {ex.Message}");
            // Log ex.Message or ex.ToString() as needed
        }
    }

    void HandleDeleteAsync(IState<List<Book>> booksState, string filePath, IClientProvider client)
    {
        try
        {
            EnsureExcelFileExists(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                client.Toast("Worksheet not found.");
                return;
            }

            var lastRow = ws.Dimension?.End.Row ?? 0;
            if (lastRow <= 1)
            {
                client.Toast("No records to clear.");
                return;
            }

            ws.DeleteRow(2, lastRow - 1);
            ws.Cells[1, 1, 1, 4].AutoFitColumns();
            package.Save();

            client.Toast("All records cleared.");
            booksState.Value = ExcelManipulation.ReadExcel();
        }
        catch (IOException ex)
        {
            client.Toast($"File access error. Please try again. Technical error: {ex.Message}");
        }
        catch (Exception ex)
        {
            client.Toast($"An unexpected error occurred. Technical error: {ex.Message}");
        }
    }
    #endregion Handle

}