using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace DiffPlexExample.Apps;

[App(icon: Icons.FileCode, title: "DiffPlex")]
public class DiffPlexApp : ViewBase
{
    public override object? Build()
    {
        var textAState = this.UseState(() => "The quick brown fox jumps over the lazy dog.\nThis is line two.\nThis is line three.");
        var textBState = this.UseState(() => "The quick brown cat jumps over the lazy dog.\nThis is line two modified.\nThis is line three.\nThis is a new line four.");
        var diffResultState = this.UseState<DiffPaneModel?>(() => null);

        var content = new List<object>
        {
            Text.H1("DiffPlex - Text Comparison Tool"),
            Text.Block("Compare two texts and see the differences highlighted."),
            new Spacer(),
            Layout.Horizontal().Gap(4)
                .Add(Layout.Vertical().Gap(2)
                    .Add(Text.H3("Text A (Original)"))
                    .Add(textAState.ToInput()))
                .Add(Layout.Vertical().Gap(2)
                    .Add(Text.H3("Text B (Modified)"))
                    .Add(textBState.ToInput())),
            new Spacer(),
            new Button("Compare Texts", onClick: () =>
            {
                var diffBuilder = new InlineDiffBuilder(new Differ());
                diffResultState.Value = diffBuilder.BuildDiffModel(textAState.Value, textBState.Value);
            })
        };

        if (diffResultState.Value != null)
        {
            content.Add(new Separator());
            content.Add(Text.H3("Comparison Result"));
            
            var lines = new List<object>();
            foreach (var line in diffResultState.Value.Lines)
            {
                var backgroundColor = line.Type switch
                {
                    ChangeType.Inserted => "#d4edda",
                    ChangeType.Deleted => "#f8d7da",
                    ChangeType.Modified => "#fff3cd",
                    _ => "transparent"
                };

                var textColor = line.Type switch
                {
                    ChangeType.Inserted => "#155724",
                    ChangeType.Deleted => "#721c24",
                    ChangeType.Modified => "#856404",
                    _ => "#000000"
                };

                var prefix = line.Type switch
                {
                    ChangeType.Inserted => "+ ",
                    ChangeType.Deleted => "- ",
                    ChangeType.Modified => "~ ",
                    _ => "  "
                };

                lines.Add(
                    Layout.Horizontal()
                        .Add(Text.Block($"{prefix}{line.Text}"))
                );
            }

            content.Add(new Card(Layout.Vertical().Add(lines.ToArray())));
            content.Add(new Spacer());
            content.Add(
                Layout.Horizontal().Gap(2)
                    .Add(Text.Strong("Legend: "))
                    .Add(Text.Block(" + Added "))
                    .Add(Text.Block(" - Removed "))
                    .Add(Text.Block(" ~ Modified "))
            );
        }

        return Layout.Vertical().Gap(4).Padding(4).Add(content.ToArray());
    }
}