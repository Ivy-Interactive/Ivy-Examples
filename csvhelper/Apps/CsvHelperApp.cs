[App(icon: Icons.Table, title: "CSV Helper")]
public class CsvHelperApp : ViewBase
{
    public class ProductModel
    {
        public Guid Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public string Category { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        // State for products list
        var products = UseState(() => new List<ProductModel>
        {
            new() { Id = Guid.NewGuid(), Name = "Wireless Mouse", Description = "Ergonomic 2.4 GHz mouse with silent click", Price = 19.99m, Category = "Accessories", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Description = "RGB backlit mechanical keyboard with blue switches", Price = 79.50m, Category = "Accessories", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "27\" 4K Monitor", Description = "Ultra-HD IPS display with HDR support", Price = 299.99m, Category = "Displays", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "USB-C Hub", Description = "7-in-1 hub with HDMI, USB 3.0 and card reader", Price = 34.95m, Category = "Peripherals", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Noise-Cancelling Headphones", Description = "Over-ear Bluetooth headphones with ANC", Price = 149.00m, Category = "Audio", CreatedAt = DateTime.UtcNow },
        });

        var newProduct = UseState(() => new ProductModel());
        
        // Export CSV download
        var downloadUrl = this.UseDownload(
            async () =>
            {
                await using var ms = new MemoryStream();
                await using var writer = new StreamWriter(ms, leaveOpen: true);
                await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

                await csv.WriteRecordsAsync(products.Value);
                await writer.FlushAsync();
                ms.Position = 0;

                return ms.ToArray();
            },
            "text/csv",
            $"products-{DateTime.UtcNow:yyyy-MM-dd}.csv"
        );

        // Import CSV using UseUpload
        var uploadUrl = this.UseUpload(
            uploadedBytes =>
            {
                try
                {
                    using var stream = new MemoryStream(uploadedBytes);
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

                    var records = csv.GetRecords<ProductModel>().ToList();
                    
                    // Assign new IDs and timestamps to imported records
                    foreach (var record in records)
                    {
                        record.Id = Guid.NewGuid();
                        record.CreatedAt = DateTime.UtcNow;
                    }

                    products.Value = products.Value.Concat(records).ToList();
                    client.Toast($"Imported {records.Count} products from CSV");
                }
                catch (Exception ex)
                {
                    client.Toast($"Failed to import CSV: {ex.Message}");
                }
            },
            "text/csv",
            "imported-products"
        );

        // Delete action
        var deleteProduct = new Action<Guid>((id) =>
        {
            var product = products.Value.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                products.Value = products.Value.Where(p => p.Id != id).ToList();
                client.Toast($"Product '{product.Name}' deleted");
            }
        });

        // State for dialog open/close
        var isDialogOpen = UseState(false);
        
        // Handle product submission via UseEffect
        UseEffect(() =>
        {
            // Check if form was submitted (non-empty required fields and dialog is open)
            if (isDialogOpen.Value && 
                !string.IsNullOrEmpty(newProduct.Value.Name) && 
                newProduct.Value.Price > 0 &&
                !string.IsNullOrEmpty(newProduct.Value.Category))
            {
                newProduct.Value.Id = Guid.NewGuid();
                newProduct.Value.CreatedAt = DateTime.UtcNow;
                products.Value = products.Value.Append(newProduct.Value).ToList();
                client.Toast("Product added successfully");
                newProduct.Value = new ProductModel();
                isDialogOpen.Value = false;
            }
        }, [newProduct]);

        // Build Add form
        var addProductDialog = newProduct.ToForm()
            .Remove(x => x.Id)
            .Remove(x => x.CreatedAt)
            .Required(m => m.Name, m => m.Price, m => m.Category)
            .Label(m => m.Name, "Product Name")
            .Label(m => m.Description, "Description")
            .Label(m => m.Price, "Price")
            .Label(m => m.Category, "Category")
            .Builder(m => m.Name, s => s.ToTextInput().Placeholder("Enter product name..."))
            .Builder(m => m.Description, s => s.ToTextInput().Placeholder("Enter description..."))
            .Builder(m => m.Price, s => s.ToNumberInput().Placeholder("Enter price...").Min(0))
            .Builder(m => m.Category, s => s.ToTextInput().Placeholder("Enter category..."))
            .ToDialog(isDialogOpen, title: "Add New Product", submitTitle: "Add Product");

        // Build the table with delete button
        var table = products.Value.Select(p => new
        {
            p.Name,
            p.Description,
            p.Price,
            p.Category,
            p.CreatedAt,
            Delete = Icons.Trash.ToButton(_ => deleteProduct(p.Id)).Small()
        }).ToTable().Width(Size.Full());

        // File input for CSV import
        var fileInput = UseState<FileInput?>(() => null);

        // Left card - Controls
        var leftCard = new Card(
            Layout.Vertical().Gap(6)
            | Text.H3("Controls")
            | Text.Small($"Total: {products.Value.Count} products")
            
            // Add Product button - opens dialog
            | new Button("Add Product")
                .Icon(Icons.Plus)
                .Primary()
                .Width(Size.Full())
                .HandleClick(_ => isDialogOpen.Value = true)
            
            // Export CSV button
            | new Button("Export CSV")
                .Icon(Icons.Download)
                .Url(downloadUrl.Value)
                .Width(Size.Full())
            
            | new Separator()
            | Text.Small("Import CSV File:")
            
            // Import CSV file input
            | fileInput.ToFileInput(uploadUrl, "Choose File").Accept(".csv")
        ).Title("Management");

        // Right card - Table
        var rightCard = new Card(table).Title("Products");

        // Two-column layout
        return Layout.Horizontal().Gap(8)
            | leftCard.Width(Size.Units(80).Max(350))
            | rightCard.Width(Size.Full());
    }
}
