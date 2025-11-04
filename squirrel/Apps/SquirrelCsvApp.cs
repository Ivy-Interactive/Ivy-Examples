namespace SquirrelExample.Apps;

[App(icon: Icons.Table, title: "Squirrel CSV Demo")]
public class SquirrelCsvApp : ViewBase
{
    // Strongly-typed model for typical fashion products CSV
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
    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // UI State
        var dataStats = UseState(() => (rows: 0, cols: 0));
        var typedProducts = UseState(() => new List<FashionProduct>());

        // Load from local CSV using Squirrel (no upload)
        void LoadFromCsv()
        {
            var csvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products.csv");
            if (!File.Exists(csvPath))
            {
                client.Toast($"CSV file not found: {csvPath}", "Squirrel");
                return;
            }

            try
            {
                // Squirrel pipeline: load → clean/normalize → sort
                var sqTable = DataAcquisition.LoadCsv(csvPath);
                int rowCount = Math.Min(sqTable.RowCount, 100);

                // Map directly from Squirrel Table by column name
                int ToInt(object? v)
                    => int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
                decimal ToDecimal(object? v)
                    => decimal.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
                double ToDouble(object? v)
                    => double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0d;
                string ToText(object? v) => Convert.ToString(v, CultureInfo.InvariantCulture);

                var products = new List<FashionProduct>(rowCount);
                for (int r = 0; r < rowCount; r++)
                {
                    object? V(string col)
                    {
                        try { return sqTable[r][col]; } catch { return null; }
                    }

                    products.Add(new FashionProduct
                    {
                        UserId = ToInt(V("User ID")),
                        ProductId = ToInt(V("Product ID")),
                        ProductName = ToText(V("Product Name")),
                        Brand = ToText(V("Brand")),
                        Category = ToText(V("Category")),
                        Price = ToDecimal(V("Price")),
                        Rating = ToDouble(V("Rating")),
                        Color = ToText(V("Color")),
                        Size = ToText(V("Size"))
                    });
                }

                typedProducts.Value = products;
                dataStats.Value = (sqTable.RowCount, 9);

                client.Toast($"Loaded {rowCount} / {sqTable.RowCount} rows", "Squirrel");
            }
            catch (Exception ex)
            {
                client.Error(ex);
            }
        }

        // Auto-load on first render
        UseEffect(LoadFromCsv);

        // Export current data via Squirrel ToCsv
        var exportUrl = this.UseDownload(
            () =>
            {
                var csvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products.csv");
                return File.ReadAllBytes(csvPath);
            },
            "text/csv",
            "fashion_products.csv"
        );

        // DataTable view of typed products
        object tableUi = typedProducts.Value.Count > 0
            ? typedProducts.Value.AsQueryable().ToTable()
                .Header(p => p.UserId, "User ID")
                .Header(p => p.ProductId, "Product ID")
                .Header(p => p.ProductName, "Product Name")
                .Header(p => p.Brand, "Brand")
                .Header(p => p.Category, "Category")
                .Header(p => p.Price, "Price")
                .Header(p => p.Rating, "Rating")
                .Header(p => p.Color, "Color")
                .Header(p => p.Size, "Size")
            : new Callout("Data not loaded yet", variant: CalloutVariant.Info);

        var left = new Card(
            Layout.Vertical().Gap(8)
            | Text.H3("Squirrel: CSV → Clean → DataTable")
            | Text.Small("DataAcquisition.LoadCsv(...).NormalizeColumn(...).SortBy(...)")
            | new Button("Reload", _ => LoadFromCsv())
            | new Button("Export CSV").Url(exportUrl.Value)
            | (dataStats.Value.rows > 0
                ? Text.Muted($"Rows: {dataStats.Value.rows}, Columns: {dataStats.Value.cols}")
                : Text.Muted("No data loaded yet"))
        ).Title("Controls").Height(Size.Fit().Min(Size.Full()));

        var right = new Card(
            Layout.Vertical()
            | tableUi
        ).Title("Data Preview").Height(Size.Fit().Min(Size.Full()));

        return Layout.Horizontal().Gap(8)
            | left.Width(Size.Fraction(0.35f))
            | right.Width(Size.Fraction(0.65f));
    }
}


