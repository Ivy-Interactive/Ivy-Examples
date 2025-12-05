namespace MermaidExample.Constants;

public static class MermaidConstants
{
    // Node shape options
    public static readonly List<string> ShapeOptions = new()
    {
        "Rectangle", "Rounded", "Stadium", "Circle", "Rhombus",
        "Hexagon", "Parallelogram", "Cylinder", "Trapezoid", "TrapezoidAlt", "Subroutine"
    };

    // Diagram direction options
    public static readonly List<string> DirectionOptions = new()
    {
        "LR", "TD", "BT", "RL"
    };

    // Link type options
    public static readonly List<string> LinkTypeOptions = new()
    {
        "Normal", "Dotted", "Thick", "Invisible"
    };

    // Arrow type options
    public static readonly List<string> ArrowTypeOptions = new()
    {
        "Normal", "Circle", "Cross", "Open"
    };

    // Default initial nodes
    public static List<Models.NodeData> GetDefaultNodes() => new()
    {
        new() { Id = "node1", Text = "Start", Shape = "Circle" },
        new() { Id = "node2", Text = "Process", Shape = "Rectangle" },
        new() { Id = "node3", Text = "End", Shape = "Stadium" }
    };

    // Default initial links
    public static List<Models.LinkData> GetDefaultLinks() => new()
    {
        new() { From = "node1", To = "node2", Label = "begin", LinkType = "Normal", ArrowType = "Normal" },
        new() { From = "node2", To = "node3", Label = "finish", LinkType = "Normal", ArrowType = "Normal" }
    };
}
