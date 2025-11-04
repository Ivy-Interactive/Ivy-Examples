namespace SquirrelExample;

[App(icon: Icons.ChartLine, title: "Fashion Products Analysis")]
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
        
        // Group by Brand and Product Name together using Squirrel Table's Rows
        var brandProductCounts = new Dictionary<string, Dictionary<string, int>>();
        
        // Use Squirrel Table's Rows collection for iteration
        foreach (var row in originalTable.Rows)
        {
            var brand = Convert.ToString(row["Brand"]) ?? "";
            var productName = Convert.ToString(row["Product Name"]) ?? "";
            
            if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(productName))
                continue;
                
            if (!brandProductCounts.ContainsKey(brand))
                brandProductCounts[brand] = new Dictionary<string, int>();
                
            if (!brandProductCounts[brand].ContainsKey(productName))
                brandProductCounts[brand][productName] = 0;
                
            brandProductCounts[brand][productName]++;
        }
        
        // Get unique product names for dropdown (from all brands)
        var allProductNames = brandProductCounts.Values
            .SelectMany(bp => bp.Keys)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
        
        // State for selected product - start with empty to show placeholder
        var selectedProduct = UseState("");
        
        // Calculate average count from all brand-product combinations
        var allCounts = brandProductCounts.Values
            .SelectMany(bp => bp.Values)
            .ToList();
        var averageCount = allCounts.Count > 0 ? allCounts.Average() : 0.0;
        
        // Create detailed data with brand breakdown for each product using Squirrel Table
        // Use Squirrel Table's Rows collection to process data
        var detailedData = new Dictionary<string, Dictionary<string, int>>();
        
        // Process data using Squirrel Table's row access
        foreach (var row in originalTable.Rows)
        {
            var brand = Convert.ToString(row["Brand"]) ?? "";
            var productName = Convert.ToString(row["Product Name"]) ?? "";
            
            if (string.IsNullOrEmpty(brand) || string.IsNullOrEmpty(productName))
                continue;
                
            if (!detailedData.ContainsKey(productName))
                detailedData[productName] = new Dictionary<string, int>();
                
            if (!detailedData[productName].ContainsKey(brand))
                detailedData[productName][brand] = 0;
                
            detailedData[productName][brand]++;
        }
        
        // Get data for selected product
        var selectedProductData = !string.IsNullOrEmpty(selectedProduct.Value) && detailedData.ContainsKey(selectedProduct.Value)
            ? detailedData[selectedProduct.Value]
            : new Dictionary<string, int>();
        
        var brandNames = selectedProductData.Keys.OrderBy(b => b).ToList();
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

        return Layout.Vertical().Gap(8)
            | new Card(
                Layout.Vertical()
                | Text.H3("Fashion Products Analysis")
                | Text.Muted($"Analysis of product quantities by brands and names. Average count: {averageCount:F2}")
                | new Separator()
                | Text.H4("Select Product")
                | selectedProduct.ToSelectInput(allProductNames.ToOptions())
                    .Variant(SelectInputs.Select)
                    .Placeholder("Select product")
                | new Separator()
                | Text.H4(string.IsNullOrEmpty(selectedProduct.Value) 
                    ? "Product Chart" 
                    : $"Product Chart - {selectedProduct.Value}")
                | (string.IsNullOrEmpty(selectedProduct.Value)
                    ? Text.Muted("Please select a product from the dropdown above to view the chart.")
                    : selectedProductData.Count > 0 
                    ? Layout.Vertical()
                        | Text.Small($"Average count: {selectedProductAverage:F2} products (shown as red line)")
                        | chartData.ToLineChart(style: LineChartStyles.Dashboard)
                            .Dimension("Brand", e => e.Brand)
                            .Measure("Count", e => e.First().Count)
                            .Measure("Average", e => e.First().Average)
                            .Key(selectedProduct.Value)
                        | new Separator()
                        | Text.H4($"Details for {selectedProduct.Value}")
                        | (detailedTable.RowCount > 0
                            ? detailedTable.Rows.Select(row => new
                            {
                                Brand = Convert.ToString(row["Brand"]) ?? "",
                                Count = Convert.ToString(row["Count"]) ?? ""
                            }).ToTable().Width(Size.Full())
                            : Text.Muted("No data available"))
                    : Text.Muted($"No data available for {selectedProduct.Value}"))
            );
    }
}
