namespace SquirrelExample.Models;

public class FashionProduct
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Rating { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
}

