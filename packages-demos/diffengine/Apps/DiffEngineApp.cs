namespace DiffengineExample.Apps;

[App(icon: Icons.Diff, title: "DiffEngine")]
public class DiffEngineApp : ViewBase
{
    private const float MainCardWidthFraction = 0.8f;
    
    private static readonly string[] Extensions =
    {
        "txt", "json"
    };

    public override object Build()
    {
        // states
        var leftText = this.UseState("");
        var rightText = this.UseState("");
        var textExtIndex = this.UseState(0);

        var leftFile = this.UseState("");
        var rightFile = this.UseState("");
        var fileExtIndex = this.UseState(0);

        var lastLeft = this.UseState<string>();
        var lastRight = this.UseState<string>();
        var error = this.UseState<string?>();

        // handlers
        Func<Task> launchText = async () =>
        {
            try
            {
                error.Value = null;
                var pair = await DiffService.LaunchTextAsync(
                    leftText.Value, rightText.Value, Extensions[textExtIndex.Value]);
                lastLeft.Value = pair.left;
                lastRight.Value = pair.right;
            }
            catch (Exception ex)
            {
                error.Value = "Could not launch a diff tool. Install WinMerge / VS Code / Meld / KDiff3 and retry.\n" + ex.Message;
            }
        };

        Func<Task> launchFiles = async () =>
        {
            if (string.IsNullOrWhiteSpace(leftFile.Value) || string.IsNullOrWhiteSpace(rightFile.Value))
            {
                error.Value = "Pick both file paths first.";
                return;
            }
            try
            {
                error.Value = null;
                var pair = await DiffService.LaunchFilesAsync(
                    leftFile.Value, rightFile.Value, Extensions[fileExtIndex.Value]);
                lastLeft.Value = pair.left;
                lastRight.Value = pair.right;
            }
            catch (Exception ex)
            {
                error.Value = "Could not launch a diff tool. Install WinMerge / VS Code / Meld / KDiff3 and retry.\n" + ex.Message;
            }
        };

        void kill()
        {
            if (string.IsNullOrEmpty(lastLeft.Value) || string.IsNullOrEmpty(lastRight.Value)) return;
            DiffService.Kill(lastLeft.Value!, lastRight.Value!);
        }

        // text diff tab content
        var ciInfo = Text.Muted($"CI mode: {(DiffService.IsCi ? "On" : "Off")} — " +
                                   (DiffService.IsCi
                                       ? "in CI, external diff tools are not launched."
                                       : "locally you can launch external diff tools (WinMerge / VS Code, etc.)."));

        var leftCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Left")
            | leftText.ToCodeInput(placeholder: "Enter left text here...");

        var rightCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Right")
            | rightText.ToCodeInput(placeholder: "Enter right text here...");

        var textExtItems = Extensions
            .Select((ext, idx) => MenuItem.Default(ext).HandleSelect(() => textExtIndex.Value = idx))
            .ToArray();

        var textTabContent =
            Layout.Vertical().Gap(6).Padding(2)
            | Text.H3("Text Diff")
            | Text.Block("Type text on each side and choose an extension. Launch writes temp files and opens your diff tool.")
            | (Layout.Horizontal().Gap(4).Grow()
                | new Card(leftCard)
                | new Card(rightCard))
            | ciInfo
            | (Layout.Horizontal().Gap(3)
                | new Button(Extensions[textExtIndex.Value])
                    .Primary()
                    .Icon(Icons.ChevronDown)
                    .WithDropDown(textExtItems)
                | new Button("Launch Diff (Text)", onClick: () => { _ = launchText(); })
                | new Button("Kill Last Diff", onClick: () => kill()))
            | Layout.Horizontal().Gap(2)
                | Text.Markdown(string.IsNullOrEmpty(lastLeft.Value) ? "" : $"**Temp:** `{lastLeft.Value}` vs `{lastRight.Value}`")
                | new Spacer()
                | Text.Small("This demo uses the DiffEngine NuGet package to launch diff tools.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy) and [DiffEngine](https://github.com/VerifyTests/DiffEngine)");

        // file diff tab content
        var leftFileCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Left Path")
            | leftFile.ToInput(placeholder: @"e.g. C:\temp\leftPath.*");

        var rightFileCard =
            Layout.Vertical().Gap(3).Padding(2)
            | Text.H4("Right Path")
            | rightFile.ToInput(placeholder: @"e.g. C:\temp\rightPath.*");

        var fileExtItems = Extensions
            .Select((ext, idx) => MenuItem.Default(ext).HandleSelect(() => fileExtIndex.Value = idx))
            .ToArray();

        var fileExtDropdown = new Button(Extensions[fileExtIndex.Value])
            .Primary()
            .Icon(Icons.ChevronDown)
            .WithDropDown(fileExtItems);

        var fileTabContent =
            Layout.Vertical().Gap(6).Padding(2)
            | Text.H3("File Diff")
            | Text.Block("Enter two file paths (they're copied to temp), then Launch.")
            | (Layout.Horizontal().Gap(4).Grow()
                | new Card(leftFileCard)
                | new Card(rightFileCard))
            | ciInfo
            | (Layout.Horizontal().Gap(3)
                | fileExtDropdown
                | new Button("Launch Diff (Files)", onClick: () => { _ = launchFiles(); })
                | new Button("Kill Last Diff", onClick: () => kill()))
            | Layout.Horizontal().Gap(2)
                | new Spacer()
                | Text.Small("This demo uses the DiffEngine NuGet package to launch diff tools.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy) and [DiffEngine](https://github.com/VerifyTests/DiffEngine)");

        // tabs layout
        var tabsView =
            Layout.Tabs(
                new Tab("Text Diff", textTabContent).Icon(Icons.FileText),
                new Tab("File Paths Diff", fileTabContent).Icon(Icons.File)
            ).Variant(TabsVariant.Tabs);

        // outer card wide enough to allow side-by-side on big screens
        return Layout.Vertical()
            | (Layout.Horizontal().Gap(2).Align(Align.Center)
                | new Card(tabsView).Width(Size.Fraction(MainCardWidthFraction)));
    }
}