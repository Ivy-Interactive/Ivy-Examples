namespace FastMemberExample;

[App(icon: Icons.Zap, title: "FastMember")]
public class FastMemberApp : ViewBase
{
    // Data model for demonstration
    public record ProductModel(string Name, string Description, decimal Price, string Category, int Stock);

    // Mutable class for benchmark Set operations
    public class MutableProduct
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public int Stock { get; set; }
    }

    private static readonly TypeAccessor ProductTypeAccessor = TypeAccessor.Create(typeof(ProductModel));
    private static readonly string[] ProductPropertyNames = { "Name", "Price", "Category", "Stock" };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly List<ProductModel> SampleProducts = new()
    {
        new("Laptop", "High-performance laptop", 999.99m, "Electronics", 15),
        new("Mouse", "Wireless mouse", 29.99m, "Electronics", 50),
        new("Book", "Programming guide", 49.99m, "Books", 100),
        new("Chair", "Ergonomic office chair", 199.99m, "Furniture", 25),
        new("Monitor", "4K monitor 27 inches", 399.99m, "Electronics", 30),
        new("Keyboard", "Mechanical keyboard", 129.99m, "Electronics", 40)
    };

    private static readonly Dictionary<string, (string code, string description, Func<string> execute)> Demonstrations = new()
    {
        ["TypeAccessor"] = (
            @"var accessor = TypeAccessor.Create(typeof(ProductModel));
var product = new ProductModel(""Laptop"", ""High-performance laptop"", 999.99m, ""Electronics"", 15);
var price = accessor[product, ""Price""];
accessor[product, ""Price""] = 899.99m;
var members = accessor.GetMembers();",
            "TypeAccessor allows getting and setting property values by name (known only at runtime)",
            DemoTypeAccessor
        ),
        ["ObjectAccessor"] = (
            @"var product = new ProductModel(""Laptop"", ""High-performance laptop"", 999.99m, ""Electronics"", 15);
var wrapped = ObjectAccessor.Create(product);
string propName = ""Price"";
var price = wrapped[propName];
wrapped[propName] = 899.99m;",
            "ObjectAccessor works with a specific object instance (can be static or DLR)",
            DemoObjectAccessor
        ),
        ["ObjectReader"] = (
            @"using(var reader = ObjectReader.Create(SampleProducts, ""Name"", ""Price"", ""Category"", ""Stock""))
{
    while (reader.Read())
    {
        var name = reader[0];
        var price = reader[1];
    }
}",
            "ObjectReader implements IDataReader for efficient reading of object sequences",
            DemoObjectReader
        ),
        ["Dynamic Objects"] = (
            @"dynamic dynamicProduct = new ExpandoObject();
var dynamicAccessor = ObjectAccessor.Create(dynamicProduct);
var sourceAccessor = ObjectAccessor.Create(product);
foreach (var propName in new[] { ""Name"", ""Price"", ""Category"" })
    dynamicAccessor[propName] = sourceAccessor[propName];",
            "FastMember works with dynamic objects (ExpandoObject, DLR types)",
            DemoDynamicObjects
        ),
        ["Bulk Operations"] = (
            @"var accessor = TypeAccessor.Create(typeof(ProductModel));
foreach (var product in products)
{
    var name = accessor[product, ""Name""];
    var price = accessor[product, ""Price""];
}",
            "Bulk operations on objects using TypeAccessor for high-performance processing",
            DemoBulkOperations
        )
    };

    public override object? Build()
    {
        var benchmarkResult = this.UseState<BenchmarkResults?>(() => null);
        var selectedDemo = this.UseState<string?>(() => null);
        var resultState = this.UseState<string?>(() => null);

        UseEffect(() =>
        {
            if (selectedDemo.Value != null && Demonstrations.TryGetValue(selectedDemo.Value, out var demo))
            {
                try
                {
                    resultState.Set(demo.execute());
                }
                catch (Exception ex)
                {
                    resultState.Set(JsonSerializer.Serialize(new { Error = ex.Message }, JsonOptions));
                }
            }
            else
            {
                resultState.Set((string?)null);
            }
        }, selectedDemo);

        return Layout.Vertical().Gap(4).Padding(4)
            | Text.H1("FastMember")
            | Text.Muted("FastMember is a library for fast access to .NET type fields and properties when member names are known only at runtime. It uses IL code generation for maximum performance.")
            | Layout.Tabs(
                new Tab("Data", BuildDataTab()),
                new Tab("Demonstrations", BuildDemosTab(selectedDemo, resultState)),
                new Tab("Performance", BuildBenchmarkTab(results => benchmarkResult.Set(results), benchmarkResult, RunPerformanceBenchmark))
            ).Variant(TabsVariant.Tabs)
            | Text.Small("This demo uses FastMember library for accessing properties of objects at runtime.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [FastMember](https://github.com/mgravell/fast-member)")
            ;
    }

    // ========== DEMONSTRATIONS ==========

    private static string DemoTypeAccessor() => JsonSerializer.Serialize(new
    {
        Description = "TypeAccessor allows getting and setting property values by name (known only at runtime)",
        Members = ProductTypeAccessor.GetMembers().Select(m => new { m.Name, Type = m.Type.Name }).ToList()
    }, JsonOptions);

    private static string DemoObjectAccessor()
    {
        var product = SampleProducts[0];
        var accessor = ObjectAccessor.Create(product);
        var originalPrice = accessor["Price"];
        var originalStock = accessor["Stock"];

        accessor["Price"] = 899.99m;
        accessor["Stock"] = 20;

        var result = JsonSerializer.Serialize(new
        {
            Description = "ObjectAccessor works with a specific object instance (can be static or DLR)",
            Original = new { Price = originalPrice, Stock = originalStock },
            Modified = new { Price = accessor["Price"], Stock = accessor["Stock"] }
        }, JsonOptions);

        accessor["Price"] = originalPrice;
        accessor["Stock"] = originalStock;
        return result;
    }

    private static string DemoObjectReader()
    {
        using var reader = ObjectReader.Create(SampleProducts, ProductPropertyNames);
        var rows = new List<object>();
        while (reader.Read())
            rows.Add(new { Name = reader[0], Price = reader[1], Category = reader[2], Stock = reader[3] });

        return JsonSerializer.Serialize(new
        {
            Description = "ObjectReader implements IDataReader for efficient reading of object sequences",
            TotalRows = rows.Count,
            Data = rows,
            UseCases = new[] { "Loading DataTable from objects", "SqlBulkCopy for fast database writes", "Exporting data to various formats" }
        }, JsonOptions);
    }

    private static string DemoDynamicObjects()
    {
        var dynamicProducts = SampleProducts.Select(product =>
        {
            dynamic dynamicProduct = new ExpandoObject();
            var dynamicAccessor = ObjectAccessor.Create(dynamicProduct);
            var sourceAccessor = ObjectAccessor.Create(product);
            foreach (var propName in ProductPropertyNames)
                dynamicAccessor[propName] = sourceAccessor[propName];
            return dynamicProduct;
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            Description = "FastMember works with dynamic objects (ExpandoObject, DLR types)",
            Count = dynamicProducts.Count,
            Sample = new { First = new { Name = dynamicProducts[0].Name, Price = dynamicProducts[0].Price, Category = dynamicProducts[0].Category } }
        }, JsonOptions);
    }

    private static string DemoBulkOperations() => JsonSerializer.Serialize(new
    {
        Description = "Bulk operations on objects using TypeAccessor",
        Processed = SampleProducts.Count,
        Results = SampleProducts.Select(p => new
        {
            Name = ProductTypeAccessor[p, "Name"],
            Price = ProductTypeAccessor[p, "Price"],
            Category = ProductTypeAccessor[p, "Category"],
            Stock = ProductTypeAccessor[p, "Stock"]
        }).ToList()
    }, JsonOptions);

    // ========== BENCHMARKS ==========

    private record BenchmarkResults(
        int Iterations,
        GetPropertyResult GetProperty,
        SetPropertyResult SetProperty
    );

    private record GetPropertyResult(
        string FastMemberTypeAccessor,
        string FastMemberObjectAccessor,
        string DynamicCSharp,
        string ReflectionPropertyInfo,
        string PropertyDescriptor,
        string FastMemberVsReflection,
        string FastMemberVsPropertyDescriptor
    );

    private record SetPropertyResult(
        string FastMemberTypeAccessor,
        string FastMemberObjectAccessor,
        string DynamicCSharp,
        string ReflectionPropertyInfo,
        string PropertyDescriptor,
        string FastMemberVsReflection,
        string FastMemberVsPropertyDescriptor
    );

    private static long MeasureTime(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static BenchmarkResults? RunPerformanceBenchmark()
    {
        const int iterations = 100_000;
        const string propertyName = "Price";
        var newValue = 799.99m;
        var testProduct = SampleProducts[0];
        var mutableProduct = new MutableProduct { Name = testProduct.Name, Description = testProduct.Description, Price = testProduct.Price, Category = testProduct.Category, Stock = testProduct.Stock };

        // GET benchmarks
        var fastMemberTypeAccessorGetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) _ = ProductTypeAccessor[testProduct, propertyName]; });
        var objectAccessor = ObjectAccessor.Create(testProduct);
        var fastMemberObjectAccessorGetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) _ = objectAccessor[propertyName]; });
        dynamic dynamicProduct = testProduct;
        var dynamicGetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) _ = dynamicProduct.Price; });
        var propInfo = typeof(ProductModel).GetProperty(propertyName)!;
        var reflectionGetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) _ = propInfo.GetValue(testProduct); });
        var propDescriptor = TypeDescriptor.GetProperties(testProduct)[propertyName]!;
        var propertyDescriptorGetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) _ = propDescriptor.GetValue(testProduct); });

        // SET benchmarks
        var mutableTypeAccessor = TypeAccessor.Create(typeof(MutableProduct));
        var fastMemberTypeAccessorSetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) mutableTypeAccessor[mutableProduct, propertyName] = newValue; });
        var mutableObjectAccessor = ObjectAccessor.Create(mutableProduct);
        var fastMemberObjectAccessorSetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) mutableObjectAccessor[propertyName] = newValue; });
        dynamic dynamicMutableProduct = mutableProduct;
        var dynamicSetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) dynamicMutableProduct.Price = newValue; });
        var mutablePropInfo = typeof(MutableProduct).GetProperty(propertyName)!;
        var reflectionSetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) mutablePropInfo.SetValue(mutableProduct, newValue); });
        var mutablePropDescriptor = TypeDescriptor.GetProperties(mutableProduct)[propertyName]!;
        var propertyDescriptorSetTime = MeasureTime(() => { for (int i = 0; i < iterations; i++) mutablePropDescriptor.SetValue(mutableProduct, newValue); });

        return new BenchmarkResults(iterations,
            new GetPropertyResult($"{fastMemberTypeAccessorGetTime} ms", $"{fastMemberObjectAccessorGetTime} ms", $"{dynamicGetTime} ms",
                $"{reflectionGetTime} ms", $"{propertyDescriptorGetTime} ms",
                $"{reflectionGetTime / (double)fastMemberTypeAccessorGetTime:F2}x faster",
                $"{propertyDescriptorGetTime / (double)fastMemberTypeAccessorGetTime:F2}x faster"),
            new SetPropertyResult($"{fastMemberTypeAccessorSetTime} ms", $"{fastMemberObjectAccessorSetTime} ms", $"{dynamicSetTime} ms",
                $"{reflectionSetTime} ms", $"{propertyDescriptorSetTime} ms",
                $"{reflectionSetTime / (double)fastMemberTypeAccessorSetTime:F2}x faster",
                $"{propertyDescriptorSetTime / (double)fastMemberTypeAccessorSetTime:F2}x faster"));
    }

    private static object BuildDemosTab(IState<string?> selectedDemo, IState<string?> resultState)
    {
        var demoOptions = Demonstrations.Keys.Select(key => new Option<string>(key, key)).ToArray();
        var selectionCard = new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Select Demonstration")
                | Text.Muted("Choose a FastMember feature to see example code and execution result")
                | selectedDemo.ToSelectInput(demoOptions).Placeholder("Choose a demonstration...").WithField().Label("Select Demonstration")
                | (selectedDemo.Value != null && Demonstrations.TryGetValue(selectedDemo.Value, out var demoInfo)
                    ? Text.Muted(demoInfo.description)
                    : Text.Muted("Please select a demonstration from the dropdown above"))
        );

        if (selectedDemo.Value == null || !Demonstrations.TryGetValue(selectedDemo.Value, out var selectedDemoData))
            return selectionCard;

        return Layout.Vertical().Gap(4)
            | selectionCard
            | (Layout.Horizontal().Gap(4)
                | new Card(Layout.Vertical().Gap(3)
                    | Text.H3("Example Code")
                    | Text.Muted("View the example code for the selected demonstration")
                    | new Code(selectedDemoData.code, Languages.Csharp).ShowLineNumbers().ShowCopyButton()).Width(Size.Fraction(0.5f))
                | new Card(Layout.Vertical().Gap(3)
                    | Text.H3("Result")
                    | Text.Muted("View the execution result")
                    | (resultState.Value != null
                        ? new Code(resultState.Value, Languages.Json).ShowLineNumbers().ShowCopyButton()
                        : Text.Muted("Computing..."))).Width(Size.Fraction(0.5f)));
    }

    private static object BuildBenchmarkTab(Action<BenchmarkResults?> showBenchmark, IState<BenchmarkResults?> benchmarkResultState, Func<BenchmarkResults?> runBenchmark)
    {
        var buttonCard = new Card(Layout.Vertical().Gap(3)
            | Text.H3("Performance Benchmark")
            | Text.Muted("Compare FastMember performance with standard .NET Reflection API, Dynamic C#, and PropertyDescriptor")
            | new Button(benchmarkResultState.Value == null ? "Run Benchmark" : "Run Benchmark Again")
                .HandleClick(_ => showBenchmark(runBenchmark())).Icon(Icons.Zap).Primary());

        if (benchmarkResultState.Value == null)
            return buttonCard;

        var results = benchmarkResultState.Value;
        return Layout.Vertical().Gap(4)
            | buttonCard
            | (Layout.Horizontal().Gap(4)
                | new Card(Layout.Vertical().Gap(3)
                    | Text.H3("Get Property")
                    | Text.Muted("Performance comparison for reading property values")
                    | results.GetProperty.ToDetails()).Width(Size.Fraction(0.5f))
                | new Card(Layout.Vertical().Gap(3)
                    | Text.H3("Set Property")
                    | Text.Muted("Performance comparison for setting property values")
                    | results.SetProperty.ToDetails()).Width(Size.Fraction(0.5f)));
    }

    private static object BuildDataTab() => new Card(Layout.Vertical().Gap(3)
        | Text.H2("Test Data")
        | Text.Muted("Sample product data used in demonstrations and benchmarks. This data represents the ProductModel objects that FastMember operations are performed on.")
        | SampleProducts.ToTable().Width(Size.Full())
            .Builder(p => p.Name, f => f.Default())
            .Builder(p => p.Description, f => f.Text())
            .Builder(p => p.Price, f => f.Default())
            .Builder(p => p.Category, f => f.Default())
            .Builder(p => p.Stock, f => f.Default())
        | Text.Muted($"Total products: {SampleProducts.Count}"));
}
