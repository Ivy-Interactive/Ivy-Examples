using System.Data;
using SnowflakeExample.Services;

namespace SnowflakeExample.Apps;

/// <summary>
/// Brand Dashboard - Analytics dashboard for top brands from PART table
/// </summary>
[App(icon: Icons.ChartBar, title: "Brand Dashboard")]
public class BrandDashboardApp : ViewBase
{
    public override object? Build()
    {
        var snowflakeService = this.UseService<SnowflakeService>();
        var refreshToken = this.UseRefreshToken();

        // State management
        var brandData = this.UseState<List<BrandStats>>(() => new List<BrandStats>());
        var totalBrandsCount = this.UseState<long>(() => 0); // Total count of all brands in table
        var totalItemsCount = this.UseState<long>(() => 0); // Total count of all items in table
        var totalAvgItemsPerBrand = this.UseState<double>(() => 0); // Average items per brand for all brands
        var totalAvgPrice = this.UseState<double>(() => 0); // Average price for all items
        var totalMinPrice = this.UseState<double>(() => 0); // Minimum price for all items
        var totalMaxPrice = this.UseState<double>(() => 0); // Maximum price for all items
        var totalInventoryValue = this.UseState<double>(() => 0); // Total inventory value for all brands
        var totalTotalSize = this.UseState<long>(() => 0); // Total size for all items
        var totalAvgSize = this.UseState<double>(() => 0); // Average size for all items
        var isLoading = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        var sortBy = this.UseState<string>("ItemCount"); // ItemCount, AvgPrice, MinPrice, MaxPrice, TotalSize, AvgSize, Brand
        var sortOrder = this.UseState<string>("DESC"); // ASC, DESC
        var limit = this.UseState<int>(7); // Number of brands to load

        // Load data on mount
        this.UseEffect(async () =>
        {
            isLoading.Value = true;
            errorMessage.Value = null;

            try
            {
                refreshToken.Refresh();

                // First, get total count of all brands and items in the table
                var brandsCountSql = @"
                    SELECT COUNT(DISTINCT P_BRAND) as TotalBrands
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var brandsCountResult = await snowflakeService.ExecuteScalarAsync(brandsCountSql);
                totalBrandsCount.Value = brandsCountResult != null ? Convert.ToInt64(brandsCountResult) : 0;

                // Get total count of all items in the table
                var itemsCountSql = @"
                    SELECT COUNT(*) as TotalItems
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var itemsCountResult = await snowflakeService.ExecuteScalarAsync(itemsCountSql);
                totalItemsCount.Value = itemsCountResult != null ? Convert.ToInt64(itemsCountResult) : 0;

                // Get overall metrics for all brands
                var overallMetricsSql = @"
                    SELECT 
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice,
                        SUM(P_RETAILPRICE) as TotalValue,
                        SUM(P_SIZE) as TotalSize,
                        AVG(P_SIZE) as AvgSize
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var overallMetricsTable = await snowflakeService.ExecuteQueryAsync(overallMetricsSql);
                if (overallMetricsTable.Rows.Count > 0)
                {
                    var row = overallMetricsTable.Rows[0];
                    totalAvgPrice.Value = Convert.ToDouble(row["AvgPrice"] ?? 0);
                    totalMinPrice.Value = Convert.ToDouble(row["MinPrice"] ?? 0);
                    totalMaxPrice.Value = Convert.ToDouble(row["MaxPrice"] ?? 0);
                    totalInventoryValue.Value = Convert.ToDouble(row["TotalValue"] ?? 0);
                    totalTotalSize.Value = Convert.ToInt64(row["TotalSize"] ?? 0);
                    totalAvgSize.Value = Convert.ToDouble(row["AvgSize"] ?? 0);
                }

                // Calculate average items per brand for all brands
                totalAvgItemsPerBrand.Value = totalBrandsCount.Value > 0
                    ? totalItemsCount.Value / (double)totalBrandsCount.Value
                    : 0;

                // Build ORDER BY clause based on sortBy and sortOrder
                var orderByColumn = sortBy.Value switch
                {
                    "AvgPrice" => "AVG(P_RETAILPRICE)",
                    "MinPrice" => "MIN(P_RETAILPRICE)",
                    "MaxPrice" => "MAX(P_RETAILPRICE)",
                    "TotalSize" => "SUM(P_SIZE)",
                    "AvgSize" => "AVG(P_SIZE)",
                    "Brand" => "P_BRAND",
                    _ => "COUNT(*)" // Default to ItemCount
                };
                
                var orderDirection = sortOrder.Value == "ASC" ? "ASC" : "DESC";
                
                // Query to get top brands with metrics, using dynamic ORDER BY and LIMIT
                var sql = $@"
                    SELECT 
                        P_BRAND as Brand,
                        COUNT(*) as ItemCount,
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice,
                        SUM(P_SIZE) as TotalSize,
                        AVG(P_SIZE) as AvgSize
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL
                    GROUP BY P_BRAND
                    ORDER BY {orderByColumn} {orderDirection}
                    LIMIT {limit.Value}";

                var dataTable = await snowflakeService.ExecuteQueryAsync(sql);

                var brands = new List<BrandStats>();
                foreach (DataRow row in dataTable.Rows)
                {
                    brands.Add(new BrandStats
                    {
                        Brand = row["Brand"]?.ToString() ?? "Unknown",
                        ItemCount = Convert.ToInt64(row["ItemCount"] ?? 0),
                        AvgPrice = Convert.ToDouble(row["AvgPrice"] ?? 0),
                        MinPrice = Convert.ToDouble(row["MinPrice"] ?? 0),
                        MaxPrice = Convert.ToDouble(row["MaxPrice"] ?? 0),
                        TotalSize = Convert.ToInt64(row["TotalSize"] ?? 0),
                        AvgSize = Convert.ToDouble(row["AvgSize"] ?? 0)
                    });
                }

                brandData.Value = brands;
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading brand data: {ex.Message}";
            }
            finally
            {
                isLoading.Value = false;
            }
        }, []);
        
        // Reload data when parameters change
        this.UseEffect(async () =>
        {
            if (brandData.Value.Count == 0) return; // Skip if initial load hasn't happened yet
            
            isLoading.Value = true;
            errorMessage.Value = null;

            try
            {
                refreshToken.Refresh();

                // Build ORDER BY clause based on sortBy and sortOrder
                var orderByColumn = sortBy.Value switch
                {
                    "AvgPrice" => "AVG(P_RETAILPRICE)",
                    "MinPrice" => "MIN(P_RETAILPRICE)",
                    "MaxPrice" => "MAX(P_RETAILPRICE)",
                    "TotalSize" => "SUM(P_SIZE)",
                    "AvgSize" => "AVG(P_SIZE)",
                    "Brand" => "P_BRAND",
                    _ => "COUNT(*)" // Default to ItemCount
                };
                
                var orderDirection = sortOrder.Value == "ASC" ? "ASC" : "DESC";
                
                // Query to get top brands with metrics, using dynamic ORDER BY and LIMIT
                var sql = $@"
                    SELECT 
                        P_BRAND as Brand,
                        COUNT(*) as ItemCount,
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice,
                        SUM(P_SIZE) as TotalSize,
                        AVG(P_SIZE) as AvgSize
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL
                    GROUP BY P_BRAND
                    ORDER BY {orderByColumn} {orderDirection}
                    LIMIT {limit.Value}";

                var dataTable = await snowflakeService.ExecuteQueryAsync(sql);

                var brands = new List<BrandStats>();
                foreach (DataRow row in dataTable.Rows)
                {
                    brands.Add(new BrandStats
                    {
                        Brand = row["Brand"]?.ToString() ?? "Unknown",
                        ItemCount = Convert.ToInt64(row["ItemCount"] ?? 0),
                        AvgPrice = Convert.ToDouble(row["AvgPrice"] ?? 0),
                        MinPrice = Convert.ToDouble(row["MinPrice"] ?? 0),
                        MaxPrice = Convert.ToDouble(row["MaxPrice"] ?? 0),
                        TotalSize = Convert.ToInt64(row["TotalSize"] ?? 0),
                        AvgSize = Convert.ToDouble(row["AvgSize"] ?? 0)
                    });
                }

                brandData.Value = brands;
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading brand data: {ex.Message}";
            }
            finally
            {
                isLoading.Value = false;
            }
        }, sortBy, sortOrder, limit);

        // Build UI with interactive controls - this will be the header
        var controlsHeader = Layout.Vertical().Align(Align.TopCenter)
            |(Layout.Vertical().Gap(3).Padding(3).Width(Size.Fraction(0.8f))
                | (Layout.Horizontal().Align(Align.TopCenter)
                        | sortBy.ToSelectInput(new[] { "ItemCount", "AvgPrice", "MinPrice", "MaxPrice", "TotalSize", "AvgSize", "Brand" }.ToOptions())
                            .WithField()
                            .Label("Sort by:")
                            .Width(Size.Full())
                        | sortOrder.ToSelectInput(new[] { "DESC", "ASC" }.ToOptions())
                            .WithField()
                            .Label("Sort order:")
                            .Width(Size.Full())
                        | new NumberInput<int>(limit).Min(1).Max(100)
                            .WithField()
                            .Label("Limit (number of brands):")
                            .Width(Size.Full())))
        ;

        var pageHeader = Layout.Vertical().Gap(3)
            | Layout.Horizontal().Gap(3).Width(Size.Full())
                | Layout.Vertical().Gap(1)
                    | Text.H3("Brand Analytics Dashboard")
                    | Text.Muted($"Top {limit.Value} Brands (sorted by {sortBy.Value} {sortOrder.Value})");

        if (errorMessage.Value != null)
        {
            return new HeaderLayout(
                header: controlsHeader,
                content: Layout.Vertical().Gap(4).Padding(4)
                    | pageHeader
                    | new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                            | Text.Small($"Error: {errorMessage.Value}")
                    )
            );
        }

        if (isLoading.Value || brandData.Value.Count == 0)
        {
            return new HeaderLayout(
                header: controlsHeader,
                content: Layout.Vertical().Gap(3).Padding(4).Align(Align.TopCenter)
                    | pageHeader.Width(Size.Fraction(0.8f))
                    | (Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(0.8f))
                        | new Skeleton().Height(Size.Units(60))
                        | new Skeleton().Height(Size.Units(60))
                        | new Skeleton().Height(Size.Units(60))
                        | new Skeleton().Height(Size.Units(60))
                        | new Skeleton().Height(Size.Units(80))
                        | new Skeleton().Height(Size.Units(80))
                        | new Skeleton().Height(Size.Units(80))
                        | new Skeleton().Height(Size.Units(80))
                        )
                    | (Layout.Vertical().Gap(3).Width(Size.Fraction(0.8f))
                        | new Skeleton().Height(Size.Units(80)))
                    | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.8f))
                        | new Skeleton().Height(Size.Units(80))
                        | new Skeleton().Height(Size.Units(80))
                        )
            );
        }

        var totalItems = brandData.Value.Sum(b => b.ItemCount);
        var avgItemsPerBrand = brandData.Value.Count > 0 ? totalItems / (double)brandData.Value.Count : 0;
        var avgPrice = brandData.Value.Count > 0 ? brandData.Value.Average(b => b.AvgPrice) : 0;
        var minPrice = brandData.Value.Count > 0 ? brandData.Value.Min(b => b.MinPrice) : 0;
        var maxPrice = brandData.Value.Count > 0 ? brandData.Value.Max(b => b.MaxPrice) : 0;
        var totalSize = brandData.Value.Sum(b => b.TotalSize);
        var avgSize = brandData.Value.Count > 0 ? brandData.Value.Average(b => b.AvgSize) : 0;
        // Total value = sum of (item count * average price) for each brand
        var totalValue = brandData.Value.Sum(b => (double)b.ItemCount * b.AvgPrice);

        // Calculate additional metrics
        var priceVariance = brandData.Value.Any()
            ? brandData.Value.Average(b => Math.Pow(b.AvgPrice - avgPrice, 2))
            : 0;
        var itemsPerSizeUnit = totalSize > 0 ? totalItems / (double)totalSize : 0;
        var medianPrice = brandData.Value.Any()
            ? brandData.Value.OrderBy(b => b.AvgPrice).Skip(brandData.Value.Count / 2).First().AvgPrice
            : 0;

        // Data is already sorted and filtered by SQL query, so use it directly
        var filteredBrands = brandData.Value;

        // Calculate trends (comparing loaded data with overall averages)
        // Helper function to calculate trend percentage
        double? CalculateTrend(double current, double baseline) => baseline != 0
            ? (current - baseline) / baseline
            : null;

        // Expected values based on overall averages
        var expectedItemsForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalItemsCount.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;
        var expectedValueForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalInventoryValue.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;

        // Calculate trends for each metric
        var totalItemsTrend = CalculateTrend(totalItems, expectedItemsForLoadedBrands);
        var avgItemsPerBrandTrend = CalculateTrend(avgItemsPerBrand, totalAvgItemsPerBrand.Value);
        var totalValueTrend = CalculateTrend(totalValue, expectedValueForLoadedBrands);
        var avgPriceTrend = CalculateTrend(avgPrice, totalAvgPrice.Value);
        var minPriceTrend = CalculateTrend(minPrice, totalMinPrice.Value);
        var maxPriceTrend = CalculateTrend(maxPrice, totalMaxPrice.Value);

        // Expected size values based on overall averages
        var expectedSizeForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalTotalSize.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;
        var totalSizeTrend = CalculateTrend(totalSize, expectedSizeForLoadedBrands);
        var avgSizeTrend = CalculateTrend(avgSize, totalAvgSize.Value);

        // Top Row - Key Metrics (10 cards with 2 new ones)
        var overallMetrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(totalItems.ToString("N0"))
            ).Title("Total Items").Icon(Icons.Database)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(minPrice.ToString("C2"))
            ).Title("Min Price").Icon(Icons.ArrowDown)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(maxPrice.ToString("C2"))
            ).Title("Max Price").Icon(Icons.ArrowUp)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(totalValue.ToString("C0"))
            ).Title("Total Inventory Value").Icon(Icons.DollarSign)
            | new MetricView("Avg Price", Icons.CreditCard,
                () => Task.FromResult(new MetricRecord(
                    avgPrice.ToString("C2"),
                    avgPriceTrend,
                    totalAvgPrice.Value > 0 ? avgPrice / (totalAvgPrice.Value + totalAvgPrice.Value/2) : null,
                    $"{avgPrice:C2} loaded"
                )))
            | new MetricView("Brands Analyzed", Icons.Tag,
                () => Task.FromResult(new MetricRecord(
                    brandData.Value.Count.ToString(),
                    null,
                    totalBrandsCount.Value > 0 ? (double)brandData.Value.Count / totalBrandsCount.Value : null, // Progress: loaded vs total
                    $"{brandData.Value.Count} loaded of {totalBrandsCount.Value} total"
                )))
            | new MetricView("Avg Items/Brand", Icons.ChartBar,
                () => Task.FromResult(new MetricRecord(
                    avgItemsPerBrand.ToString("N0"),
                    avgItemsPerBrandTrend,
                    totalAvgItemsPerBrand.Value > 0 ? (double)avgItemsPerBrand / totalAvgItemsPerBrand.Value : null,
                    $"{avgItemsPerBrand:N0} loaded"
                )))
            | new MetricView("Total Size", Icons.Box,
                () => Task.FromResult(new MetricRecord(
                    totalSize.ToString("N0"),
                    totalSizeTrend,
                    totalTotalSize.Value > 0 ? (double)totalSize / totalTotalSize.Value : null,
                    $"{totalSize:N0} loaded"
                )));

        // Brand Distribution Pie Chart
        var pieChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | Text.H3("Brand Distribution")
                | Text.Muted("Distribution of items by brand")
        ).Height(Size.Units(80));

        var pieChart = filteredBrands.ToPieChart(
            dimension: b => b.Brand,
            measure: b => b.Sum(f => f.ItemCount),
            PieChartStyles.Dashboard,
            new PieChartTotal(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", filteredBrands.Sum(b => b.ItemCount)), "Total Items"));

        // Brand Popularity Bar Chart (using LineChart as bar alternative)
        var barChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Count = (double)b.ItemCount })
            .ToList();

        var barChart = barChartData.ToLineChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Count", e => e.Sum(f => f.Count));

        var barChartCard = new Card(
            Layout.Horizontal().Gap(3).Padding(3)
                | pieChart
                | barChart
        ).Title("Brand Popularity");



        // Min Price by Brand Chart
        var minPriceChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Price = b.MinPrice })
            .ToList();

        var minPriceChart = minPriceChartData.ToBarChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));
        
        var minPriceChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | minPriceChart
        ).Title("Min Price by Brand");

        // Min Price by Brand Chart
        var maxPriceChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Price = b.MaxPrice })
            .ToList();
            
        var maxPriceChart = maxPriceChartData.ToBarChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));

        var maxPriceChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | maxPriceChart
        ).Title("Max Price by Brand");


        var content = Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | pageHeader.Width(Size.Fraction(0.8f))
            | overallMetrics.Width(Size.Fraction(0.8f))
            | barChartCard.Width(Size.Fraction(0.8f))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.8f))
                | minPriceChartCard
                | maxPriceChartCard);

        return new HeaderLayout(
            header: controlsHeader,
            content: content
        );
    }


    private class BrandStats
    {
        public string Brand { get; set; } = "";
        public long ItemCount { get; set; }
        public double AvgPrice { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public long TotalSize { get; set; }
        public double AvgSize { get; set; }
    }
}

