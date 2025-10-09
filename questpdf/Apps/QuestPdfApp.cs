using QuestPdfDemo.Models;
using QuestPdfDemo.Services;

namespace QuestPdfDemo.Apps;

[App(icon: Icons.FileDown, title: "QuestPDF", path: ["Apps"])]
public class QuestPdfApp : ViewBase
{
  private readonly PdfGenerationService _pdfService;
  
  public QuestPdfApp()
  {
    _pdfService = new PdfGenerationService();
  }
  
  public override object? Build()
  {
    var title = UseState("Resume");
    var body = UseState(LoadEmbedded("Assets.Resume.md"));
    var pageSize = UseState("A4");
    var landscape = UseState(false);
    var margins = UseState(30);
    var insertType = UseState("Heading 1");
    
    void InsertSnippetByType(string type)
    {
      string snippet = type switch
      {
        "Heading 1" => "# ",
        "Heading 2" => "## ",
        "Heading 3" => "### ",
        "Bullet"    => "- ",
        "Numbered"  => "1. ",
        "Quote"     => "> ",
        "Checkbox"  => "- [ ] ",
        "Table"     => "|  |  |\n|  |  |",
        _ => string.Empty
      };
      var current = body.Value ?? string.Empty;
      var sep = current.EndsWith("\n") || current.Length == 0 ? "" : "\n";
      body.Set(current + sep + snippet + "\n");
    }
    
    byte[] GeneratePdf()
    {
      var settings = new PdfSettings
      {
        PageSize = pageSize.Value,
        Landscape = landscape.Value,
        Margins = margins.Value
      };
      
      return _pdfService.GeneratePdf(title.Value, body.Value ?? string.Empty, settings);
    }
    
    var downloadUrl = this.UseDownload(() => GeneratePdf(), "application/pdf", "report.pdf");
    
    var previewBytes = UseState<byte[]?>(() => GeneratePdf());
    this.UseEffect(async () =>
    {
      await Task.Delay(350);
      previewBytes.Set(GeneratePdf());
    }, title, body, pageSize, margins, landscape);
    
    var previewDataUrl = previewBytes.Value is null
      ? string.Empty
      : "data:application/pdf;base64," + Convert.ToBase64String(previewBytes.Value) + "#zoom=page-width&toolbar=0&navpanes=0";
    
    long refreshToken = 0;
    if (previewBytes.Value is not null)
    {
      var h = System.Security.Cryptography.SHA256.HashData(previewBytes.Value);
      refreshToken = Math.Abs(BitConverter.ToInt64(h, 0));
    }
    
    var leftForm = new Card(
          Layout.Vertical().Gap(6).Padding(3)
          | Text.H2("QuestPDF Demo")
          | Text.Muted("Generate a simple PDF document")
          | title.ToInput(placeholder: "Title")
            | (
              Layout.Horizontal().Gap(3)
              | new Button($"Page: {pageSize.Value}").Outline().Width(30).Icon(Icons.ChevronDown).WithDropDown(
                    MenuItem.Default("A4").HandleSelect(() => pageSize.Value = "A4"),
                    MenuItem.Default("Letter").HandleSelect(() => pageSize.Value = "Letter")
                )
              | new Button(landscape.Value ? "Landscape" : "Portrait").Outline().Width(30).Icon(Icons.ChevronDown).WithDropDown(
                    MenuItem.Default("Portrait").HandleSelect(() => landscape.Value = false),
                    MenuItem.Default("Landscape").HandleSelect(() => landscape.Value = true)
                )
              | new Button($"Margins: {margins.Value}").Outline().Width(35).Icon(Icons.ChevronDown).WithDropDown(
                    MenuItem.Default("15").HandleSelect(() => margins.Value = 15),
                    MenuItem.Default("30").HandleSelect(() => margins.Value = 30),
                    MenuItem.Default("50").HandleSelect(() => margins.Value = 50)
                )
              | new Button($"Type: {insertType.Value}").Outline().Width(45).Icon(Icons.ChevronDown).WithDropDown(
                    MenuItem.Default("Heading 1").HandleSelect(() => { insertType.Value = "Heading 1"; InsertSnippetByType("Heading 1"); }),
                    MenuItem.Default("Heading 2").HandleSelect(() => { insertType.Value = "Heading 2"; InsertSnippetByType("Heading 2"); }),
                    MenuItem.Default("Heading 3").HandleSelect(() => { insertType.Value = "Heading 3"; InsertSnippetByType("Heading 3"); }),
                    MenuItem.Default("Bullet").HandleSelect(() => { insertType.Value = "Bullet"; InsertSnippetByType("Bullet"); }),
                    MenuItem.Default("Numbered").HandleSelect(() => { insertType.Value = "Numbered"; InsertSnippetByType("Numbered"); }),
                    MenuItem.Default("Quote").HandleSelect(() => { insertType.Value = "Quote"; InsertSnippetByType("Quote"); }),
                    MenuItem.Default("Checkbox").HandleSelect(() => { insertType.Value = "Checkbox"; InsertSnippetByType("Checkbox"); }),
                    MenuItem.Default("Table").HandleSelect(() => { insertType.Value = "Table"; InsertSnippetByType("Table"); })
                )
              | new Button("Download").Primary().Icon(Icons.Download).Url(downloadUrl.Value)
            )
          | body.ToCodeInput().Language(Languages.Markdown).Width(Ivy.Shared.Size.Full()).Height(Ivy.Shared.Size.Units(90)).Placeholder("Body (Markdown)")
          | Text.Markdown("This demo uses the QuestPDF NuGet package to generate PDFs.")
          | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [QuestPDF](https://github.com/QuestPDF/QuestPDF)")
        ).Width(Ivy.Shared.Size.Fraction(0.5f)).Height(Ivy.Shared.Size.Full());
    
    var rightPreview = new Card(
          Layout.Vertical().Gap(0).Padding(0)
          | (string.IsNullOrEmpty(previewDataUrl)
              ? Text.Muted("Generating preview...")
              : new Iframe(previewDataUrl, refreshToken).Width(Ivy.Shared.Size.Screen()).Height(Ivy.Shared.Size.Screen()))
        ).Width(Ivy.Shared.Size.Fraction(0.5f)).Height(Ivy.Shared.Size.Full());
    
    return Layout.Horizontal().Gap(4)
      | leftForm
      | rightPreview;
  }
  
  private string LoadEmbedded(string name)
  {
    try
    {
      using var s = typeof(QuestPdfApp).Assembly.GetManifestResourceStream("QuestPdfDemo." + name);
      if (s == null) return string.Empty;
      using var sr = new StreamReader(s);
      return sr.ReadToEnd();
    }
    catch { return string.Empty; }
  }
}