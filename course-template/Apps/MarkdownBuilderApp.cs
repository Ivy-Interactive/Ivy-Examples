using Ivy;
using Ivy.Apps;
using Ivy.Shared;
using Ivy.Core;
using static Ivy.Views.Layout;
using static Ivy.Views.Text;
using CourseTemplate.Apps.Services;

namespace CourseTemplate.Apps;

[App(order: 1, title: "Markdown Builder", icon: Icons.FileText)]
public class MarkdownBuilderApp() : ViewBase
{
    private record FileNode(string Label, string FullPath, bool IsFolder, FileNode[] Children);

    private string _markdownContent = """
        # Welcome to Markdown Builder!

        This is an example of markdown content. You can edit this text in the left panel.

        ## Features

        - **Bold text**
        - *Italic text*
        - `Code`
        - [Links](https://example.com)

        ### Code

        ```csharp
        public class Example
        {
            public void DoSomething()
            {
                Console.WriteLine("Hello, World!");
            }
        }
        ```

        > This is a quote

        ---

        **Start editing in the left panel!**
        """;

    public override object? Build()
    {
        var markdownState = this.UseState(_markdownContent);
        var selectedPath = this.UseState<string?>();
        var isSheetOpen = this.UseState(false);
        var isFileSet = this.UseState(false);
        var isSaving = this.UseState(false);

        // services
        var fs = new FileSystemService();
        var generator = new GenerationService(fs);

        // build menu items from Modules directory
        var modulesPath = fs.FindModulesFolder();
        var tree = BuildTree(modulesPath);

        MenuItem ToMenuItem(FileNode node)
        {
            if (node.IsFolder)
            {
                return new MenuItem(
                    node.Label,
                    node.Children.Select(ToMenuItem).ToArray(),
                    Icons.Folder,
                    Expanded: true
                );
            }
            else
            {
                return new MenuItem(
                    node.Label,
                    Icon: Icons.FileText,
                    Tag: node.FullPath
                );
            }
        }

        ValueTask OnSelect(Event<SidebarMenu, object> ev)
        {
            if (ev.Value is string path && System.IO.File.Exists(path))
            {
                selectedPath.Set(path);
                try
                {
                    var content = System.IO.File.ReadAllText(path);
                    markdownState.Set(content);
                }
                catch
                {
                    // ignore read errors for now
                }
                isFileSet.Value = true;
                isSheetOpen.Value = false;
            }
            return ValueTask.CompletedTask;
        }
        
        var leftCard = new Card(
            Layout.Vertical().Gap(6).Padding(3)
            | Text.H3("Markdown Builder")
            | (isFileSet.Value
                ? (object)(Layout.Horizontal().Align(Align.Left).Gap(2)
                    | Text.Block("Enter your markdown code")
                    | new Spacer().Width(Size.Grow())
                    | new Button(System.IO.Path.GetFileName(selectedPath.Value)).Icon(Icons.FileText).HandleClick(() => { isSheetOpen.Value = true; })
                  )
                : (object)(Layout.Horizontal().Align(Align.Left).Gap(2)
                    | Text.Block("Enter your markdown code")
                    | new Spacer().Width(Size.Grow())
                    | new Button("Choose File").Icon(Icons.FolderOpen).HandleClick(() => { isSheetOpen.Value = true; })
                  )
              )
            | markdownState.ToCodeInput()
                .Language(Languages.Markdown)
                .Width(Size.Full())
                .Height(Size.Units(140))
                .Placeholder("Body (Markdown)")
            | (isFileSet.Value
                ? new Button(isSaving.Value ? "Saving..." : "Save")
                    .Icon(Icons.Save)
                    .Disabled(isSaving.Value)
                    .HandleClick(() =>
                    {
                        if (selectedPath.Value == null) return;
                        isSaving.Value = true;
                        try
                        {
                            // Save file
                            System.IO.File.WriteAllText(selectedPath.Value, markdownState.Value);

                            // Regenerate only this file and wait until generated output updated
                            generator.RegenerateSingleBlocking(selectedPath.Value);
                        }
                        catch { }
                        finally
                        {
                            isSaving.Value = false;
                        }
                    })
                : null)
            | Text.Markdown("Builds HTML from Markdown.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework)")
        ).Width(Size.Fraction(0.4f));

        var rightPanel = Layout.Vertical().Width(Size.Fraction(0.6f))
            | Text.Markdown(markdownState.Value);

        var main = Layout.Horizontal().Gap(6)
            | leftCard
            | rightPanel;

        var sheet = isSheetOpen.Value
            ? new Sheet(_ => { isSheetOpen.Value = false; return ValueTask.CompletedTask; },
                Layout.Vertical().Scroll().Padding(2)
                    | (tree != null
                        ? new SidebarMenu(OnSelect, ToMenuItem(tree))
                        : Text.Muted("Modules folder not found")),
                title: "Pages",
                description: "Select a page to edit")
                .Width(Size.Rem(28))
            : null;

        return new Fragment(main, sheet);
    }

    private static FileNode? BuildTree(string? root)
    {
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
        return BuildNode(root);
    }

    private static FileNode BuildNode(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        // Показываем реальные имена файлов и папок (с нумерацией)
        var display = name;
        if (System.IO.Directory.Exists(path))
        {
            var children = System.IO.Directory.GetFileSystemEntries(path)
                .OrderBy(p => p)
                .Select(BuildNode)
                .ToArray();
            return new FileNode(display, path, true, children);
        }
        else
        {
            return new FileNode(display, path, false, Array.Empty<FileNode>());
        }
    }

}


