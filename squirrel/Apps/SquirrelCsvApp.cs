namespace SquirrelExample;

[App(icon: Icons.Table, title: "Squirrel CSV Demo")]
public class SquirrelCsvApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // UI State
        var dataStats = UseState(() => (rows: 0, cols: 0));
        var typedProducts = UseState(() => new List<FashionProduct>());
        var originalProducts = UseState(() => new List<FashionProduct>());
        
        // Sorting state
        var sortField = UseState("None");
        var sortDirection = UseState("Ascending");
        
        // Filter state
        var ratingMin = UseState(() => (double?)null);
        var ratingMax = UseState(() => (double?)null);
        var priceMin = UseState(() => (decimal?)null);
        var priceMax = UseState(() => (decimal?)null);
        var selectedBrands = UseState(() => new HashSet<string>());
        var selectedCategories = UseState(() => new HashSet<string>());
        
        // Column visibility state
        var visibleColumns = UseState(() => new HashSet<string>
        {
            "User ID", "Product ID", "Product Name", "Brand", "Category", "Price", "Rating", "Color", "Size"
        });

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

                originalProducts.Value = products;
                typedProducts.Value = products;
                dataStats.Value = (sqTable.RowCount, 9);

                client.Toast($"Loaded {rowCount} / {sqTable.RowCount} rows", "Squirrel");
            }
            catch (Exception ex)
            {
                client.Error(ex);
            }
        }

        // Apply filters and sorting
        void ApplyFiltersAndSort()
        {
            if (originalProducts.Value.Count == 0)
            {
                typedProducts.Value = new List<FashionProduct>();
                return;
            }

            var filtered = originalProducts.Value.AsEnumerable();

            // Apply filters
            if (ratingMin.Value.HasValue)
                filtered = filtered.Where(p => p.Rating >= ratingMin.Value.Value);
            if (ratingMax.Value.HasValue)
                filtered = filtered.Where(p => p.Rating <= ratingMax.Value.Value);
            if (priceMin.Value.HasValue)
                filtered = filtered.Where(p => p.Price >= priceMin.Value.Value);
            if (priceMax.Value.HasValue)
                filtered = filtered.Where(p => p.Price <= priceMax.Value.Value);
            if (selectedBrands.Value.Count > 0)
                filtered = filtered.Where(p => selectedBrands.Value.Contains(p.Brand));
            if (selectedCategories.Value.Count > 0)
                filtered = filtered.Where(p => selectedCategories.Value.Contains(p.Category));

            // Apply sorting
            if (!string.IsNullOrEmpty(sortField.Value) && sortField.Value != "None")
            {
                filtered = sortField.Value switch
                {
                    "User ID" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.UserId)
                        : filtered.OrderByDescending(p => p.UserId),
                    "Product ID" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.ProductId)
                        : filtered.OrderByDescending(p => p.ProductId),
                    "Product Name" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.ProductName)
                        : filtered.OrderByDescending(p => p.ProductName),
                    "Brand" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Brand)
                        : filtered.OrderByDescending(p => p.Brand),
                    "Category" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Category)
                        : filtered.OrderByDescending(p => p.Category),
                    "Price" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Price)
                        : filtered.OrderByDescending(p => p.Price),
                    "Rating" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Rating)
                        : filtered.OrderByDescending(p => p.Rating),
                    "Color" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Color)
                        : filtered.OrderByDescending(p => p.Color),
                    "Size" => sortDirection.Value == "Ascending"
                        ? filtered.OrderBy(p => p.Size)
                        : filtered.OrderByDescending(p => p.Size),
                    _ => filtered
                };
            }

            typedProducts.Value = filtered.ToList();
        }

        // Auto-apply filters and sorting when any filter/sort changes
        UseEffect(ApplyFiltersAndSort, [sortField, sortDirection, ratingMin, ratingMax, priceMin, priceMax, selectedBrands, selectedCategories]);
        
        // Rebuild table when visible columns change
        UseEffect(() => { }, [visibleColumns]);

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

        // Build DataTable with visible columns only
        object BuildDataTable()
        {
            if (typedProducts.Value.Count == 0)
                return new Callout("Data not loaded yet", variant: CalloutVariant.Info);

            var table = typedProducts.Value.AsQueryable().ToTable();
            
            if (visibleColumns.Value.Contains("User ID"))
                table = table.Header(p => p.UserId, "User ID");
            if (visibleColumns.Value.Contains("Product ID"))
                table = table.Header(p => p.ProductId, "Product ID");
            if (visibleColumns.Value.Contains("Product Name"))
                table = table.Header(p => p.ProductName, "Product Name");
            if (visibleColumns.Value.Contains("Brand"))
                table = table.Header(p => p.Brand, "Brand");
            if (visibleColumns.Value.Contains("Category"))
                table = table.Header(p => p.Category, "Category");
            if (visibleColumns.Value.Contains("Price"))
                table = table.Header(p => p.Price, "Price");
            if (visibleColumns.Value.Contains("Rating"))
                table = table.Header(p => p.Rating, "Rating");
            if (visibleColumns.Value.Contains("Color"))
                table = table.Header(p => p.Color, "Color");
            if (visibleColumns.Value.Contains("Size"))
                table = table.Header(p => p.Size, "Size");

            return table;
        }

        var tableUi = BuildDataTable();

        // Field options for sorting (including "None" option)
        var sortFieldOptions = new[] { "None", "User ID", "Product ID", "Product Name", "Brand", "Category", "Price", "Rating", "Color", "Size" };
        var sortDirections = new[] { "Ascending", "Descending" };
        
        // Get unique values for filters
        var allBrands = originalProducts.Value.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList();
        var allCategories = originalProducts.Value.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
        var allColumns = new[] { "User ID", "Product ID", "Product Name", "Brand", "Category", "Price", "Rating", "Color", "Size" };

        var left = new Card(
            Layout.Vertical().Gap(6).Scroll()
            | Text.H3("Data Edit")
            | Text.Muted("Filter, sort, and customize columns")
            
            | new Separator()
            | Text.Small("Column Visibility")
            | Layout.Vertical().Gap(2)
                | allColumns.Select(col => 
                    Layout.Horizontal().Gap(2)
                        | (visibleColumns.Value.Contains(col) ? Icons.Square : Icons.Square)
                        | Text.Small(col)
                        | new Spacer()
                        | new Button(visibleColumns.Value.Contains(col) ? "Hide" : "Show", _ =>
                        {
                            var cols = new HashSet<string>(visibleColumns.Value);
                            if (cols.Contains(col)) cols.Remove(col); else cols.Add(col);
                            visibleColumns.Value = cols;
                        }).Small()
                ).ToArray()
            
            | new Separator()
            | Text.Small("Filters")
            | Text.Label("Rating Range")
            | Layout.Horizontal().Gap(2)
                | new NumberInput<double?>(ratingMin).Placeholder("Min").Min(0).Max(5).Step(0.1).Width(Size.Fraction(0.5f))
                | new NumberInput<double?>(ratingMax).Placeholder("Max").Min(0).Max(5).Step(0.1).Width(Size.Fraction(0.5f))
            | Text.Label("Price Range")
            | Layout.Horizontal().Gap(2)
                | new NumberInput<decimal?>(priceMin).Placeholder("Min").Min(0).Step(0.01).Width(Size.Fraction(0.5f))
                | new NumberInput<decimal?>(priceMax).Placeholder("Max").Min(0).Step(0.01).Width(Size.Fraction(0.5f))
            | Text.Label("Brands")
            | Layout.Vertical().Gap(2)
                | allBrands.Select(brand =>
                    Layout.Horizontal().Gap(2)
                        | (selectedBrands.Value.Contains(brand) ? Icons.Square : Icons.Square)
                        | Text.Small(brand)
                        | new Spacer()
                        | new Button(selectedBrands.Value.Contains(brand) ? "Remove" : "Add", _ =>
                        {
                            var brands = new HashSet<string>(selectedBrands.Value);
                            if (brands.Contains(brand)) brands.Remove(brand); else brands.Add(brand);
                            selectedBrands.Value = brands;
                        }).Small()
                ).ToArray()
            | Text.Label("Categories")
            | Layout.Vertical().Gap(2)
                | allCategories.Select(category =>
                    Layout.Horizontal().Gap(2)
                        | (selectedCategories.Value.Contains(category) ? Icons.Square : Icons.Square)
                        | Text.Small(category)
                        | new Spacer()
                        | new Button(selectedCategories.Value.Contains(category) ? "Remove" : "Add", _ =>
                        {
                            var cats = new HashSet<string>(selectedCategories.Value);
                            if (cats.Contains(category)) cats.Remove(category); else cats.Add(category);
                            selectedCategories.Value = cats;
                        }).Small()
                ).ToArray()
            
            | new Separator()
            | Text.Small("Sorting")
            | Text.Label("Sort by field")
            | sortField.ToSelectInput(sortFieldOptions.ToOptions())
                .Variant(SelectInputs.Select)
            | Text.Label("Sort direction")
            | sortDirection.ToSelectInput(sortDirections.ToOptions())
                .Variant(SelectInputs.Select)
            
            | new Separator()
            | Text.Small("Actions")
            | new Button("Export CSV").Url(exportUrl.Value)
            
            | new Separator()
            | (dataStats.Value.rows > 0
                ? Text.Muted($"Total: {dataStats.Value.rows} rows, Showing: {typedProducts.Value.Count} rows")
                : Text.Muted("No data loaded yet"))
        ).Height(Size.Fit().Min(Size.Full()));

        var right = new Card(
            Layout.Vertical()
            | Text.H3("Data Preview")
            | tableUi
        ).Height(Size.Fit().Min(Size.Full()));

        return Layout.Horizontal().Gap(8)
            | left.Width(Size.Fraction(0.35f))
            | right.Width(Size.Fraction(0.65f));
    }
}


