
[App(icon: Icons.Box, title: "CsvHelper")]
public class CsvHelperApp : ViewBase
{
    public record ProductModel(string Name, string Description, decimal Price, string Category);

    public override object? Build()
    {
        // Seed initial products
        var products = UseState(() => new ProductModel[]
        {
            new("Wireless Mouse", "Ergonomic 2.4 GHz mouse with silent click", 19.99m, "Accessories"),
            new("Mechanical Keyboard", "RGB backlit mechanical keyboard with blue switches", 79.50m, "Accessories"),
            new("27\" 4K Monitor", "Ultra-HD IPS display with HDR support", 299.99m, "Displays"),
            new("USB-C Hub", "7-in-1 hub with HDMI, USB 3.0 and card reader", 34.95m, "Peripherals"),
            new("Noise-Cancelling Headphones", "Over-ear Bluetooth headphones with ANC", 149.00m, "Audio"),
        });

        // Create a download URL that builds a CSV from the current products using CsvHelper
        var downloadUrl = this.UseDownload(
            async () =>
            {
                await using var ms = new MemoryStream();
                await using var writer = new StreamWriter(ms, leaveOpen: true);
                await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

                // write records to the in-memory stream
                await csv.WriteRecordsAsync(products.Value);
                await writer.FlushAsync();
                ms.Position = 0;

                return ms.ToArray(); // Ivy needs a byte[]
            },
            "text/csv",
            $"products-{DateTime.UtcNow:yyyy-MM-dd}.csv"
        );

        var downloadBtn = new Button("Download CSV")
            .Primary()
            .Url(downloadUrl.Value);

        // Build the UI
        return Layout.Vertical()
            | Text.H2("CSV Helper Demo")
            | products.Value.ToTable()
                .Width(Size.Full())
                .Builder(p => p.Name, f => f.Default())
                .Builder(p => p.Description, f => f.Text())
                .Builder(p => p.Price, f => f.Default())
                .Builder(p => p.Category, f => f.Default())
            | downloadBtn;
    }
}
