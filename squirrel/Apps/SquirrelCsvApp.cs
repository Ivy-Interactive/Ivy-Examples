namespace SquirrelExample;

[App(icon: Icons.Table, title: "Squirrel CSV Demo")]
public class SquirrelCsvApp : ViewBase
{
    private static readonly string[] AllColumns = { "User ID", "Product ID", "Product Name", "Brand", "Category", "Price", "Rating", "Color", "Size" };
    private static readonly string[] SortFieldOptions = { "None", "User ID", "Product ID", "Product Name", "Brand", "Category", "Price", "Rating", "Color", "Size" };

    private static readonly string CsvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products.csv");
    private static readonly string WorkingCsvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products_working.csv");

    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // UI State
        var dataStats = UseState(() => (rows: 0, cols: 0));
        var typedProducts = UseState(() => new List<FashionProduct>());
        var originalTable = UseState<Squirrel.Table?>(() => null);
        
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
        var visibleColumns = UseState(() => new HashSet<string>(AllColumns));

        // Load from CSV file using Squirrel
        void LoadFromCsv()
        {
            if (!File.Exists(CsvPath))
            {
                client.Toast($"CSV file not found: {CsvPath}", "Squirrel");
                return;
            }

            try
            {
                // Load original file into Squirrel Table
                var sqTable = DataAcquisition.LoadCsv(CsvPath);
                originalTable.Value = sqTable;
                dataStats.Value = (sqTable.RowCount, AllColumns.Length);
                ApplyFiltersAndSort();
                client.Toast($"Loaded {sqTable.RowCount} rows from file", "Squirrel");
            }
            catch (Exception ex)
            {
                client.Error(ex);
            }
        }

        // Apply filters and sorting using Squirrel Table operations
        void ApplyFiltersAndSort()
        {
            if (originalTable.Value == null)
            {
                typedProducts.Value = new List<FashionProduct>();
                return;
            }

            try
            {
                // Start with original table
                var table = originalTable.Value;


                // Apply sorting using Squirrel SortBy
                if (sortField.Value != "None")
                {
                    table = table.SortBy(sortField.Value);
                }

                // Save processed table to working file
                SaveTableToFile(table, WorkingCsvPath);

                // Convert filtered and sorted table to products
                var products = ConvertTableToProducts(table);
                
                // Apply descending sort if needed (reverse the list)
                if (sortField.Value != "None" && sortDirection.Value == "Descending")
                {
                    products.Reverse();
                }
                
                // Apply additional filters that Squirrel doesn't support directly
                var filtered = products.AsEnumerable();
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

                typedProducts.Value = filtered.ToList();
            }
            catch (Exception ex)
            {
                client.Error(ex);
            }
        }

        // Convert Squirrel Table to List<FashionProduct>
        List<FashionProduct> ConvertTableToProducts(Squirrel.Table table)
        {
            int ToInt(object? v) => int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
            decimal ToDecimal(object? v) => decimal.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
            double ToDouble(object? v) => double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0d;
            string ToText(object? v) => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";

            var products = new List<FashionProduct>(table.RowCount);
            for (int r = 0; r < table.RowCount; r++)
            {
                object? V(string col)
                {
                    try { return table[r][col]; } catch { return null; }
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
            return products;
        }

        // Save Squirrel Table to CSV file
        void SaveTableToFile(Squirrel.Table table, string filePath)
        {
            // Write CSV manually
            using var writer = new StreamWriter(filePath);
            // Write headers
            writer.WriteLine(string.Join(",", AllColumns));
            // Write rows
            for (int i = 0; i < table.RowCount; i++)
            {
                var row = table[i];
                var values = AllColumns.Select(col => 
                {
                    try { return EscapeCsvField(Convert.ToString(row[col])); }
                    catch { return ""; }
                });
                writer.WriteLine(string.Join(",", values));
            }
        }

        string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }

        // Auto-apply filters and sorting when any filter/sort changes
        UseEffect(ApplyFiltersAndSort, [sortField, sortDirection, ratingMin, ratingMax, priceMin, priceMax, selectedBrands, selectedCategories]);
        
        // Rebuild table when visible columns change
        UseEffect(() => { }, [visibleColumns]);

        // Auto-load on first render
        UseEffect(LoadFromCsv);

        // Column definitions for CSV export
        var columnDefs = new Dictionary<string, Func<FashionProduct, string>>
        {
            { "User ID", p => p.UserId.ToString(CultureInfo.InvariantCulture) },
            { "Product ID", p => p.ProductId.ToString(CultureInfo.InvariantCulture) },
            { "Product Name", p => EscapeCsvField(p.ProductName) },
            { "Brand", p => EscapeCsvField(p.Brand) },
            { "Category", p => EscapeCsvField(p.Category) },
            { "Price", p => p.Price.ToString(CultureInfo.InvariantCulture) },
            { "Rating", p => p.Rating.ToString(CultureInfo.InvariantCulture) },
            { "Color", p => EscapeCsvField(p.Color) },
            { "Size", p => EscapeCsvField(p.Size) }
        };

        // Export current filtered and sorted data to CSV (only visible columns)
        var exportUrl = this.UseDownload(
            () =>
            {
                if (typedProducts.Value.Count == 0) return Array.Empty<byte>();

                using var ms = new MemoryStream();
                using var writer = new StreamWriter(ms, leaveOpen: true);
                
                var visibleCols = AllColumns.Where(c => visibleColumns.Value.Contains(c)).ToList();
                writer.WriteLine(string.Join(",", visibleCols));
                
                foreach (var product in typedProducts.Value)
                {
                    var line = string.Join(",", visibleCols.Select(col => columnDefs[col](product)));
                    writer.WriteLine(line);
                }
                
                writer.Flush();
                ms.Position = 0;
                return ms.ToArray();
            },
            "text/csv",
            "fashion_products_filtered.csv"
        );

        // Build DataTable with visible columns only
        object BuildDataTable()
        {
            if (typedProducts.Value.Count == 0)
                return new Callout("Data not loaded yet", variant: CalloutVariant.Info);
            if (visibleColumns.Value.Count == 0)
                return new Callout("No columns selected", variant: CalloutVariant.Info);

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
            }).ToTable().RemoveEmptyColumns();
            
            // Apply headers for visible columns
            if (visibleColumns.Value.Contains("User ID")) table = table.Header(p => p.UserID, "User ID");
            if (visibleColumns.Value.Contains("Product ID")) table = table.Header(p => p.ProductID, "Product ID");
            if (visibleColumns.Value.Contains("Product Name")) table = table.Header(p => p.ProductName, "Product Name");
            if (visibleColumns.Value.Contains("Brand")) table = table.Header(p => p.Brand, "Brand");
            if (visibleColumns.Value.Contains("Category")) table = table.Header(p => p.Category, "Category");
            if (visibleColumns.Value.Contains("Price")) table = table.Header(p => p.Price, "Price");
            if (visibleColumns.Value.Contains("Rating")) table = table.Header(p => p.Rating, "Rating");
            if (visibleColumns.Value.Contains("Color")) table = table.Header(p => p.Color, "Color");
            if (visibleColumns.Value.Contains("Size")) table = table.Header(p => p.Size, "Size");

            return table;
        }

        var tableUi = BuildDataTable();
        var sortDirections = new[] { "Ascending", "Descending" };
        
        // Get unique brands and categories from original table (not filtered)
        List<string> GetUniqueValuesFromOriginalTable(string columnName)
        {
            if (originalTable.Value == null) return new List<string>();
            var values = new HashSet<string>();
            for (int i = 0; i < originalTable.Value.RowCount; i++)
            {
                try
                {
                    var value = Convert.ToString(originalTable.Value[i][columnName]) ?? "";
                    if (!string.IsNullOrEmpty(value))
                        values.Add(value);
                }
                catch { }
            }
            return values.OrderBy(v => v).ToList();
        }
        
        var allBrands = GetUniqueValuesFromOriginalTable("Brand");
        var allCategories = GetUniqueValuesFromOriginalTable("Category");

        var left = new Card(
            Layout.Vertical().Gap(6).Scroll()
            | Text.H3("Data Edit")
            | Text.Muted("Filter, sort, and customize columns")
            
            | new Separator()
            | Text.Small("Column Visibility")
            | Layout.Vertical().Gap(2)
                | AllColumns.Select(col => 
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
                | BuildFilterCheckboxes(allBrands, selectedBrands)
            | Text.Label("Categories")
            | Layout.Vertical().Gap(2)
                | BuildFilterCheckboxes(allCategories, selectedCategories)
            
            | new Separator()
            | Text.Small("Sorting")
            | Text.Label("Sort by field")
            | sortField.ToSelectInput(SortFieldOptions.ToOptions()).Variant(SelectInputs.Select)
            | Text.Label("Sort direction")
            | sortDirection.ToSelectInput(sortDirections.ToOptions()).Variant(SelectInputs.Select)
            
            | new Separator()
            | Text.Small("Actions")
            | new Button("Export Filtered CSV").Url(exportUrl.Value).Icon(Icons.Download).Disabled(typedProducts.Value.Count == 0)
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

    // Helper method to build filter checkboxes
    object[] BuildFilterCheckboxes(List<string> items, IState<HashSet<string>> selectedSet)
    {
        return items.Select<string, object>(item =>
        {
            var itemState = UseState(() => selectedSet.Value.Contains(item));
            UseEffect(() => itemState.Set(selectedSet.Value.Contains(item)), [selectedSet]);
            return new BoolInput(itemState.Value, e =>
            {
                var set = new HashSet<string>(selectedSet.Value);
                if (e.Value) set.Add(item); else set.Remove(item);
                selectedSet.Value = set;
                itemState.Set(e.Value);
            }).Label(item);
        }).ToArray();
    }
}


