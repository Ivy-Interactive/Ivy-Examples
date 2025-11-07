using Constructs;
using ConstructsExample.Domain;
using ConstructsExample.Extensions;

namespace ConstructsExample.Apps;

[App(icon: Icons.FileCode2, title: "Constructs")]
public class ConstructsApp : ViewBase
{
    private RootConstruct? _root;
    const int MaxLines = 40;

    public override object? Build()
    {
        var status = UseState(string.Empty);
        var parent = UseState(string.Empty);
        var childId = UseState(string.Empty);
        var maxLines = UseState(MaxLines);
        var client = UseService<IClientProvider>();

        if (_root is null)
        {
            try
            {
                _root = DemoConstruct.BuildRoot();
            }
            catch (Exception ex)
            {
                return Layout.Center()
                     | new Card(
                         Layout.Vertical().Gap(6).Padding(3)
                         | Text.H2("Constructs initialization failed")
                         | Text.Block("Make sure Node.js 20/22 LTS is installed (jsii runtime).")
                         | new Separator()
                         | Text.Markdown("```text\n" + ex + "\n```")
                       )
                       .Width(Size.Units(140).Max(800));
            }
        }
        parent.Set(parent.Value.TrimStart('/'));
        string basePath = string.IsNullOrWhiteSpace(parent.Value) ? _root.Node.Path : parent.Value.Trim();
        var asciiLines = _root.RenderAsciiLines(basePath);
        var visible = asciiLines.Take(maxLines.Value).ToList();
        bool hasMore = asciiLines.Count > visible.Count;

        // Build left (controls) panel
        LayoutView left = Layout.Vertical().Gap(6)
            | Text.H2("AWS Constructs — interactive demo")
            | Text.Block("Write a parent path, add a child construct, or reset to the canonical tree.")
            | parent.ToInput(placeholder: "Parent path, ex. Root/Demo/Nested. Start typing to filter tree view.")
            | Text.Block("New child id")
            | childId.ToInput(placeholder: "ChildX")
            | (
                Layout.Horizontal().Gap(2)
                | new Button("Add child", onClick: () =>
                {
                    string parentPath = string.IsNullOrWhiteSpace(parent.Value)
                        ? _root.Node.Path
                        : parent.Value.Trim();

                    string newChildId = childId.Value.Trim();
                    if (string.IsNullOrWhiteSpace(newChildId))
                    {
                        client.Toast("Enter a non-empty child id.");
                        return;
                    }

                    Construct? parentNode = _root.FindByPath(parentPath);
                    if (parentNode == null)
                    {
                        client.Toast($"Parent path not found: {parentPath}");
                        return;
                    }

                    _ = new Construct(parentNode, newChildId);
                    childId.Set(string.Empty);
                    client.Toast($"Added: {parentNode.Node.Path}/{newChildId}");
                }).Icon(Icons.Plus)
                | new Button("Reset", _ =>
                {
                    _root = DemoConstruct.BuildRoot();
                    parent.Set(string.Empty);
                    client.Toast("Tree reset.");
                    maxLines.Set(MaxLines);
                }).Icon(Icons.RefreshCw)
                )
                | Text.Small("This demo uses the AWS Constructs library to build composable configuration models.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [AWS Constructs](https://github.com/aws/constructs)")
            ;

        // Build right (tree view) panel
        Card right = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Current tree")
                | new Separator()
                | Text.Markdown("```text\n" + string.Join('\n', visible) + "\n```")
                | (hasMore
                    ? new Button("Show more", onClick: () => maxLines.Set(maxLines.Value + MaxLines))
                    : (visible.Count > MaxLines
                        ? new Button("Collapse", onClick: () => maxLines.Set(MaxLines))
                        : Text.Block(string.Empty)))
            )
            .Width(Size.Fraction(0.8f));

        return Layout.Center()
             | new Card(
                 Layout.Horizontal().Gap(12).Padding(3)
                 | left
                 | right
               ).Width(Size.Fraction(0.8f));
    }
}
