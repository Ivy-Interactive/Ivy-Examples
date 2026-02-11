namespace SquirrelExample;

[App(icon: Icons.ChartBar, title: "Fashion Analytics")]
public class FashionAnalyticsApp : ViewBase
{
    public override object? Build()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "fashion_products.csv");

        if (!File.Exists(csvPath))
        {
            return new Card(
                Layout.Vertical()
                | Text.H3("Fashion Analytics")
                | Text.Muted("CSV file not found. Please make sure the data file is available next to the application binaries.")
                | Text.Muted(csvPath)
            );
        }

        var fashions = DataAcquisition.LoadCsv(csvPath);
        var typedProducts = RecordTable<FashionProduct>.FromTable(fashions).Rows.ToList();

        if (typedProducts.Count == 0)
        {
            return new Card(
                Layout.Vertical()
                | Text.H3("Fashion Analytics")
                | Text.Muted("The CSV file did not contain any rows.")
                | Text.Muted(csvPath)
            );
        }

        var brands = fashions.SplitOn("Brand");

        var brandStats = brands
            .Where(t => !string.IsNullOrWhiteSpace(t.Key))
            .Select(t =>
            {
                var prices = t.Value["Price"]
                    .Select(z => z?.Trim())
                    .Where(z => !string.IsNullOrEmpty(z))
                    .Select(z => Convert.ToDecimal(z.Trim()))
                    .ToList();

                return new BrandProductSummary(
                    Brand: t.Key,
                    Count: t.Value.RowCount,
                    HighestPrice: prices.Count > 0 ? prices.Max() : 0m,
                    LowestPrice: prices.Count > 0 ? prices.Min() : 0m
                    );
            })
            .OrderByDescending(summary => summary.Count)
            .ToList();

        var summaryStats = new List<SummaryMetric>
        {
            new SummaryMetric("Products", typedProducts.Count.ToString("N0")),
            new SummaryMetric("Brands", brandStats.Count.ToString("N0")),
            new SummaryMetric("Average Price", typedProducts.Average(p => p.Price).ToString("C2")),
            new SummaryMetric("Average Rating", typedProducts.Average(p => p.Rating).ToString("0.00"))
        };

        var summaryRow = summaryStats.Aggregate(
            Layout.Horizontal().Gap(2),
            (layout, metric) =>
                layout | new Card(
                    Layout.Vertical()
                    | Text.Muted(metric.Label)
                    | Text.H4(metric.Value)
                )
        );

        var summaryCard = new Card(
            Layout.Vertical()
            | Text.H2("Fashion Analytics")
            | Text.Muted($"Loaded {typedProducts.Count:N0} products from {Path.GetFileName(csvPath)}")
            | summaryRow
            | Text.Small("This dashboard recreates the analytical pipeline from the original console sample using Ivy UI components.")
        );

        var brandSummaryTable = brandStats
            .Select(stat => new
            {
                Brand = stat.Brand,
                Products = stat.Count,
                HighestPrice = stat.HighestPrice.ToString("C2"),
                LowestPrice = stat.LowestPrice.ToString("C2")
            })
            .ToTable()
            .Header(x => x.Brand, "Brand")
            .Header(x => x.Products, "Products")
            .Header(x => x.HighestPrice, "Highest Price")
            .Header(x => x.LowestPrice, "Lowest Price");

        var brandCard = new Card(
            Layout.Vertical().Gap(2)
            | Text.H3("Brand Performance")
            | Text.Muted("Split the Squirrel table on Brand, project count and price range.")
            | new Card(brandSummaryTable.Width(Size.Full()))
        );

        var dressesBySize = fashions
            .Filter("Product Name", "Dress")
            .SortInThisOrder("Size", new List<string> { "S", "M", "L", "XL" });

        var dressesBySizeTable = RecordTable<FashionProduct>.FromTable(dressesBySize)
            .Rows
            .Select(p => new
            {
                Product = p.ProductName,
                Size = p.Size,
                Brand = p.Brand,
                Price = p.Price.ToString("C2"),
                Rating = p.Rating.ToString("0.0")
            })
            .ToTable()
            .Header(x => x.Product, "Product")
            .Header(x => x.Size, "Size")
            .Header(x => x.Brand, "Brand")
            .Header(x => x.Price, "Price")
            .Header(x => x.Rating, "Rating");

        var adidasDressM = fashions
            .Filter("Brand", "Adidas")
            .Filter("Product Name", "Dress")
            .Filter("Size", "M");

        var adidasDressRows = RecordTable<FashionProduct>.FromTable(adidasDressM).Rows.ToList();

        var adidasDressTable = adidasDressRows
            .Select(p => new
            {
                Product = p.ProductName,
                Size = p.Size,
                Category = p.Category,
                Price = p.Price.ToString("C2"),
                Rating = p.Rating.ToString("0.0")
            })
            .ToTable()
            .Header(x => x.Product, "Product")
            .Header(x => x.Size, "Size")
            .Header(x => x.Category, "Category")
            .Header(x => x.Price, "Price")
            .Header(x => x.Rating, "Rating");

        var womensFashion = fashions
            .FilterByRegex("Category", "Women")
            .SortBy("Rating", how: Squirrel.SortDirection.Descending);

        var womensFashionRows = RecordTable<FashionProduct>.FromTable(womensFashion).Rows.ToList();

        var womensFashionTable = womensFashionRows
            .Select(p => new
            {
                Product = p.ProductName,
                Brand = p.Brand,
                Category = p.Category,
                Price = p.Price.ToString("C2"),
                Rating = p.Rating.ToString("0.0")
            })
            .ToTable()
            .Header(x => x.Product, "Product")
            .Header(x => x.Brand, "Brand")
            .Header(x => x.Category, "Category")
            .Header(x => x.Price, "Price")
            .Header(x => x.Rating, "Rating");

        static List<ValueForMoneyRow> ProjectValueForMoneyRows(object tableObject)
        {
            dynamic table = tableObject;
            var productNames = ((IEnumerable<string>)table["Product Name"]).ToList();
            var brands = ((IEnumerable<string>)table["Brand"]).ToList();
            var categories = ((IEnumerable<string>)table["Category"]).ToList();
            var prices = ((IEnumerable<string>)table["Price"]).ToList();
            var ratings = ((IEnumerable<string>)table["Rating"]).ToList();
            var values = ((IEnumerable<string>)table["ValueForMoney"]).ToList();

            var rowCount = (int)table.RowCount;

            return Enumerable.Range(0, rowCount)
                .Select(i =>
                {
                    var priceFormatted = decimal.TryParse(prices[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var priceDecimal)
                        ? priceDecimal.ToString("C2")
                        : prices[i];

                    var ratingFormatted = double.TryParse(ratings[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var ratingDouble)
                        ? ratingDouble.ToString("0.0")
                        : ratings[i];

                    var valueFormatted = decimal.TryParse(values[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var valueDecimal)
                        ? valueDecimal.ToString("0.00")
                        : values[i];

                    return new ValueForMoneyRow(
                        Product: productNames[i],
                        Brand: brands[i],
                        Category: categories[i],
                        Price: priceFormatted,
                        Rating: ratingFormatted,
                        ValueForMoney: valueFormatted);
                })
                .ToList();
        }

        fashions.AddColumn(columnName: "ValueForMoney", formula: "[Rating]/[Price]", decimalDigits: 2);

        var valueForMoney = fashions
            .SortBy("ValueForMoney", how: Squirrel.SortDirection.Descending)
            .Pick("Product Name", "Brand", "Category", "Price", "Rating", "ValueForMoney");

        var topValueForMoneyRows = ProjectValueForMoneyRows(valueForMoney.Top(15));
        var bottomValueForMoneyRows = ProjectValueForMoneyRows(valueForMoney.Bottom(15));

        var topValueForMoneyTable = topValueForMoneyRows
            .ToTable()
            .Header(x => x.Product, "Product")
            .Header(x => x.Brand, "Brand")
            .Header(x => x.Category, "Category")
            .Header(x => x.Price, "Price")
            .Header(x => x.Rating, "Rating")
            .Header(x => x.ValueForMoney, "Value For Money");

        var bottomValueForMoneyTable = bottomValueForMoneyRows
            .ToTable()
            .Header(x => x.Product, "Product")
            .Header(x => x.Brand, "Brand")
            .Header(x => x.Category, "Category")
            .Header(x => x.Price, "Price")
            .Header(x => x.Rating, "Rating")
            .Header(x => x.ValueForMoney, "Value For Money");

        var filtersCard = new Card(
            Layout.Vertical().Gap(2)
            | Text.H3("Filtered Insights")
            | Text.Muted("Recreate the console filtering samples with Ivy UI tables.")
            | new Expandable(
                "Dresses Sorted by Size",
                Layout.Vertical().Gap(1)
                | Text.Muted("Filter dresses only, then sort them using the custom size order S → XL.")
                | dressesBySizeTable.Width(Size.Full()))
            | new Expandable(
                "Adidas Dress (Size M)",
                Layout.Vertical().Gap(1)
                | Text.Muted("Chain filters to keep Adidas dresses in size M only.")
                | (adidasDressRows.Count == 0
                    ? Text.Muted("No records found for Adidas Dress size M.")
                    : adidasDressTable.Width(Size.Full())))
            | new Expandable(
                "Women's Fashion (Top Rated)",
                Layout.Vertical().Gap(1)
                | Text.Muted("Use a regex filter on category for Women and sort the results by rating descending.")
                | womensFashionTable.Width(Size.Full()))
        );

        var valueForMoneyCard = new Card(
            Layout.Vertical().Gap(2)
            | Text.H3("Value For Money")
            | Text.Muted("Add a calculated column and surface top and bottom performers based on rating-to-price ratio.")
            | new Expandable(
                "Top 15 Products",
                Layout.Vertical().Gap(1)
                | Text.Muted("Products with the best rating-to-price ratio, sorted descending.")
                | topValueForMoneyTable.Width(Size.Full()))
            | new Expandable(
                "Bottom 15 Products",
                Layout.Vertical().Gap(1)
                | Text.Muted("Products with the weakest rating-to-price ratio from the same calculation.")
                | bottomValueForMoneyTable.Width(Size.Full()))
        );

        return Layout.Vertical().Gap(3)
            | summaryCard
            | brandCard
            | filtersCard
            | valueForMoneyCard;
    }

    private record BrandProductSummary(string Brand, int Count, decimal HighestPrice, decimal LowestPrice);

    private record SummaryMetric(string Label, string Value);

    private record ValueForMoneyRow(string Product, string Brand, string Category, string Price, string Rating, string ValueForMoney);
}

