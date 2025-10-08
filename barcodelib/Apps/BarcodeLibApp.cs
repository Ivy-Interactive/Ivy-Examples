using BarcodeStandard;
using SkiaSharp;
using Type = BarcodeStandard.Type;

namespace BarcodeLibExample.Apps
{
    [App(icon: Icons.Barcode, title: "BarcodeLib", path: ["Apps"])]
    public sealed class BarcodeLibApp : ViewBase
    {
        private static readonly (string Label, Type Type)[] Symbologies =
        {
            ("UPC-A", Type.UpcA),
            ("EAN-13", Type.Ean13),
            ("Code128", Type.Code128),
            ("Code39", Type.Code39),
            ("Interleaved 2 of 5", Type.Interleaved2Of5),
            ("ITF-14", Type.Itf14)
        };

        public override object? Build()
        {
            var text = UseState("123456789012");
            var typeIndex = UseState(0);
            var includeLabel = UseState(true);
            // holds the generated preview data URI. null means no preview yet
            var previewUri = UseState("");

            // fixed barcode size
            const int width = 300;
            const int height = 120;

            var downloadUrl = this.UseDownload(() =>
            {
                if (string.IsNullOrWhiteSpace(text.Value))
                    return Array.Empty<byte>();

                var (_, type) = Symbologies[typeIndex.Value];
                var b = new Barcode { IncludeLabel = includeLabel.Value };
                using var bitmap = b.Encode(type, text.Value, SKColors.Black, SKColors.White, width, height);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }, "image/png", "barcode.png");

            var typeItems = Symbologies
                .Select((item, idx) => MenuItem.Default(item.Label).HandleSelect(() => typeIndex.Value = idx))
                .ToArray();

            var typeDropDown = new Button(Symbologies[typeIndex.Value].Label)
                .Primary()
                .Icon(Icons.ChevronDown)
                .WithDropDown(typeItems);

            var controls = Layout.Horizontal().Gap(2).Align(Align.Center)
                | typeDropDown
                | new Button(includeLabel.Value ? "Label: ON" : "Label: OFF")
                    .Primary()
                    .HandleClick(() => includeLabel.Value = !includeLabel.Value)
                | new Button("Preview").Primary().Icon(Icons.Eye)
                    .HandleClick(() =>
                    {
                        if (string.IsNullOrWhiteSpace(text.Value))
                        {
                            previewUri.Value = "";
                            return;
                        }
                        var (_, type) = Symbologies[typeIndex.Value];
                        var b = new Barcode { IncludeLabel = includeLabel.Value };
                        using var bitmap = b.Encode(type, text.Value, SKColors.Black, SKColors.White, width, height);
                        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        var base64 = Convert.ToBase64String(data.ToArray());
                        previewUri.Value = $"data:image/png;base64,{base64}";
                    })
                | new Button("Download").Primary().Icon(Icons.Download)
                    .Disabled(string.IsNullOrEmpty(previewUri.Value))
                    .Url(downloadUrl.Value ?? "");

            var leftCard = new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | Text.H2("Input")
                | Text.Muted("Enter barcode value and options")
                | text.ToInput(placeholder: "Enter the barcode value …")
                | controls
                | Text.Small("This demo uses the BarcodeLib NuGet package to generate barcodes.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [BarcodeLib](https://github.com/barnhill/barcodelib)")
            ).Width(Size.Fraction(0.45f)).Height(110);

            var rightCard = new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | Text.H2("Barcode")
                | Text.Muted("Preview")
                | Layout.Horizontal(
                 (previewUri.Value is string uri && !string.IsNullOrEmpty(uri)
                    ? new Image(uri) // Use intrinsic size to avoid scaling blur
                    : Text.Muted("No preview"))).Align(Align.Center)
            ).Width(Size.Fraction(0.45f)).Height(110);

            return Layout.Horizontal().Gap(6).Align(Align.Center)
                | leftCard
                | rightCard;
        }
    }
}
