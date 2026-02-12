namespace MermaidExample.Helpers;

public static class MermaidHelpers
{
    /// <summary>
    /// Converts string representation of shape to Node.ShapeType
    /// </summary>
    public static Node.ShapeType ConvertToShapeType(string shape)
    {
        return shape switch
        {
            "Rectangle" => Node.ShapeType.Rectangle,
            "Rounded" => Node.ShapeType.Rounded,
            "Stadium" => Node.ShapeType.Stadium,
            "Circle" => Node.ShapeType.Circle,
            "Rhombus" => Node.ShapeType.Rhombus,
            "Hexagon" => Node.ShapeType.Hexagon,
            "Parallelogram" => Node.ShapeType.Parallelogram,
            "Cylinder" => Node.ShapeType.Cylinder,
            "Trapezoid" => Node.ShapeType.Trapezoid,
            "TrapezoidAlt" => Node.ShapeType.TrapezoidAlt,
            "Subroutine" => Node.ShapeType.Subroutine,
            _ => Node.ShapeType.Rectangle
        };
    }

    /// <summary>
    /// Converts string representation of link type to Link.LinkType
    /// </summary>
    public static Link.LinkType ConvertToLinkType(string linkType)
    {
        return linkType switch
        {
            "Dotted" => Link.LinkType.Dotted,
            "Thick" => Link.LinkType.Thick,
            "Invisible" => Link.LinkType.Invisible,
            _ => Link.LinkType.Normal
        };
    }

    /// <summary>
    /// Converts string representation of arrow type to Link.ArrowType
    /// </summary>
    public static Link.ArrowType ConvertToArrowType(string arrowType)
    {
        return arrowType switch
        {
            "Circle" => Link.ArrowType.Circle,
            "Cross" => Link.ArrowType.Cross,
            "Open" => Link.ArrowType.Open,
            _ => Link.ArrowType.Normal
        };
    }
}
