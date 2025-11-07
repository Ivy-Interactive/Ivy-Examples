using System.Dynamic;
using System.Text;
using System.Text.Json;

namespace FastMemberDemo.Apps;
[App(icon: Icons.PartyPopper, title: "FastMemberDemo")]
public class FastMemberDemoApp : ViewBase
{
    public record ProductModel(string Name, string Description, decimal Price, string Category);

    // Cache TypeAccessor since it's thread-safe and can be reused
    private static readonly TypeAccessor TypeAccessor = TypeAccessor.Create(typeof(ProductModel));

    // Pre-defined member names to avoid magic strings
    private static readonly string[] ProductProperties = { "Name", "Price", "Category" };

    // JSON serializer options for pretty printing
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public override object? Build()
    {
        // Create a list of products (immutable for thread safety)
        var products = new List<ProductModel>
        {
            new("Laptop", "High-performance laptop", 999.99m, "Electronics"),
            new("Mouse", "Wireless mouse", 29.99m, "Electronics"),
            new("Book", "Programming guide", 49.99m, "Books"),
            new("Chair", "Ergonomic office chair", 199.99m, "Furniture")
        };

        var result = this.UseState<string>("");

        // Unified button click handler to reduce code duplication
        void HandleButtonClick(Func<string> action)
        {
            result.Set(action());
        }


        string GetColumnTypes()
        {
            var columnInfo = TypeAccessor.GetMembers()
               .ToDictionary(member => member.Name, member => member.Type.Name);

            return JsonSerializer.Serialize(columnInfo, JsonOptions);
        }

        string GetFirstProductInfo()
        {
            if (products.Count == 0) return JsonSerializer.Serialize(new { Message = "No products available." }, JsonOptions);

            var firstProduct = products[0];
            var accessor = ObjectAccessor.Create(firstProduct);

            // Create dynamic object to represent the product data
            var productData = new
            {
                Original = new
                {
                    Name = accessor["Name"],
                    Price = accessor["Price"],
                    Category = accessor["Category"]
                },
                Modified = new
                {
                    Name = accessor["Name"],
                    Price = 899.99m, // Modified price
                    Category = accessor["Category"]
                }
            };

            // Actually modify the property value
            accessor["Price"] = 899.99m;

            return JsonSerializer.Serialize(productData, JsonOptions);
        }

        string GetAllProductInfo()
        {
            using var reader = ObjectReader.Create(products, ProductProperties);
            var productList = new List<object>();

            while (reader.Read())
            {
                productList.Add(new
                {
                    Product = reader[0],
                    Price = reader[1],
                    Category = reader[2]
                });
            }

            return productList.Count == 0
                ? JsonSerializer.Serialize(new { Message = "No products to display." }, JsonOptions)
                : JsonSerializer.Serialize(productList, JsonOptions);
        }

        string GetAllPriceInfo()
        {
            if (products.Count == 0) return JsonSerializer.Serialize(new { Message = "No products available." }, JsonOptions);

            var dynamicProducts = new List<dynamic>();

            foreach (var product in products)
            {
                dynamic dynamicProduct = new ExpandoObject();
                var dynamicAccessor = ObjectAccessor.Create(dynamicProduct);
                var sourceAccessor = ObjectAccessor.Create(product);

                // Copy only needed properties
                foreach (var propertyName in ProductProperties)
                {
                    dynamicAccessor[propertyName] = sourceAccessor[propertyName];
                }

                dynamicProducts.Add(dynamicProduct);
            }

            // Convert dynamic objects to serializable format
            var serializableProducts = dynamicProducts.Select(p => new
            {
                Name = p.Name,
                Price = p.Price,
                Category = p.Category
            }).ToList();

            return JsonSerializer.Serialize(serializableProducts, JsonOptions);
        }

        // Build the UI
        return Layout.Vertical()
            | Text.H2("FastMember Demo")
            | new WrapLayout([ // Action buttons for different FastMember demonstrations
                new Button("Get Column Types", () => HandleButtonClick(GetColumnTypes)),
                new Button("Get First Product Info", () => HandleButtonClick(GetFirstProductInfo)),
                new Button("Get All Product Info", () => HandleButtonClick(GetAllProductInfo)),
                new Button("Get All Price Info", () => HandleButtonClick(GetAllPriceInfo))], gap: 3)
            | BuildProductTable(products)// Display products in a table
            | Text.H2("Result") // Results section title
            | new Code(result.Value, Languages.Json)
                .ShowLineNumbers()
                .ShowCopyButton() // Text area for displaying operation results
            | Text.Small("Results are displayed in JSON format with proper formatting.");

    }

    // Extract table building to separate method for better readability
    private static TableBuilder<ProductModel> BuildProductTable(IList<ProductModel> products)
    {
        return products.ToTable()
            .Width(Size.Full())
            .Builder(p => p.Name, f => f.Default())
            .Builder(p => p.Description, f => f.Text())
            .Builder(p => p.Price, f => f.Default())
            .Builder(p => p.Category, f => f.Default());
    }
}
