namespace SnowflakeDashboard;

[App(icon: Icons.ChartBar, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var refreshToken = this.UseRefreshToken();
        var snowflakeService = this.UseService<SnowflakeService>();
        
        var brandData = this.UseState<List<BrandStats>>(() => new List<BrandStats>());
        var totalItems = this.UseState<long>(() => 0);
        var avgPrice = this.UseState<double>(() => 0);
        var isLoading = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        this.UseEffect(async () =>
        {
            isLoading.Value = true;
            errorMessage.Value = null;
            
            try
            {
                // Overall statistics
                var totalSql = @"
                    SELECT 
                        COUNT(*) as TotalItems,
                        AVG(P_RETAILPRICE) as AvgPrice
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";
                
                var totalResult = await snowflakeService.ExecuteQueryAsync(totalSql);
                if (totalResult.Rows.Count > 0)
                {
                    var row = totalResult.Rows[0];
                    totalItems.Value = Convert.ToInt64(row["TotalItems"] ?? 0);
                    avgPrice.Value = Convert.ToDouble(row["AvgPrice"] ?? 0);
                }
                
                // Top 10 brands
                var brandsSql = @"
                    SELECT 
                        P_BRAND as Brand,
                        COUNT(*) as ItemCount,
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL
                    GROUP BY P_BRAND
                    ORDER BY ItemCount DESC
                    LIMIT 10";
                
                var brandsResult = await snowflakeService.ExecuteQueryAsync(brandsSql);
                var brands = new List<BrandStats>();
                
                foreach (System.Data.DataRow row in brandsResult.Rows)
                {
                    brands.Add(new BrandStats
                    {
                        Brand = row["Brand"]?.ToString() ?? "",
                        ItemCount = Convert.ToInt64(row["ItemCount"] ?? 0),
                        AvgPrice = Convert.ToDouble(row["AvgPrice"] ?? 0),
                        MinPrice = Convert.ToDouble(row["MinPrice"] ?? 0),
                        MaxPrice = Convert.ToDouble(row["MaxPrice"] ?? 0)
                    });
                }
                
                brandData.Value = brands;
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error: {ex.Message}";
            }
            finally
            {
                isLoading.Value = false;
            }
        }, []);
        
        if (errorMessage.Value != null)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Text.H3("Error")
                        | Text.Small(errorMessage.Value)
                ).Width(Size.Fraction(0.5f));
        }
        
        if (isLoading.Value || brandData.Value.Count == 0)
        {
            return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
                | Text.H2("Loading...")
                | (Layout.Grid().Columns(3).Gap(3)
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)));
        }
        
        // Key metrics
        var metrics = Layout.Grid().Columns(3).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(totalItems.Value.ToString("N0"))
            ).Title("Total Items").Icon(Icons.Database)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(avgPrice.Value.ToString("C2"))
            ).Title("Average Price").Icon(Icons.DollarSign)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(brandData.Value.Count.ToString())
            ).Title("Brands").Icon(Icons.Tag);
        
        // Brand distribution chart
        var pieChart = brandData.Value.ToPieChart(
            dimension: b => b.Brand,
            measure: b => b.Sum(f => f.ItemCount),
            PieChartStyles.Dashboard,
            new PieChartTotal(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", brandData.Value.Sum(b => b.ItemCount)), "Total"));
        
        // Average prices chart
        var priceChartData = brandData.Value
            .Select(b => new { Brand = b.Brand, Price = b.AvgPrice })
            .ToList();
        
        var priceChart = priceChartData.ToBarChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));
        
        // Top brands table
        var brandsTable = brandData.Value.AsQueryable()
            .ToDataTable()
            .Header(b => b.Brand, "Brand")
            .Header(b => b.ItemCount, "Count")
            .Header(b => b.AvgPrice, "Avg Price")
            .Header(b => b.MinPrice, "Min Price")
            .Header(b => b.MaxPrice, "Max Price")
            .Height(Size.Units(200));
        
        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | Text.H1("Snowflake Dashboard")
            | Text.Muted("Brand analytics from Snowflake Sample Data")
            | metrics.Width(Size.Fraction(0.9f))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.9f))
                | new Card(Layout.Vertical().Gap(3).Padding(3) | pieChart).Title("Brand Distribution")
                | new Card(Layout.Vertical().Gap(3).Padding(3) | priceChart).Title("Average Prices"))
            | new Card(
                Layout.Vertical().Gap(3).Padding(3)
                    | Text.H3("Top 10 Brands")
                    | brandsTable
            ).Width(Size.Fraction(0.9f));
    }
    
    private class BrandStats
    {
        public string Brand { get; set; } = "";
        public long ItemCount { get; set; }
        public double AvgPrice { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
    }
}

