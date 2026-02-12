namespace MermaidExample.Services;

public class MermaidService
{
    /// <summary>
    /// Generates Mermaid code from data models
    /// </summary>
    public string GenerateMermaidCode(
        string direction,
        List<Models.NodeData> nodes,
        List<Models.LinkData> links)
    {
        var mermaidNodes = new List<Node>();
        
        // Convert nodes
        foreach (var node in nodes)
        {
            var shapeType = Helpers.MermaidHelpers.ConvertToShapeType(node.Shape);
            mermaidNodes.Add(new Node(node.Id, node.Text, shapeType));
        }

        var mermaidLinks = new List<Link>();
        
        // Convert links
        foreach (var link in links)
        {
            var linkType = Helpers.MermaidHelpers.ConvertToLinkType(link.LinkType);
            var arrowType = Helpers.MermaidHelpers.ConvertToArrowType(link.ArrowType);
            mermaidLinks.Add(new Link(link.From, link.To, link.Label, null, false, linkType, arrowType));
        }

        // Generate Mermaid code
        var flowchart = new Flowchart(direction, mermaidNodes, mermaidLinks);
        return flowchart.CalculateFlowchart();
    }
}
