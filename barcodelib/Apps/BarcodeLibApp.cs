using BarcodeStandard;
using SkiaSharp;
using Type = BarcodeStandard.Type;

namespace BarcodeLibExample.Apps
{
    [App(icon: Icons.Barcode, title: "BarcodeLib Demo", path: ["Apps"])]
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
            var text = UseState("038000356216");
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

            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(6).Padding(3)
                    | Text.H2("Generate a barcode")
                    | Text.Muted("This demo uses the BarcodeLib NuGet package to generate barcodes.")
                    | text.ToInput(placeholder: "Enter the barcode value …")
                    | typeDropDown
                    | new Button(includeLabel.Value ? "Label: ON" : "Label: OFF")
                        .HandleClick(() => includeLabel.Value = !includeLabel.Value)
                    | Text.Muted("Barcode size is fixed at 300×120 pixels.")
                    // button to generate the preview; clicking this will generate and display the barcode
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
                    // show the preview image if available
                    | (!string.IsNullOrEmpty(previewUri.Value) ? new Image(previewUri.Value!).Width(150).Height(60) : "")
                    // disable the download button until a preview has been generated
                    | new Button("Download").Primary().Icon(Icons.Download)
                        .Disabled(string.IsNullOrEmpty(previewUri.Value))
                        .Url(downloadUrl.Value)
                  ).Width(Size.Units(120).Max(900));
        }
    }
}
