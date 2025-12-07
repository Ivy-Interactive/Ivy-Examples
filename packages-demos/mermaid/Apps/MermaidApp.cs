namespace MermaidExample.Apps;

[App(title: "Mermaid", icon: Icons.Network)]
public class MermaidApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var mermaidService = new Services.MermaidService();

        // State for nodes and links
        var nodes = UseState(() => Constants.MermaidConstants.GetDefaultNodes());
        var links = UseState(() => Constants.MermaidConstants.GetDefaultLinks());
        var direction = UseState("LR");
        var mermaidCode = UseState("");

        // State for add forms
        var newNodeId = UseState("");
        var newNodeText = UseState("");
        var newNodeShape = UseState("Rectangle");

        var newLinkFrom = UseState("");
        var newLinkTo = UseState("");
        var newLinkLabel = UseState("");
        var newLinkType = UseState("Normal");
        var newLinkArrow = UseState("Normal");

        // Generate on first render
        UseEffect(() =>
        {
            GenerateMermaidCode();
        }, []);

        // Automatic generation on data change
        UseEffect(() =>
        {
            GenerateMermaidCode();
        }, [nodes.ToTrigger(), links.ToTrigger(), direction.ToTrigger()]);

        // Generate Mermaid code
        void GenerateMermaidCode()
        {
            try
            {
                var code = mermaidService.GenerateMermaidCode(direction.Value, nodes.Value, links.Value);
                mermaidCode.Set(code);
            }
            catch (Exception ex)
            {
                client.Error(ex);
            }
        }

        // Node management functions
        void AddNode()
        {
            if (string.IsNullOrWhiteSpace(newNodeId.Value) || string.IsNullOrWhiteSpace(newNodeText.Value))
            {
                client.Toast("Please fill in Node ID and Text");
                return;
            }

            if (nodes.Value.Any(n => n.Id == newNodeId.Value))
            {
                client.Toast($"Node with ID '{newNodeId.Value}' already exists");
                return;
            }

            nodes.Set(n => n.Concat(new[] { new Models.NodeData
            {
                Id = newNodeId.Value,
                Text = newNodeText.Value,
                Shape = newNodeShape.Value
            }}).ToList());

            newNodeId.Set("");
            newNodeText.Set("");
            newNodeShape.Set("Rectangle");
        }

        void RemoveNode(string nodeId)
        {
            nodes.Set(n => n.Where(node => node.Id != nodeId).ToList());
            links.Set(l => l.Where(link => link.From != nodeId && link.To != nodeId).ToList());
        }

        // Link management functions
        void AddLink()
        {
            if (string.IsNullOrWhiteSpace(newLinkFrom.Value) || string.IsNullOrWhiteSpace(newLinkTo.Value))
            {
                client.Toast("Please select From and To nodes");
                return;
            }

            links.Set(l => l.Concat(new[] { new Models.LinkData
            {
                From = newLinkFrom.Value,
                To = newLinkTo.Value,
                Label = newLinkLabel.Value,
                LinkType = newLinkType.Value,
                ArrowType = newLinkArrow.Value
            }}).ToList());

            newLinkFrom.Set("");
            newLinkTo.Set("");
            newLinkLabel.Set("");
            newLinkType.Set("Normal");
            newLinkArrow.Set("Normal");
        }

        void RemoveLink(int index)
        {
            links.Set(l => l.Where((_, i) => i != index).ToList());
        }

        // Select options from constants
        var nodeIdOptions = nodes.Value.Select(n => n.Id).ToList();

        // Nodes table
        object nodesView;
        if (nodes.Value.Count == 0)
        {
            nodesView = Text.Muted("No nodes yet");
        }
        else
        {
            var nodesTableRows = new List<TableRow>();
            // Header row
            nodesTableRows.Add(new TableRow([
                new TableCell("ID").IsHeader(),
                new TableCell("Text").IsHeader(),
                new TableCell("Shape").IsHeader(),
                new TableCell("Action").IsHeader()
            ]));
            // Data rows
            foreach (var node in nodes.Value)
            {
                nodesTableRows.Add(new TableRow([
                    new TableCell(node.Id),
                    new TableCell(node.Text),
                    new TableCell(node.Shape),
                    new TableCell(new Button("Delete", onClick: () => RemoveNode(node.Id)).Size(Sizes.Small))
                ]));
            }
            nodesView = new Table([.. nodesTableRows]);
        }

        // Links table
        object linksView;
        if (links.Value.Count == 0)
        {
            linksView = Text.Muted("No links yet");
        }
        else
        {
            var linksTableRows = new List<TableRow>();
            // Header row
            linksTableRows.Add(new TableRow([
                new TableCell("From").IsHeader(),
                new TableCell("To").IsHeader(),
                new TableCell("Label").IsHeader(),
                new TableCell("Type").IsHeader(),
                new TableCell("Action").IsHeader()
            ]));
            // Data rows
            for (int i = 0; i < links.Value.Count; i++)
            {
                var link = links.Value[i];
                var index = i;
                linksTableRows.Add(new TableRow([
                    new TableCell(link.From),
                    new TableCell(link.To),
                    new TableCell(string.IsNullOrEmpty(link.Label) ? "—" : link.Label),
                    new TableCell($"{link.LinkType}/{link.ArrowType}"),
                    new TableCell(new Button("Delete", onClick: () => RemoveLink(index)).Size(Sizes.Small))
                ]));
            }
            linksView = new Table([.. linksTableRows]);
        }

        // Add Node form - extracted as array to fix layout issues
        var addNodeView = (object)new[]
        {
            Layout.Horizontal().Gap(1).Align(Align.Left)
                | newNodeId.ToTextInput().Placeholder("ID").Width(Size.Fraction(0.26f))
                | newNodeText.ToTextInput().Placeholder("Text").Width(Size.Fraction(0.26f))
                | newNodeShape.ToSelectInput(Constants.MermaidConstants.ShapeOptions.ToOptions()).Placeholder("Shape").Width(Size.Fraction(0.26f))
                | new Button("Add", onClick: AddNode).Size(Sizes.Medium)
        };

        // Add Link form - extracted as array to fix layout issues
        var addLinkView = (object)new[]
        {
            Layout.Horizontal().Gap(1).Align(Align.Left)
                | newLinkFrom.ToSelectInput(nodeIdOptions.ToOptions()).Placeholder("From").Width(Size.Fraction(0.11f))
                | newLinkTo.ToSelectInput(nodeIdOptions.ToOptions()).Placeholder("To").Width(Size.Fraction(0.11f))
                | newLinkLabel.ToTextInput().Placeholder("Label").Width(Size.Fraction(0.25f))
                | newLinkType.ToSelectInput(Constants.MermaidConstants.LinkTypeOptions.ToOptions()).Placeholder("Link").Width(Size.Fraction(0.15f))
                | newLinkArrow.ToSelectInput(Constants.MermaidConstants.ArrowTypeOptions.ToOptions()).Placeholder("Arrow").Width(Size.Fraction(0.15f))
                | new Button("Add", onClick: AddLink).Size(Sizes.Medium).Disabled(nodeIdOptions.Count < 2)
        };

        // Direction selector - extracted as array
        var directionView = (object)new[]
        {
            Layout.Horizontal().Gap(2).Align(Align.Left)
                | Text.H4("Choose Direction:")
                | direction.ToSelectInput(Constants.MermaidConstants.DirectionOptions.ToOptions()).Width(Size.Px(100))
        };

        // UI
        return Layout.Horizontal().Gap(3)
            | new Card(
                Layout.Vertical().Gap(2)
                | Text.H3("Mermaid Builder")
                
                // Direction selector
                | directionView

                // Nodes section
                | Text.H4("Nodes")
                | nodesView
                | Text.H4("Add Node")
                | addNodeView

                // Links section
                | Text.H4("Links")
                | linksView
                | Text.H4("Add Link")
                | addLinkView
            ).Width(Size.Half()).Height(Size.Fit().Min(Size.Full()))

            | new Card(
                Layout.Vertical().Gap(3)
                | Text.H3("Preview")

                // Mermaid visualization
                | Text.H4("Mermaid Diagram")
                | (string.IsNullOrEmpty(mermaidCode.Value)
                    ? Text.Muted("Generate a diagram to see the preview")
                    : Text.Markdown($"```mermaid\n{mermaidCode.Value}\n```"))

                // Mermaid code
                | Text.H4("Mermaid Code")
                | mermaidCode.ToCodeInput(variant: CodeInputs.Default, language: Languages.Markdown)
                    .Height(Size.Px(150))
                    .Disabled(true)
                | new Spacer()
                | Text.Small("This demo uses the MermaidDotNet NuGet package to generate Mermaid diagrams.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [MermaidDotNet](https://github.com/samsmithnz/MermaidDotNet)")
            ).Width(Size.Half()).Height(Size.Fit().Min(Size.Full()));
    }
}
