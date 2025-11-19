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
        var result = this.UseState<string>("");
        var benchmarkResult = this.UseState<string>("");

        // Handlers for demonstrations
        void ShowResult(string json) => result.Set(json);
        void ShowBenchmark(string json) => benchmarkResult.Set(json);

        // ========== DEMONSTRATIONS ==========

        string DemoTypeAccessor()
        {
            var info = new
            {
                Description = "TypeAccessor allows getting and setting property values by name (known only at runtime)",
                Members = ProductTypeAccessor.GetMembers()
                    .Select(m => new { m.Name, Type = m.Type.Name, IsReadable = m.CanRead, IsWritable = m.CanWrite })
                    .ToList(),
                Example = new
                {
                    Code = @"var accessor = TypeAccessor.Create(typeof(ProductModel));
var product = new ProductModel(...);
accessor[product, ""Price""] = 899.99m;
var price = accessor[product, ""Price""];"
                }
            };
            return JsonSerializer.Serialize(info, JsonOptions);
        }

        string DemoObjectAccessor()
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
                Example = new
                {
                    Code = @"var wrapped = ObjectAccessor.Create(product);
string propName = ""Price""; // known only at runtime
wrapped[propName] = 899.99m;
Console.WriteLine(wrapped[propName]);"
                }
            };

            // Restore original values
            accessor["Price"] = originalPrice;
            accessor["Stock"] = originalStock;

            return JsonSerializer.Serialize(info, JsonOptions);
        }

        string DemoObjectReader()
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
                },
                Example = new
                {
                    Code = @"IEnumerable<ProductModel> data = ...;
using(var reader = ObjectReader.Create(data, ""Id"", ""Name"", ""Price""))
{
    table.Load(reader);
}"
                }
            };

            return JsonSerializer.Serialize(info, JsonOptions);
        }

        string DemoDynamicObjects()
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

        string DemoBulkOperations()
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

        string RunPerformanceBenchmark()
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

        // ========== UI ==========

        var demosTabContent = BuildDemosTab(ShowResult, DemoTypeAccessor, DemoObjectAccessor, DemoObjectReader, DemoDynamicObjects, DemoBulkOperations);
        var benchmarkTabContent = BuildBenchmarkTab(ShowBenchmark, RunPerformanceBenchmark);
        var dataTabContent = BuildDataTab();

        return Layout.Vertical().Gap(4)
            | Text.H1("FastMember - Fast Access to .NET Members")
            | Text.Muted("FastMember is a library for fast access to .NET type fields and properties when member names are known only at runtime. It uses IL code generation for maximum performance.")
            | Layout.Tabs(
                new Tab("Demonstrations", demosTabContent),
                new Tab("Performance", benchmarkTabContent),
                new Tab("Data", dataTabContent)
            ).Variant(TabsVariant.Tabs)
            | (!string.IsNullOrEmpty(result.Value) ? BuildResultSection("Result", result.Value) : null)
            | (!string.IsNullOrEmpty(benchmarkResult.Value) ? BuildResultSection("Benchmark Results", benchmarkResult.Value) : null);
    }

    private static object BuildDemosTab(Action<string> showResult, 
        Func<string> demoTypeAccessor, 
        Func<string> demoObjectAccessor, 
        Func<string> demoObjectReader, 
        Func<string> demoDynamicObjects, 
        Func<string> demoBulkOperations)
    {
        return Layout.Vertical().Gap(3)
            | Text.H2("Feature Demonstrations")
            | new WrapLayout([
                new Button("TypeAccessor").HandleClick(_ => showResult(demoTypeAccessor())).Icon(Icons.Code),
                new Button("ObjectAccessor").HandleClick(_ => showResult(demoObjectAccessor())).Icon(Icons.Pencil),
                new Button("ObjectReader").HandleClick(_ => showResult(demoObjectReader())).Icon(Icons.Database),
                new Button("Dynamic Objects").HandleClick(_ => showResult(demoDynamicObjects())).Icon(Icons.Sparkles),
                new Button("Bulk Operations").HandleClick(_ => showResult(demoBulkOperations())).Icon(Icons.Layers)
            ])
            | Text.Small("Click a button to see usage example and result");
    }

    private static object BuildBenchmarkTab(Action<string> showBenchmark, Func<string> runBenchmark)
    {
        return Layout.Vertical().Gap(3)
            | Text.H2("Performance Comparison")
            | Text.Muted("This benchmark compares FastMember speed with standard .NET Reflection API")
            | new Button("Run Benchmark").HandleClick(_ => showBenchmark(runBenchmark())).Icon(Icons.Zap).Primary()
            | Text.Small("Warning: Benchmark may take a few seconds. Results depend on hardware.");
    }

    private static object BuildDataTab()
    {
        return Layout.Vertical().Gap(3)
            | Text.H2("Test Data")
            | BuildProductTable(SampleProducts)
            | Text.Small($"Total products: {SampleProducts.Count}");
    }

    private static object BuildResultSection(string title, string json)
    {
        return Layout.Vertical().Gap(2)
            | Text.H2(title)
            | new Code(json, Languages.Json)
                .ShowLineNumbers()
                .ShowCopyButton()
                .Height(Size.Units(400));
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
