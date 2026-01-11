namespace SquirrelExample.Models;

public record FashionProduct(
    int UserId,
    int ProductId,
    string ProductName,
    string Brand,
    string Category,
    decimal Price,
    double Rating,
    string Color,
    string Size);
