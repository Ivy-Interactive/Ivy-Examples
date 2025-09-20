using IvyClosedXml.Apps.Views;

namespace IvyClosedXml.Apps;

/// <summary>
/// Represents an Excel editor application that provides a user interface for creating and editing Excel files.
/// It features a header with a file creation dialog and a content area displaying Excel file sheets.
/// </summary>
[App(icon: Icons.ChartBar, path: ["Apps"])]
public class ExcelEditorApp : ViewBase
{
    public override object? Build()
    {
        var refreshToken = this.UseRefreshToken();
        return new HeaderLayout(
         header: new Card(new FileCreateDialog(refreshToken))
             .Title("Excel file editor"),
         content: Layout.Vertical().Gap(1)
             | new ExcelFileSheets()
      );
    }
}
