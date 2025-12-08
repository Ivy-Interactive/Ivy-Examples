namespace SnowflakeDashboard;

[App(icon: Icons.ChartBar, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {   // Limits configuration
        const int LIMIT = 7;
        const float CONTENT_WIDTH = 0.8f;
        
        var refreshToken = this.UseRefreshToken();
        var configuration = this.UseService<IConfiguration>();
        
        // Track verification status
        var isVerified = this.UseState(() => VerifiedCredentials.IsVerified);
        
        // Subscribe to verification status changes
        this.UseEffect(() =>
        {
            void OnVerificationStatusChanged()
            {
                isVerified.Value = VerifiedCredentials.IsVerified;
                refreshToken.Refresh();
            }
            
            VerifiedCredentials.VerificationStatusChanged += OnVerificationStatusChanged;
            isVerified.Value = VerifiedCredentials.IsVerified;
        }, [EffectTrigger.AfterInit()]);
        
        // Load credentials from configuration on mount
        this.UseEffect(() =>
        {
            VerifiedCredentials.LoadFromConfiguration(configuration);
        }, [EffectTrigger.AfterInit()]);
        
        // Credentials form state (must be declared before conditional return)
        var isDialogOpen = this.UseState(false);
        var credentialsForm = this.UseState(() => new SnowflakeCredentialsRequest());
        var verificationStatus = this.UseState<string?>(() => null);
        var isVerifying = this.UseState(false);
        
        // Show credentials card if not verified
        if (!isVerified.Value)
        {
            return BuildCredentialsCard(refreshToken, configuration, isDialogOpen, credentialsForm, verificationStatus, isVerifying);
        }
        
        var snowflakeService = this.UseService<SnowflakeService>();
        
        var brandData = this.UseState<List<BrandStats>>(() => new List<BrandStats>());
        var totalItems = this.UseState<long>(() => 0);
        var avgPrice = this.UseState<double>(() => 0);
        var minPrice = this.UseState<double>(() => 0);
        var maxPrice = this.UseState<double>(() => 0);
        var popularBrandSizes = this.UseState<List<SizeStats>>(() => new List<SizeStats>());
        var containerDistribution = this.UseState<List<ContainerStats>>(() => new List<ContainerStats>());
        var popularBrandContainers = this.UseState<List<ContainerStats>>(() => new List<ContainerStats>());
        var isLoading = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        
        this.UseEffect(async () =>
        {
            isLoading.Value = true;
            errorMessage.Value = null;
            
            try
            {
                // Top brands
                var brandsSql = $@"
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
                    LIMIT {LIMIT}";
                
                var brands = await LoadBrandsAsync(snowflakeService, LIMIT);
                brandData.Value = brands;
                
                if (brands.Count > 0)
                {
                    CalculateBrandStatistics(brands, totalItems, avgPrice, minPrice, maxPrice);
                    await LoadPopularBrandDataAsync(snowflakeService, brands[0].Brand, popularBrandSizes, popularBrandContainers);
                }
                
                containerDistribution.Value = await LoadContainersAsync(snowflakeService, LIMIT);
                
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
                | Text.H1("Snowflake Dashboard")
                | Text.Muted($"Brand analytics from Snowflake Sample Data - Top {LIMIT}")
                | (Layout.Grid().Columns(5).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                    | new Skeleton().Height(Size.Units(60))
                    | new Skeleton().Height(Size.Units(60))
                    | new Skeleton().Height(Size.Units(60))
                    | new Skeleton().Height(Size.Units(60))
                    | new Skeleton().Height(Size.Units(60)))
                | (Layout.Grid().Columns(3).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                    | new Skeleton().Height(Size.Units(80))
                    | new Skeleton().Height(Size.Units(80)))
                | new Skeleton().Height(Size.Units(120)).Width(Size.Fraction(CONTENT_WIDTH));
        }
        
        // Key metrics
        var metrics = Layout.Grid().Columns(5).Gap(3)
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
                    | Text.H3(minPrice.Value.ToString("C2"))
            ).Title("Min Price").Icon(Icons.ArrowDown)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Text.H3(maxPrice.Value.ToString("C2"))
            ).Title("Max Price").Icon(Icons.ArrowUp)
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
        
        // Popular brand sizes chart
        var sizesChartData = popularBrandSizes.Value
            .Select(s => new { Size = s.Size.ToString(), Count = (double)s.Count })
            .ToList();
        
        var sizesChart = sizesChartData.ToBarChart()
            .Dimension("Size", e => e.Size)
            .Measure("Count", e => e.Sum(f => f.Count));
        
        // Container distribution chart
        var containerChart = containerDistribution.Value.ToPieChart(
            dimension: c => c.Container,
            measure: c => c.Sum(f => f.Count),
            PieChartStyles.Dashboard,
            new PieChartTotal(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", containerDistribution.Value.Sum(c => c.Count)), "Total"));
        
        // Popular brand containers chart
        var brandContainersChartData = popularBrandContainers.Value
            .Select(c => new { Container = c.Container, Count = (double)c.Count })
            .ToList();
        
        var brandContainersChart = brandContainersChartData.ToBarChart()
            .Dimension("Container", e => e.Container)
            .Measure("Count", e => e.Sum(f => f.Count));
        
        // Min price line chart
        var minPriceChartData = brandData.Value
            .Select(b => new { Brand = b.Brand, Price = b.MinPrice })
            .ToList();
        
        var minPriceChart = minPriceChartData.ToLineChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));
        
        // Max price line chart
        var maxPriceChartData = brandData.Value
            .Select(b => new { Brand = b.Brand, Price = b.MaxPrice })
            .ToList();
        
        var maxPriceChart = maxPriceChartData.ToLineChart()
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
            .Height(Size.Units(80));

        var mostPopularBrand = brandData.Value.Count > 0 ? brandData.Value[0].Brand : "N/A";
        
        // Clear credentials button
        var clearCredentialsButton = new Button("Log Out")
            .Icon(Icons.LogOut)
            .Variant(ButtonVariant.Secondary)
            .HandleClick(_ =>
            {
                VerifiedCredentials.Clear();
                refreshToken.Refresh();
            });
 
        return Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | Text.H1("Snowflake Dashboard")
            | Text.Muted($"Brand analytics from Snowflake Sample Data - Top {LIMIT}")
            |  new FloatingPanel(clearCredentialsButton, Align.TopRight).Offset(new Thickness(0, 5, 5, 0))
            | metrics.Width(Size.Fraction(CONTENT_WIDTH))
            | (Layout.Grid().Columns(3).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                | new Card(Layout.Vertical().Gap(3).Padding(3) | minPriceChart).Title("Min Price by Brand")
                | new Card(Layout.Vertical().Gap(3).Padding(3) | pieChart).Title($"Top {LIMIT} Brands Distribution")
                | new Card(Layout.Vertical().Gap(3).Padding(3) | maxPriceChart).Title("Max Price by Brand"))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                | new Card(Layout.Vertical().Gap(3).Padding(3) | priceChart).Title("Average Prices")
                | new Card(Layout.Vertical().Gap(3).Padding(3) | containerChart).Title($"Top {LIMIT} Container Distribution"))

            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(CONTENT_WIDTH))
                | new Card(Layout.Vertical().Gap(3).Padding(3) | sizesChart).Title($"Sizes - Most Popular Brand ({mostPopularBrand})")
                | new Card(Layout.Vertical().Gap(3).Padding(3) | brandContainersChart).Title($"Containers - Most Popular Brand ({mostPopularBrand})"))

            | new Card(Layout.Vertical().Gap(3).Padding(3) | brandsTable)
                .Title($"Top {LIMIT} Brands")
                .Width(Size.Fraction(CONTENT_WIDTH));
    }
    
    private object BuildCredentialsCard(dynamic refreshToken, IConfiguration configuration, IState<bool> isDialogOpen, IState<SnowflakeCredentialsRequest> credentialsForm, IState<string?> verificationStatus, IState<bool> isVerifying)
    {
        // Handle credential verification when form is submitted
        this.UseEffect(async () =>
        {
            var credentials = credentialsForm.Value;
            if (!string.IsNullOrWhiteSpace(credentials.Account) && !isDialogOpen.Value && !isVerifying.Value && verificationStatus.Value == null)
            {
                isVerifying.Value = true;

                try
                {
                    var connectionString = $"account={credentials.Account};user={credentials.User};password={credentials.Password};";
                    var snowflakeService = new SnowflakeService(connectionString);

                    var isValid = await snowflakeService.TestConnectionAsync();

                    if (isValid)
                    {
                        VerifiedCredentials.SetCredentials(credentials.Account, credentials.User, credentials.Password);
                        verificationStatus.Value = "success";
                        isVerifying.Value = false;
                        refreshToken.Refresh();
                    }
                    else
                    {
                        VerifiedCredentials.Clear();
                        verificationStatus.Value = "error";
                        isVerifying.Value = false;
                    }
                }
                catch
                {
                    VerifiedCredentials.Clear();
                    verificationStatus.Value = "error";
                    isVerifying.Value = false;
                }
            }
        }, [credentialsForm, isDialogOpen]);
        
        var showSuccessMessage = VerifiedCredentials.IsVerified && verificationStatus.Value == null;
        
        return Layout.Center()
            | new Card(
                Layout.Vertical().Gap(4).Padding(4)
                | Text.H3("Snowflake Connection Required")
                | Text.Muted("Please enter your Snowflake credentials to access the dashboard.")
                | (Layout.Vertical().Gap(3).Padding(3)
                    | Text.Markdown("**1. Sign up or log in** to [Snowflake](https://www.snowflake.com/)")
                    | Text.Markdown("**2. Navigate to your Account** settings")
                    | Text.Markdown("**3. Copy your Account Identifier** (e.g., `xy12345.us-east-1`)")
                    | Text.Markdown("**4. Note your Username and Password**")
                    | Text.Markdown("**5. Enter your credentials below**"))
                | (VerifiedCredentials.IsVerified
                    ? new Button("Clear Credentials")
                        .Icon(Icons.LogOut)
                        .Variant(ButtonVariant.Secondary)
                        .HandleClick(_ =>
                        {
                            VerifiedCredentials.Clear();
                            verificationStatus.Value = null;
                            refreshToken.Refresh();
                        })
                    : new Button("Enter Credentials")
                        .Icon(Icons.Key)
                        .Variant(ButtonVariant.Primary)
                        .Disabled(isVerifying.Value)
                        .HandleClick(_ =>
                        {
                            isDialogOpen.Value = true;
                            verificationStatus.Value = null;
                        }))
                | new Spacer()
                | (showSuccessMessage
                    ? new Callout("Connection verified successfully! Dashboard is now available.", "Success", CalloutVariant.Success)
                    : verificationStatus.Value == "success"
                    ? new Callout("Connection successful! Dashboard is now available.", "Success", CalloutVariant.Success)
                    : verificationStatus.Value == "error"
                    ? new Callout("Connection failed. Please check your credentials and try again.", "Error", CalloutVariant.Error)
                    : new Callout("Never publish credentials in public repositories or share them with unauthorized parties.", "Important Notice", CalloutVariant.Warning, Icons.TriangleAlert))
            ).Width(Size.Fraction(0.3f))
            | (isDialogOpen.Value
                ? credentialsForm.ToForm()
                    .Builder(e => e.Account, e => e.ToTextInput())
                    .Label(e => e.Account, "Account Identifier")
                    .Builder(e => e.User, e => e.ToTextInput())
                    .Label(e => e.User, "Username")
                    .Builder(e => e.Password, e => e.ToPasswordInput())
                    .Label(e => e.Password, "Password")
                    .ToDialog(isDialogOpen,
                        title: "Enter Snowflake Credentials",
                        submitTitle: isVerifying.Value ? "Verifying..." : "Save",
                        width: Size.Fraction(0.3f)
                    )
                : null);
    }
    
    private class BrandStats
    {
        public string Brand { get; set; } = "";
        public long ItemCount { get; set; }
        public double AvgPrice { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
    }
    
    private class SizeStats
    {
        public long Size { get; set; }
        public long Count { get; set; }
    }
    
    private class ContainerStats
    {
        public string Container { get; set; } = "";
        public long Count { get; set; }
    }
    
    private record SnowflakeCredentialsRequest
    {
        [Required]
        public string Account { get; init; } = "";

        [Required]
        public string User { get; init; } = "";

        [Required]
        public string Password { get; init; } = "";
    }
    
    // Helper methods
    private async Task<List<BrandStats>> LoadBrandsAsync(SnowflakeService service, int limit)
    {
        var sql = $@"
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
            LIMIT {limit}";
        
        var result = await service.ExecuteQueryAsync(sql);
        return result.Rows.Cast<System.Data.DataRow>()
            .Select(row => new BrandStats
            {
                Brand = row["Brand"]?.ToString() ?? "",
                ItemCount = Convert.ToInt64(row["ItemCount"] ?? 0),
                AvgPrice = Convert.ToDouble(row["AvgPrice"] ?? 0),
                MinPrice = Convert.ToDouble(row["MinPrice"] ?? 0),
                MaxPrice = Convert.ToDouble(row["MaxPrice"] ?? 0)
            })
            .ToList();
    }
    
    private void CalculateBrandStatistics(List<BrandStats> brands, IState<long> totalItems, IState<double> avgPrice, IState<double> minPrice, IState<double> maxPrice)
    {
        if (brands.Count == 0) return;
        
        totalItems.Value = brands.Sum(b => b.ItemCount);
        avgPrice.Value = brands.Average(b => b.AvgPrice);
        minPrice.Value = brands.Min(b => b.MinPrice);
        maxPrice.Value = brands.Max(b => b.MaxPrice);
    }
    
    private async Task LoadPopularBrandDataAsync(SnowflakeService service, string brand, IState<List<SizeStats>> popularBrandSizes, IState<List<ContainerStats>> popularBrandContainers)
    {
        var escapedBrand = brand.Replace("'", "''");
        
        // Load sizes
        var sizesSql = $@"
            SELECT P_SIZE as Size, COUNT(*) as Count
            FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
            WHERE P_BRAND = '{escapedBrand}' AND P_SIZE IS NOT NULL
            GROUP BY P_SIZE
            ORDER BY Count DESC";
        
        var sizesResult = await service.ExecuteQueryAsync(sizesSql);
        popularBrandSizes.Value = sizesResult.Rows.Cast<System.Data.DataRow>()
            .Select(row => new SizeStats
            {
                Size = Convert.ToInt64(row["Size"] ?? 0),
                Count = Convert.ToInt64(row["Count"] ?? 0)
            })
            .ToList();
        
        // Load containers
        var containersSql = $@"
            SELECT P_CONTAINER as Container, COUNT(*) as Count
            FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
            WHERE P_BRAND = '{escapedBrand}' AND P_CONTAINER IS NOT NULL
            GROUP BY P_CONTAINER
            ORDER BY Count DESC";
        
        var containersResult = await service.ExecuteQueryAsync(containersSql);
        popularBrandContainers.Value = containersResult.Rows.Cast<System.Data.DataRow>()
            .Select(row => new ContainerStats
            {
                Container = row["Container"]?.ToString() ?? "",
                Count = Convert.ToInt64(row["Count"] ?? 0)
            })
            .ToList();
    }
    
    private async Task<List<ContainerStats>> LoadContainersAsync(SnowflakeService service, int limit)
    {
        var sql = $@"
            SELECT P_CONTAINER as Container, COUNT(*) as Count
            FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
            WHERE P_CONTAINER IS NOT NULL
            GROUP BY P_CONTAINER
            ORDER BY Count DESC
            LIMIT {limit}";
        
        var result = await service.ExecuteQueryAsync(sql);
        return result.Rows.Cast<System.Data.DataRow>()
            .Select(row => new ContainerStats
            {
                Container = row["Container"]?.ToString() ?? "",
                Count = Convert.ToInt64(row["Count"] ?? 0)
            })
            .ToList();
    }
}

