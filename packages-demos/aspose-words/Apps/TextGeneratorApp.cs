using Aspose.Words;

namespace AsposeWordsExample.Apps;

[App(icon: Icons.FileText, title: "Text to DOCX")]
public class TextGeneratorApp : ViewBase
{
    public override object? Build()
    {
        var inputText = UseState(() => "");
        var generatedDoc = UseState<Document?>(() => null);
        var isGenerating = UseState(() => false);
        var errorMessage = UseState(() => "");

        var downloadUrl = this.UseDownload(
            () =>
            {
                if (generatedDoc.Value == null)
                {
                    var emptyDoc = new Document();
                    using var stream = new MemoryStream();
                    emptyDoc.Save(stream, SaveFormat.Docx);
                    return stream.ToArray();
                }

                using var docStream = new MemoryStream();
                generatedDoc.Value.Save(docStream, SaveFormat.Docx);
                return docStream.ToArray();
            },
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"generated-text-{DateTime.Now:yyyy-MM-dd-HHmmss}.docx"
        );

        return Layout.Center()
            | (new Card(
                Layout.Vertical().Gap(3).Padding(2)
                | Text.H2("Text to DOCX")
                | Text.Block("Enter your text below and generate a Word document instantly.")
                | inputText.ToTextAreaInput(placeholder: "Type your text here...").Height(Size.Units(35))
                | new Spacer()
                | (generatedDoc.Value == null && !isGenerating.Value
                    ? new Button("Generate Document", _ =>
                    {
                        if (string.IsNullOrWhiteSpace(inputText.Value))
                        {
                            return;
                        }

                        isGenerating.Set(true);
                        errorMessage.Set("");
                        try
                        {
                            var doc = new Document();
                            var builder = new DocumentBuilder(doc);

                            // Add the user's text
                            builder.Font.Size = 12;
                            builder.Font.Name = "Arial";
                            builder.Write(inputText.Value);

                            generatedDoc.Set(doc);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.Set($"Failed to generate document: {ex.Message}");
                        }
                        finally
                        {
                            isGenerating.Set(false);
                        }
                    })
                    .Primary()
                    .Icon(Icons.Play)
                    .Disabled(string.IsNullOrWhiteSpace(inputText.Value))
                    : null!)
                | (isGenerating.Value
                    ? Text.Muted("Generating document...")
                    : null!)
                | (!string.IsNullOrEmpty(errorMessage.Value)
                    ? Text.Danger(errorMessage.Value)
                    : null!)
                | (generatedDoc.Value != null && !isGenerating.Value
                    ? new Button("Download DOCX")
                        .Primary()
                        .Icon(Icons.Download)
                        .Url(downloadUrl.Value)
                    : null!)
                | new Spacer()
                | Text.Small("This demo uses Aspose.Words for .NET to create, manipulate, and export Word documents.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Aspose.Words for .NET](https://products.aspose.com/words/net/)")
            )
            .Width(Size.Units(120).Max(600)));
    }
}