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

        // Export current filtered and sorted data to CSV (only visible columns)
        var exportUrl = this.UseDownload(
            () =>
            {
                if (typedProducts.Value.Count == 0)
                {
                    return Array.Empty<byte>();
                }

                // Create CSV content from current filtered/sorted data
                using var ms = new MemoryStream();
                using var writer = new StreamWriter(ms, leaveOpen: true);
                
                // Build header and column order based on visible columns
                var columns = new List<(string name, Func<FashionProduct, string> getValue)>();
                
                if (visibleColumns.Value.Contains("User ID"))
                    columns.Add(("User ID", p => p.UserId.ToString(CultureInfo.InvariantCulture)));
                if (visibleColumns.Value.Contains("Product ID"))
                    columns.Add(("Product ID", p => p.ProductId.ToString(CultureInfo.InvariantCulture)));
                if (visibleColumns.Value.Contains("Product Name"))
                    columns.Add(("Product Name", p => EscapeCsvField(p.ProductName)));
                if (visibleColumns.Value.Contains("Brand"))
                    columns.Add(("Brand", p => EscapeCsvField(p.Brand)));
                if (visibleColumns.Value.Contains("Category"))
                    columns.Add(("Category", p => EscapeCsvField(p.Category)));
                if (visibleColumns.Value.Contains("Price"))
                    columns.Add(("Price", p => p.Price.ToString(CultureInfo.InvariantCulture)));
                if (visibleColumns.Value.Contains("Rating"))
                    columns.Add(("Rating", p => p.Rating.ToString(CultureInfo.InvariantCulture)));
                if (visibleColumns.Value.Contains("Color"))
                    columns.Add(("Color", p => EscapeCsvField(p.Color)));
                if (visibleColumns.Value.Contains("Size"))
                    columns.Add(("Size", p => EscapeCsvField(p.Size)));
                
                // Write header
                writer.WriteLine(string.Join(",", columns.Select(c => c.name)));
                
                // Write data rows
                foreach (var product in typedProducts.Value)
                {
                    var line = string.Join(",", columns.Select(c => c.getValue(product)));
                    writer.WriteLine(line);
                }
                
                writer.Flush();
                ms.Position = 0;
                return ms.ToArray();
            },
            "text/csv",
            "fashion_products_filtered.csv"
        );
        
        // Helper function to escape CSV fields
        string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        // Build DataTable with visible columns only
        object BuildDataTable()
        {
            if (typedProducts.Value.Count == 0)
                return new Callout("Data not loaded yet", variant: CalloutVariant.Info);

            if (visibleColumns.Value.Count == 0)
                return new Callout("No columns selected", variant: CalloutVariant.Info);

            // Build table with only visible columns by creating anonymous objects conditionally
            var table = typedProducts.Value.Select(p => new
            {
                UserID = visibleColumns.Value.Contains("User ID") ? p.UserId : (int?)null,
                ProductID = visibleColumns.Value.Contains("Product ID") ? p.ProductId : (int?)null,
                ProductName = visibleColumns.Value.Contains("Product Name") ? p.ProductName : null,
                Brand = visibleColumns.Value.Contains("Brand") ? p.Brand : null,
                Category = visibleColumns.Value.Contains("Category") ? p.Category : null,
                Price = visibleColumns.Value.Contains("Price") ? p.Price : (decimal?)null,
                Rating = visibleColumns.Value.Contains("Rating") ? p.Rating : (double?)null,
                Color = visibleColumns.Value.Contains("Color") ? p.Color : null,
                Size = visibleColumns.Value.Contains("Size") ? p.Size : null
            }).ToTable()
            .RemoveEmptyColumns();
            
            // Apply proper headers for visible columns only
            if (visibleColumns.Value.Contains("User ID"))
                table = table.Header(p => p.UserID, "User ID");
            if (visibleColumns.Value.Contains("Product ID"))
                table = table.Header(p => p.ProductID, "Product ID");
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
                {
                    var colState = UseState(() => visibleColumns.Value.Contains(col));
                    UseEffect(() => colState.Set(visibleColumns.Value.Contains(col)), [visibleColumns]);
                    return new BoolInput(colState.Value, e =>
                    {
                        var cols = new HashSet<string>(visibleColumns.Value);
                        if (e.Value) cols.Add(col); else cols.Remove(col);
                        visibleColumns.Value = cols;
                        colState.Set(e.Value);
                    }).Label(col);
                }).ToArray()
            
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
                {
                    var brandState = UseState(() => selectedBrands.Value.Contains(brand));
                    UseEffect(() => brandState.Set(selectedBrands.Value.Contains(brand)), [selectedBrands]);
                    return new BoolInput(brandState.Value, e =>
                    {
                        var brands = new HashSet<string>(selectedBrands.Value);
                        if (e.Value) brands.Add(brand); else brands.Remove(brand);
                        selectedBrands.Value = brands;
                        brandState.Set(e.Value);
                    }).Label(brand);
                }).ToArray()
            | Text.Label("Categories")
            | Layout.Vertical().Gap(2)
                | allCategories.Select(category =>
                {
                    var categoryState = UseState(() => selectedCategories.Value.Contains(category));
                    UseEffect(() => categoryState.Set(selectedCategories.Value.Contains(category)), [selectedCategories]);
                    return new BoolInput(categoryState.Value, e =>
                    {
                        var cats = new HashSet<string>(selectedCategories.Value);
                        if (e.Value) cats.Add(category); else cats.Remove(category);
                        selectedCategories.Value = cats;
                        categoryState.Set(e.Value);
                    }).Label(category);
                }).ToArray()
            
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
            | new Button("Export Filtered CSV").Url(exportUrl.Value)
                .Icon(Icons.Download)
                .Disabled(typedProducts.Value.Count == 0)
            | Text.Muted("Export current filtered and sorted data")
            
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


