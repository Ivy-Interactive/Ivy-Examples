namespace SquirrelExample;

[App(icon: Icons.ChartLine, title: "Squirrel Data Chart")]
public class PhysicsSimulationApp : ViewBase
{
    public override object? Build()
    {
        // Load CSV file
        var csvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products.csv");
        
        if (!File.Exists(csvPath))
        {
            return new Card(
                Layout.Vertical()
                | Text.H3("Error")
                | Text.Muted($"CSV file not found: {csvPath}")
            );
        }

        // Load data from CSV using Squirrel
        var originalTable = DataAcquisition.LoadCsv(csvPath);
        
        // Group by Brand and Product Name using Squirrel APIs
        var brandProductCounts = originalTable
            .SplitOn("Brand")
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .SplitOn("Product Name")
                    .Where(inner => !string.IsNullOrWhiteSpace(inner.Key))
                    .ToDictionary(inner => inner.Key, inner => inner.Value.RowCount));
        
        // Get unique product names for dropdown (from all brands)
        var allProductNames = originalTable["Product Name"].OrderBy(t => t).Distinct().ToList();
        
        // State for selected product - start with empty to show placeholder
        var selectedProduct = UseState("");
        
        // Calculate average count from all brand-product combinations
        var allCounts = brandProductCounts.Values
            .SelectMany(bp => bp.Values)
            .ToList();
        var averageCount = allCounts.Count > 0 ? allCounts.Average() : 0.0;
        
        // Create detailed data with brand breakdown for each product using Squirrel Table
        // Use SplitOn to group by Product Name, then by Brand
        var products = originalTable.SplitOn("Product Name");
        var detailedData = new Dictionary<string, Dictionary<string, int>>();
        
        products.Select(t =>
                new
                {
                    Product = t.Key,
                    Brands = t.Value.SplitOn("Brand")
                        .Select(z => new { Brand = z.Key, Count = z.Value.RowCount })
                }
            ).ToList()
            .ForEach(z =>
            {
               detailedData.Add(z.Product, new Dictionary<string, int>());
                foreach (var brand in z.Brands)
                {
                    detailedData[z.Product].Add(brand.Brand, brand.Count);
                }
            });
        
        // Get data for selected product
        var selectedProductData = !string.IsNullOrEmpty(selectedProduct.Value) && detailedData.ContainsKey(selectedProduct.Value)
            ? detailedData[selectedProduct.Value]
            : new Dictionary<string, int>();
        
        // Get brand names from filtered table for selected product
        var filteredTable = !string.IsNullOrEmpty(selectedProduct.Value) && products.ContainsKey(selectedProduct.Value)
            ? products[selectedProduct.Value]
            : null;
        var brandNames = filteredTable != null && filteredTable.RowCount > 0
            ? filteredTable["Brand"].OrderBy(b => b).Distinct().ToList()
            : selectedProductData.Keys.OrderBy(b => b).ToList();
        var brandCounts = brandNames.Select(b => (double)selectedProductData[b]).ToList();
        var selectedProductAverage = brandCounts.Count > 0 ? brandCounts.Average() : 0.0;
        
        // Create chart data for LineChart with Count and Average
        var chartData = brandNames.Select((brand, index) => new
        {
            Brand = brand,
            Count = brandCounts[index],
            Average = selectedProductAverage
        }).ToList();
        
        // Create detailed table for selected product
        var detailedTable = new Squirrel.Table();
        var detailedBrands = new List<string>();
        var detailedCounts = new List<int>();
        
        foreach (var brand in brandNames)
        {
            detailedBrands.Add(brand);
            detailedCounts.Add(selectedProductData[brand]);
        }
        
        detailedTable.AddColumn("Brand", detailedBrands);
        detailedTable.AddColumn("Count", detailedCounts.Select(c => c.ToString()).ToList());

        return Layout.Horizontal()
            | new Card(
                Layout.Vertical()
				| Text.H3("Squirrel Data Chart")
				| Text.Muted($"Analysis of product quantities by brands and names ")
                | Text.Label("Select Product")
                | selectedProduct.ToSelectInput(allProductNames.ToOptions())
                    .Variant(SelectInputs.Select)
                    .Placeholder("Select product")
                | Text.Small("This demo uses Squirrel library to load and manipulate CSV data.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Squirrel](https://github.com/sudipto80/Squirrel)")
                ).Height(Size.Fit().Min(Size.Full()))

                | new Card(
                    Layout.Vertical()
                    | Text.H4(string.IsNullOrEmpty(selectedProduct.Value)
                        ? "Product Chart"
                        : $"Product Chart - {selectedProduct.Value}")
                    | (string.IsNullOrEmpty(selectedProduct.Value)
                        ? Text.Muted("Please select a product from the dropdown above to view the chart.")
                        : selectedProductData.Count > 0
                        ? (
                            Layout.Vertical()
                            | Text.Muted("Line chart shows product counts per brand and the overall average (red line)")
                            | chartData.ToLineChart(style: LineChartStyles.Dashboard)
                                .Dimension("Brand", e => e.Brand)
                                .Measure("Count", e => e.First().Count)
                                .Measure("Average", e => e.First().Average)
                                .Key(selectedProduct.Value)
                            | Text.H4($"Details for {selectedProduct.Value}")
                            | Text.Muted("Table shows product counts per brand")
                            | (detailedTable.RowCount > 0
                                ? detailedTable.Rows.Select(row => new
                                {
                                    Brand = row["Brand"] ?? "",
                                    Count = row["Count"] ?? ""
                                }).ToTable().Width(Size.Full())
                                : Text.Muted("No data available"))
                          )
                        : Text.Muted($"No data available for {selectedProduct.Value}")
                      )
                ).Height(Size.Fit().Min(Size.Full()));
    }
}
