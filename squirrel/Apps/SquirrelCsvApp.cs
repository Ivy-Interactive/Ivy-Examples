namespace SquirrelExample;

[App(icon: Icons.Table, title: "Squirrel Data Editor")]
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
        var selectedBrands = UseState(() => Array.Empty<string>());
        var selectedCategories = UseState(() => Array.Empty<string>());
        
        // Column visibility state - use array for Toggle select
        var visibleColumns = UseState(() => AllColumns.ToArray());
        var page = UseState(1);
        var pageSize = 20;

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
                if (selectedBrands.Value.Length > 0)
                    filtered = filtered.Where(p => selectedBrands.Value.Contains(p.Brand));
                if (selectedCategories.Value.Length > 0)
                    filtered = filtered.Where(p => selectedCategories.Value.Contains(p.Category));

                typedProducts.Value = filtered.ToList();
                // Reset to first page when data changes
                page.Value = 1;
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
        object BuildDataTable(List<FashionProduct> productsSource)
        {
            if (productsSource.Count == 0)
                return new Callout("Data not loaded yet", variant: CalloutVariant.Info);
            if (visibleColumns.Value.Length == 0)
                return new Callout("No columns selected", variant: CalloutVariant.Info);

            var table = productsSource.Select(p => new
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

            return table.Width(Size.Full());
        }

        // Pagination calculations
        var totalRows = typedProducts.Value.Count;
        var numPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
        if (page.Value > numPages) page.Value = numPages;
        var startIndex = Math.Max(0, (page.Value - 1) * pageSize);
        var pagedProducts = typedProducts.Value.Skip(startIndex).Take(pageSize).ToList();
        var showingStart = totalRows == 0 ? 0 : startIndex + 1;
        var showingEnd = totalRows == 0 ? 0 : startIndex + pagedProducts.Count;

        var tableUi = BuildDataTable(pagedProducts);
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
            Layout.Vertical()
            | Text.H3("Data Edit")
            | Text.Muted("Filter, sort, and customize columns")

            | new Card(
                Layout.Vertical()
            | visibleColumns.ToSelectInput(AllColumns.ToOptions())
                .Variant(SelectInputs.Toggle)).Title("Column Visibility")

            | new Card(
                Layout.Vertical()
                | new Card(
                    Layout.Vertical()
                    | Text.Small("Rating Range")
                    | (Layout.Horizontal().Gap(2)
                        | new NumberInput<double?>(ratingMin).Placeholder("Min").Min(0).Max(5).Step(0.1).Width(Size.Fraction(0.5f))
                        | new NumberInput<double?>(ratingMax).Placeholder("Max").Min(0).Max(5).Step(0.1).Width(Size.Fraction(0.5f)))
                    | new Spacer().Height(Size.Units(3))
                    | Text.Small("Price Range")
                    | (Layout.Horizontal().Gap(2)
                        | new NumberInput<decimal?>(priceMin).Placeholder("Min").Min(0).Step(0.01).Width(Size.Fraction(0.5f))
                        | new NumberInput<decimal?>(priceMax).Placeholder("Max").Min(0).Step(0.01).Width(Size.Fraction(0.5f)))
                )

                | new Card(
                    Layout.Vertical()
                    | Text.Small("Brands")
                    | selectedBrands.ToSelectInput(allBrands.ToOptions())
                        .Variant(SelectInputs.Toggle)
                    | Text.Small("Categories")
                    | selectedCategories.ToSelectInput(allCategories.ToOptions())
                        .Variant(SelectInputs.Toggle))
            ).Title("Filters")

            | new Card(
                Layout.Horizontal()
                | (Layout.Vertical()
                    | Text.Small("Sort by field")
                    | sortField.ToSelectInput(SortFieldOptions.ToOptions()).Variant(SelectInputs.Select))
                | (Layout.Vertical()
                    | Text.Small("Sort direction")
                    | sortDirection.ToSelectInput(sortDirections.ToOptions()).Variant(SelectInputs.Select))
            ).Title("Sorting")

            
            | new Button("Export Filtered CSV").Url(exportUrl.Value).Icon(Icons.Download).Disabled(typedProducts.Value.Count == 0).Width(Size.Full())          
            | new Spacer()
            | Text.Small("This demo uses Squirrel library to load and manipulate CSV data.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Squirrel](https://github.com/sudipto80/Squirrel)")
        ).Height(Size.Fit().Min(Size.Full()));

        var right = new Card(
            Layout.Vertical()
            | Text.H3("Data Preview")
            | (dataStats.Value.rows > 0
                ? Text.Muted(totalRows > 0
                    ? $"Total: {totalRows} rows, Showing: {showingStart}-{showingEnd}"
                    : "No data loaded yet")
                : Text.Muted("No data loaded yet"))
            | new Card(tableUi)
            | new Pagination(page.Value, numPages, newPage => page.Set(newPage.Value))
        ).Height(Size.Fit().Min(Size.Full()));

        return Layout.Horizontal()
            | left.Width(Size.Fraction(0.5f))
            | right;
    }
}


