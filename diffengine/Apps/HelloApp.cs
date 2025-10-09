namespace DiffengineExample.Apps;

[App(icon: Icons.Diff, title: "DiffEngine")]
public class HelloApp : ViewBase
{
    public override object Build()
    {
        // states
        var leftText = this.UseState("");
        var rightText = this.UseState("");
        var textExt = this.UseState("txt");

        var leftFile = this.UseState("");
        var rightFile = this.UseState("");
        var fileExt = this.UseState("txt");

        var lastLeft = this.UseState<string>();
        var lastRight = this.UseState<string>();
        var error = this.UseState<string>();

        // handlers
        Func<Task> launchText = async () =>
        {
            try
            {
                error.Value = "";
                var pair = await DiffService.LaunchTextAsync(
                    leftText.Value, rightText.Value, textExt.Value ?? "txt");
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
                error.Value = "";
                var pair = await DiffService.LaunchFilesAsync(
                    leftFile.Value, rightFile.Value, fileExt.Value ?? "txt");
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
        var textTabContent =
            Layout.Vertical().Gap(6).Padding(2)
            | Text.Block("Type text on each side and choose an extension. Launch writes temp files and opens your diff tool.")
            | Layout.Vertical().Gap(2)
                | Text.Block("Left (text)")
                | leftText.ToInput(placeholder: "left text…")
                | Text.Block("Right (text)")
                | rightText.ToInput(placeholder: "right text…")
                | Text.Block("Extension")
                | textExt.ToInput(placeholder: "txt / json")
            | Layout.Horizontal().Gap(3)
                | new Button("Launch Diff (Text)", onClick: () => { _ = launchText(); })
                | new Button("Kill Last Diff", onClick: () => kill());

        // file diff tab content
        var fileTabContent =
            Layout.Vertical().Gap(6).Padding(2)
            | Text.Block("Enter two file paths (they're copied to temp), then Launch.")
            | Layout.Vertical().Gap(2)
                | Text.Block("Left file path")
                | leftFile.ToInput(placeholder: @"e.g. C:\temp\left.txt")
                | Text.Block("Right file path")
                | rightFile.ToInput(placeholder: @"e.g. C:\temp\right.txt")
                | Text.Block("Treat as extension")
                | fileExt.ToInput(placeholder: "txt / json")
            | Layout.Horizontal().Gap(3)
                | new Button("Launch Diff (Files)", onClick: () => { _ = launchFiles(); })
                | new Button("Kill Last Diff", onClick: () => kill());

        // tabs layout
        var tabsView =
            Layout.Tabs(
                new Tab("Text Diff", textTabContent).Icon(Icons.FileText),
                new Tab("File Paths Diff", fileTabContent).Icon(Icons.File)
            ).Variant(TabsVariant.Tabs);

        // page
        var page =
            Layout.Vertical().Gap(8).Padding(2)
            | Text.H2("DiffEngine × Ivy")
            | Text.Block(string.IsNullOrWhiteSpace(error.Value) ? "" : "⚠ " + error.Value)
            | tabsView
            | Layout.Horizontal().Gap(2)
                | Text.Small($"CI: {(DiffService.IsCi ? "On" : "Off")}")
                | Text.Small(string.IsNullOrEmpty(lastLeft.Value) ? "No active diff" : "Last diff ready (Kill available)")
                | Text.Small(string.IsNullOrEmpty(lastLeft.Value) ? "" : $"Temp: {lastLeft.Value} vs {lastRight.Value}");

        // outer card wide enough to allow side-by-side on big screens
        return Layout.Center()
             | page;
    }
}