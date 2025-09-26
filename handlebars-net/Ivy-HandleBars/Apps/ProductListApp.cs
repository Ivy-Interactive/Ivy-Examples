using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;

namespace Ivy_Handlebars_Demo.Apps;

public class Product
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

[App(icon: Icons.FileText, title: "Product List with Handlebars")]
public class ProductListApp : ViewBase
{
    private readonly IHandlebars handlebars;

    public ProductListApp()
    {
        // Initialize Handlebars with all available helpers
        handlebars = Handlebars.Create();

        // Register built-in helpers with options
        HandlebarsHelpers.Register(handlebars);

        // Custom helpers for product-specific formatting
        handlebars.RegisterHelper("formatPrice", (context, args) =>
        {
            if (args.Length > 0 && args[0] is decimal price)
                return $"${price:N2}";
            return "$0.00";
        });

        // Register datetime helper
        handlebars.RegisterHelper("datetime", (context, arguments) =>
        {
            if (arguments.Length < 2) return arguments[0];

            if (arguments[0] is DateTime date)
            {
                var format = arguments[1]?.ToString()?.ToLower();
                return format switch
                {
                    "humanize" => HumanizeDateTime(date),
                    _ => date.ToString()
                };
            }
            return arguments[0];
        });
    }

    private static string HumanizeDateTime(DateTime date)
    {
        var timeSpan = DateTime.Now - date;

        return timeSpan.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{Math.Floor(timeSpan.TotalMinutes)} minutes ago",
            < 120 => "an hour ago",
            < 1440 => $"{Math.Floor(timeSpan.TotalHours)} hours ago",
            < 2880 => "yesterday",
            _ => date.ToString("MMM dd, yyyy")
        };
    }

    public override object? Build()
    {
        // Define the initial state for our list of products.
        var productsState = this.UseState(new List<Product>());

        // State for the product being added in the input fields.
        var productNameState = this.UseState<string>(string.Empty);
        var productPriceState = this.UseState<string>(string.Empty);
        var productCategoryState = this.UseState<string>(string.Empty);

        // Add a new product to the list
        ValueTask AddProduct(Event<Button> _)
        {
            var products = productsState.Value;
            decimal price = 0;
            if (!string.IsNullOrWhiteSpace(productPriceState.Value) && decimal.TryParse(productPriceState.Value, out var parsedPrice))
                price = parsedPrice;

            products.Add(new Product
            {
                Name = productNameState.Value,
                Price = price,
                Category = productCategoryState.Value,
                CreatedDate = DateTime.Now
            });

            productsState.Value = new List<Product>(products); // Create a new list to trigger a state update

            // Clear the form
            productNameState.Value = string.Empty;
            productPriceState.Value = string.Empty;
            productCategoryState.Value = string.Empty;

            return ValueTask.CompletedTask;
        }

        // Build the UI
        return new Card(
            Layout.Center(
                Layout.Vertical()
                | Layout.Vertical().Gap(10).Padding(2)
                | Text.H2("Product List Demo")
                | Text.Block("Add products below. The list is rendered using Handlebars.Net.")
                | new Separator()
                | Text.Block("New Product Details:")
                | productNameState.ToInput(placeholder: "Product Name")
                | productPriceState.ToInput(placeholder: "Price")
                | productCategoryState.ToInput(placeholder: "Category")
                | new Button("Add Product") { OnClick = AddProduct }
                | new Separator()
                | Text.H2("Products")
                | Layout.Vertical().Gap(8)
                | (
                    productsState.Value.Count == 0
                        ? Text.Block("No products yet.")
                        : productsState.Value.Select(product =>
                    {
                        // Use Handlebars to process product created time
                        var createdTimeTemplate = handlebars.Compile(@"
                            Added: {{datetime CreatedDate 'humanize'}}                            
                        ");

                        var createdTime = createdTimeTemplate(product);

                        return new Card(
                            Layout.Vertical().Gap(4)
                            | Text.H4(product.Name)
                            | Text.Block($"Price: {handlebars.Compile("{{formatPrice Price}}")(product)}")
                            | Text.Block($"Category: {product.Category}")
                            | Text.Block(createdTime)
                        );
                    }).ToArray()
                )
            )
        );
    }
}