namespace MermaidExample.Models;

public class NodeData
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Shape { get; set; } = "Rectangle";
}

public class LinkData
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string LinkType { get; set; } = "Normal";
    public string ArrowType { get; set; } = "Normal";
}


