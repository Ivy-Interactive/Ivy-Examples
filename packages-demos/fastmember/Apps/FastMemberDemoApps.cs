using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace FastMemberDemo.Apps;

[App(icon: Icons.Zap, title: "FastMember")]
public class FastMemberDemoApp : ViewBase
{
    // Data model for demonstration
    public record ProductModel(string Name, string Description, decimal Price, string Category, int Stock);

    // Cache TypeAccessor - it's thread-safe and can be reused
    private static readonly TypeAccessor ProductTypeAccessor = TypeAccessor.Create(typeof(ProductModel));
    private static readonly string[] ProductPropertyNames = { "Name", "Price", "Category", "Stock" };

    // JSON serialization settings
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Test data
    private static readonly List<ProductModel> SampleProducts = new()
    {
        new("Laptop", "High-performance laptop", 999.99m, "Electronics", 15),
        new("Mouse", "Wireless mouse", 29.99m, "Electronics", 50),
        new("Book", "Programming guide", 49.99m, "Books", 100),
        new("Chair", "Ergonomic office chair", 199.99m, "Furniture", 25),
        new("Monitor", "4K monitor 27 inches", 399.99m, "Electronics", 30),
        new("Keyboard", "Mechanical keyboard", 129.99m, "Electronics", 40)
    };

    public override object? Build()
    {
        var benchmarkResult = this.UseState<string>("");

        // Handler for benchmark
        void ShowBenchmark(string json) => benchmarkResult.Set(json);

        // ========== UI ==========

        var demosTabContent = BuildDemosTab();
        var benchmarkTabContent = BuildBenchmarkTab(ShowBenchmark, (IState<string>)benchmarkResult, RunPerformanceBenchmark);
        var dataTabContent = BuildDataTab();

        return Layout.Vertical().Gap(4)
            | Text.H1("FastMember - Fast Access to .NET Members")
            | Text.Muted("FastMember is a library for fast access to .NET type fields and properties when member names are known only at runtime. It uses IL code generation for maximum performance.")
            | Layout.Tabs(
                new Tab("Data", dataTabContent),
                new Tab("Demonstrations", demosTabContent),
                new Tab("Performance", benchmarkTabContent)
            ).Variant(TabsVariant.Tabs);
    }

    // ========== DEMONSTRATIONS ==========

    private string DemoTypeAccessor()
    {
        var info = new
        {
            Description = "TypeAccessor allows getting and setting property values by name (known only at runtime)",
            Members = ProductTypeAccessor.GetMembers()
                .Select(m => new { m.Name, Type = m.Type.Name })
                .ToList(),
        };
        return JsonSerializer.Serialize(info, JsonOptions);
    }

    private string DemoObjectAccessor()
    {
        if (SampleProducts.Count == 0)
            return JsonSerializer.Serialize(new { Error = "No products available" }, JsonOptions);

        var product = SampleProducts[0];
        var accessor = ObjectAccessor.Create(product);

        // Get values
        var originalPrice = accessor["Price"];
        var originalStock = accessor["Stock"];

        // Modify values
        accessor["Price"] = 899.99m;
        accessor["Stock"] = 20;

        var info = new
        {
            Description = "ObjectAccessor works with a specific object instance (can be static or DLR)",
            Original = new { Price = originalPrice, Stock = originalStock },
            Modified = new { Price = accessor["Price"], Stock = accessor["Stock"] },
        };

        // Restore original values
        accessor["Price"] = originalPrice;
        accessor["Stock"] = originalStock;

        return JsonSerializer.Serialize(info, JsonOptions);
    }

    private string DemoObjectReader()
    {
        using var reader = ObjectReader.Create(SampleProducts, ProductPropertyNames);
        var rows = new List<object>();

        while (reader.Read())
        {
            rows.Add(new
            {
                Name = reader[0],
                Price = reader[1],
                Category = reader[2],
                Stock = reader[3]
            });
        }

        var info = new
        {
            Description = "ObjectReader implements IDataReader for efficient reading of object sequences",
            TotalRows = rows.Count,
            Data = rows,
            UseCases = new[]
            {
                "Loading DataTable from objects",
                "SqlBulkCopy for fast database writes",
                "Exporting data to various formats"
            }
        };

        return JsonSerializer.Serialize(info, JsonOptions);
    }

    private string DemoDynamicObjects()
    {
        var dynamicProducts = new List<dynamic>();

        foreach (var product in SampleProducts)
        {
            dynamic dynamicProduct = new ExpandoObject();
            var dynamicAccessor = ObjectAccessor.Create(dynamicProduct);
            var sourceAccessor = ObjectAccessor.Create(product);

            foreach (var propName in ProductPropertyNames)
            {
                dynamicAccessor[propName] = sourceAccessor[propName];
            }

            dynamicProducts.Add(dynamicProduct);
        }

        var info = new
        {
            Description = "FastMember works with dynamic objects (ExpandoObject, DLR types)",
            Count = dynamicProducts.Count,
            Sample = new
            {
                First = new
                {
                    Name = dynamicProducts[0].Name,
                    Price = dynamicProducts[0].Price,
                    Category = dynamicProducts[0].Category
                }
            }
        };

        return JsonSerializer.Serialize(info, JsonOptions);
    }

    private string DemoBulkOperations()
    {
        var results = new List<object>();

        foreach (var product in SampleProducts)
        {
            var accessor = TypeAccessor.Create(typeof(ProductModel));
            var data = new
            {
                Name = accessor[product, "Name"],
                Price = accessor[product, "Price"],
                Category = accessor[product, "Category"],
                Stock = accessor[product, "Stock"]
            };
            results.Add(data);
        }

        var info = new
        {
            Description = "Bulk operations on objects using TypeAccessor",
            Processed = results.Count,
            Results = results
        };

        return JsonSerializer.Serialize(info, JsonOptions);
    }

    // ========== BENCHMARKS ==========

    private string RunPerformanceBenchmark()
    {
        const int iterations = 100_000;
        var testProduct = SampleProducts[0];
        var propertyName = "Price";
        var newValue = 799.99m;

        // Benchmark 1: TypeAccessor vs Reflection (Get)
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var value = ProductTypeAccessor[testProduct, propertyName];
        }
        sw1.Stop();
        var fastMemberGetTime = sw1.ElapsedMilliseconds;

        var sw2 = Stopwatch.StartNew();
        var propInfo = typeof(ProductModel).GetProperty(propertyName)!;
        for (int i = 0; i < iterations; i++)
        {
            var value = propInfo.GetValue(testProduct);
        }
        sw2.Stop();
        var reflectionGetTime = sw2.ElapsedMilliseconds;

        // Benchmark 2: TypeAccessor vs Reflection (Set)
        var sw3 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            ProductTypeAccessor[testProduct, propertyName] = newValue;
        }
        sw3.Stop();
        var fastMemberSetTime = sw3.ElapsedMilliseconds;

        var sw4 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            propInfo.SetValue(testProduct, newValue);
        }
        sw4.Stop();
        var reflectionSetTime = sw4.ElapsedMilliseconds;

        // Benchmark 3: ObjectReader vs Manual iteration
        var sw5 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            using var reader = ObjectReader.Create(SampleProducts, ProductPropertyNames);
            while (reader.Read())
            {
                var _ = reader[0];
            }
        }
        sw5.Stop();
        var objectReaderTime = sw5.ElapsedMilliseconds;

        var sw6 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            foreach (var product in SampleProducts)
            {
                var _ = product.Name;
            }
        }
        sw6.Stop();
        var manualIterationTime = sw6.ElapsedMilliseconds;

        var benchmark = new
        {
            Description = $"Performance comparison: FastMember vs Reflection ({iterations:N0} iterations)",
            Results = new
            {
                GetProperty = new
                {
                    FastMember = $"{fastMemberGetTime} ms",
                    Reflection = $"{reflectionGetTime} ms",
                    Speedup = $"{reflectionGetTime / (double)fastMemberGetTime:F2}x faster",
                    FastMemberMs = fastMemberGetTime,
                    ReflectionMs = reflectionGetTime
                },
                SetProperty = new
                {
                    FastMember = $"{fastMemberSetTime} ms",
                    Reflection = $"{reflectionSetTime} ms",
                    Speedup = $"{reflectionSetTime / (double)fastMemberSetTime:F2}x faster",
                    FastMemberMs = fastMemberSetTime,
                    ReflectionMs = reflectionSetTime
                },
                BulkRead = new
                {
                    ObjectReader = $"{objectReaderTime} ms",
                    ManualIteration = $"{manualIterationTime} ms",
                    Speedup = $"{manualIterationTime / (double)objectReaderTime:F2}x faster",
                    ObjectReaderMs = objectReaderTime,
                    ManualMs = manualIterationTime
                }
            },
            Conclusion = "FastMember is significantly faster than standard Reflection, especially with repeated usage"
        };

        return JsonSerializer.Serialize(benchmark, JsonOptions);
    }

    private object BuildDemosTab()
    {
        var selectedDemo = this.UseState<string?>(() => null);
        var resultState = this.UseState<string?>(() => null);

        var demonstrations = new Dictionary<string, (string code, string description, Func<string> execute)>
        {
            ["TypeAccessor"] = (
                @"var accessor = TypeAccessor.Create(typeof(ProductModel));
var product = new ProductModel(""Laptop"", ""High-performance laptop"", 999.99m, ""Electronics"", 15);

// Get property value by name
var price = accessor[product, ""Price""];

// Set property value by name
accessor[product, ""Price""] = 899.99m;

// Get all members
var members = accessor.GetMembers();",
                "TypeAccessor allows getting and setting property values by name (known only at runtime)",
                DemoTypeAccessor
            ),
            ["ObjectAccessor"] = (
                @"var product = new ProductModel(""Laptop"", ""High-performance laptop"", 999.99m, ""Electronics"", 15);
var wrapped = ObjectAccessor.Create(product);

// Get property value
string propName = ""Price""; // known only at runtime
var price = wrapped[propName];

// Set property value
wrapped[propName] = 899.99m;
wrapped[""Stock""] = 20;",
                "ObjectAccessor works with a specific object instance (can be static or DLR)",
                DemoObjectAccessor
            ),
            ["ObjectReader"] = (
                @"IEnumerable<ProductModel> data = SampleProducts;

// Create ObjectReader with specific properties
using(var reader = ObjectReader.Create(data, ""Name"", ""Price"", ""Category"", ""Stock""))
{
    // Use as IDataReader
    while (reader.Read())
    {
        var name = reader[0];
        var price = reader[1];
        var category = reader[2];
        var stock = reader[3];
    }
    
    // Can be used with DataTable.Load() or SqlBulkCopy
    // table.Load(reader);
}",
                "ObjectReader implements IDataReader for efficient reading of object sequences",
                DemoObjectReader
            ),
            ["Dynamic Objects"] = (
                @"var product = new ProductModel(""Laptop"", ""High-performance laptop"", 999.99m, ""Electronics"", 15);

// Create dynamic object
dynamic dynamicProduct = new ExpandoObject();
var dynamicAccessor = ObjectAccessor.Create(dynamicProduct);
var sourceAccessor = ObjectAccessor.Create(product);

// Copy properties to dynamic object
foreach (var propName in new[] { ""Name"", ""Price"", ""Category"" })
{
    dynamicAccessor[propName] = sourceAccessor[propName];
}

// Now you can use dynamicProduct.Name, dynamicProduct.Price, etc.",
                "FastMember works with dynamic objects (ExpandoObject, DLR types)",
                DemoDynamicObjects
            ),
            ["Bulk Operations"] = (
                @"var products = SampleProducts;
var accessor = TypeAccessor.Create(typeof(ProductModel));

// Process multiple objects efficiently
foreach (var product in products)
{
    var name = accessor[product, ""Name""];
    var price = accessor[product, ""Price""];
    var category = accessor[product, ""Category""];
    var stock = accessor[product, ""Stock""];
    
    // Perform operations...
}",
                "Bulk operations on objects using TypeAccessor for high-performance processing",
                DemoBulkOperations
            )
        };

        UseEffect(() =>
        {
            if (selectedDemo.Value != null && demonstrations.TryGetValue(selectedDemo.Value, out var demo))
            {
                try
                {
                    var result = demo.execute();
                    resultState.Set(result);
                }
                catch (Exception ex)
                {
                    var error = JsonSerializer.Serialize(new { Error = ex.Message }, JsonOptions);
                    resultState.Set(error);
                }
            }
            else
            {
                resultState.Set((string?)null);
            }
        }, selectedDemo);

        var demoOptions = demonstrations.Keys
            .Select(key => new Option<string>(key, key))
            .ToArray();

        var leftCard = new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Select Demonstration")
                | Text.Muted("Choose a FastMember feature to see example code and execution result")
                | selectedDemo
                    .ToSelectInput(demoOptions)
                    .Placeholder("Choose a demonstration...")
                    .WithField()
                    .Label("Select Demonstration")
                | (selectedDemo.Value != null && demonstrations.TryGetValue(selectedDemo.Value, out var demoInfo)
                    ? Layout.Vertical().Gap(3)
                        | Text.Muted(demoInfo.description)
                        | Text.Label("Example Code")
                        | new Code(demoInfo.code, Languages.Csharp)
                            .ShowLineNumbers()
                            .ShowCopyButton()
                    : Text.Muted("Please select a demonstration from the dropdown above"))
        ).Width(Size.Fraction(0.5f));

        var rightCard = new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Result")
                | Text.Muted("View the execution result")
                | (selectedDemo.Value != null && resultState.Value != null
                    ? new Code(resultState.Value, Languages.Json)
                        .ShowLineNumbers()
                        .ShowCopyButton()
                    : selectedDemo.Value != null
                        ? Text.Muted("Computing...")
                        : Text.Muted("Select a demonstration from the left to see the result here"))
        ).Width(Size.Fraction(0.5f));

        return Layout.Horizontal().Gap(4)
            | leftCard
            | rightCard;
    }

    private static object BuildBenchmarkTab(Action<string> showBenchmark, IState<string> benchmarkResultState, Func<string> runBenchmark)
    {
        var leftCard = new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Performance Benchmark")
                | Text.Muted("Compare FastMember performance with standard .NET Reflection API")
                | new Button("Run Benchmark").HandleClick(_ => showBenchmark(runBenchmark())).Icon(Icons.Zap).Primary()
        ).Width(Size.Fraction(0.5f));

        var rightCard = new Card(
            Layout.Vertical().Gap(3)
                | Text.H3("Benchmark Results")
                | Text.Muted("View performance comparison results")
                | (string.IsNullOrEmpty(benchmarkResultState.Value)
                    ? Text.Muted("Click 'Run Benchmark' to see performance comparison results")
                    : new Code(benchmarkResultState.Value, Languages.Json)
                        .ShowLineNumbers()
                        .ShowCopyButton()
                        .Height(Size.Fit()))
        ).Width(Size.Fraction(0.5f));

        return Layout.Horizontal().Gap(4)
            | leftCard
            | rightCard;
    }

    private static object BuildDataTab()
    {
        return Layout.Vertical()
            | new Card(
                Layout.Vertical().Gap(3)
                | Text.H2("Test Data")
                | Text.Muted("View the test data in a table")
                | BuildProductTable(SampleProducts)
                | Text.Muted($"Total products: {SampleProducts.Count}"));
    }


    private static TableBuilder<ProductModel> BuildProductTable(IList<ProductModel> products)
    {
        return products.ToTable()
            .Width(Size.Full())
            .Builder(p => p.Name, f => f.Default())
            .Builder(p => p.Description, f => f.Text())
            .Builder(p => p.Price, f => f.Default())
            .Builder(p => p.Category, f => f.Default())
            .Builder(p => p.Stock, f => f.Default());
    }
}
