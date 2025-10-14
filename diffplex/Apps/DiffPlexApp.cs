using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace DiffPlexExample.Apps;

[App(icon: Icons.Diff, title: "DiffPlex")]
public class DiffPlexApp : ViewBase
{
    private const float MainCardWidthFraction = 0.8f;

    public override object? Build()
    {
        // States
        var leftText = this.UseState(() => "The quick brown fox jumps over the lazy dog.\nThis is line two.\nThis is line three.");
        var rightText = this.UseState(() => "The quick brown cat jumps over the lazy dog.\nThis is line two modified.\nThis is line three.\nThis is a new line four.");
        var diffResult = this.UseState<SideBySideDiffModel?>(() => null);
        var ignoreWhitespace = this.UseState(() => false);
        var ignoreCase = this.UseState(() => false);

        // Handler
        void compareDiff()
        {
            var diffBuilder = new SideBySideDiffBuilder(new Differ());
            diffResult.Value = diffBuilder.BuildDiffModel(
                leftText.Value ?? "", 
                rightText.Value ?? "",
                ignoreWhitespace.Value,
                ignoreCase.Value
            );
        }

        // Left card with code input (Original)
        var leftCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Original Text")
            | leftText.ToCodeInput(placeholder: "Enter original text here...").Height(Size.Units(50));

        // Right card with code input (Modified)
        var rightCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Modified Text")
            | rightText.ToCodeInput(placeholder: "Enter modified text here...").Height(Size.Units(50));

        // Comparison controls
        var controls =
            Layout.Horizontal().Gap(3)
            | ignoreWhitespace.ToBoolInput(variant: BoolInputs.Checkbox).Label("Ignore Whitespace")
            | ignoreCase.ToBoolInput(variant: BoolInputs.Checkbox).Label("Ignore Case")
            | new Button("Compare Texts", onClick: compareDiff).Primary().Icon(Icons.GitCompare)
            | new Button("Clear", onClick: () => diffResult.Value = null).Variant(ButtonVariant.Secondary).Icon(Icons.X);

        // State for diff results display
        var diffDisplayText = UseState("");
        
        // Update diff display when result changes
        if (diffResult.Value != null)
        {
            var diffLines = new List<string>();
            
            // Use the NewText pane which has all lines including imaginary ones
            foreach (var line in diffResult.Value.NewText.Lines)
            {
                var prefix = line.Type switch
                {
                    ChangeType.Inserted => "+ ",
                    ChangeType.Deleted => "- ",
                    ChangeType.Modified => "~ ",
                    ChangeType.Imaginary => "- ",  // Deleted lines from original (shown as empty in new)
                    _ => "  "
                };
                
                var lineText = line.Type == ChangeType.Imaginary ? "" : (line.Text ?? "");
                diffLines.Add(prefix + lineText);
            }
            
            diffDisplayText.Value = string.Join("\n", diffLines);
        }
        
        // Comparison results card
        var resultsCard = diffResult.Value != null
            ? (object)(Layout.Vertical().Gap(3).Padding(2)
                | Text.H4("Comparison Results")
                | diffDisplayText.ToCodeInput()
                    .Height(Size.Units(50))
                    .Language(Languages.Text)
                    .Disabled()
                    .ShowCopyButton())
            : (object)(Layout.Vertical().Gap(3).Padding(2)
                | Text.H4("Comparison Results")
                | Text.Muted("Click Compare to see differences here..."));

        // Main content layout
        var mainContent =
            Layout.Vertical().Gap(6).Padding(2)
            | Text.H3("DiffPlex Text Comparison")
            | Text.Block("Enter original text on the left, modified text in the middle, and see the differences on the right.")
            | (Layout.Horizontal().Gap(4).Grow()
                | new Card(leftCard).Width(Size.Fraction(0.33f))
                | new Card(rightCard).Width(Size.Fraction(0.33f))
                | new Card(resultsCard).Width(Size.Fraction(0.33f)))
            | controls
            | new Spacer()
            | Text.Small("This demo uses the DiffPlex NuGet package for text comparison.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [DiffPlex](https://github.com/mmanela/diffplex)");
                

        // Outer card for consistent width
        return new Card(mainContent).Height(Size.Fit().Min(Size.Full()));
    }
}