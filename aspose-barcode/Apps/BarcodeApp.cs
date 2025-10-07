namespace AsposeBarCodeExample.Apps;

[App(icon: Icons.QrCode, title: "Aspose BarCode", path: ["Apps"])]
public class BarcodeApp : ViewBase
{
  private enum DemoSize
  {
    Small,
    Medium,
    Large
  }

  private static float GetXDimension(DemoSize size)
  {
    return size switch
    {
      DemoSize.Small => 5f,
      DemoSize.Medium => 10f,
      DemoSize.Large => 15f,
      _ => 10f
    };
  }

  private static byte[] GeneratePngBytes(string text, SymbologyEncodeType encodeType, float xDimension)
  {
    using var generator = new BarcodeGenerator(encodeType, text);
    generator.Parameters.Barcode.XDimension.Pixels = xDimension;

    using var ms = new MemoryStream();
    generator.Save(ms, BarCodeImageFormat.Png);
    return ms.ToArray();
  }

  public override object? Build()
  {
    var text = UseState("");
    var encodeType = UseState(EncodeTypes.QR);
    var size = UseState(DemoSize.Medium);
    var previewUri = UseState("");

    var downloadUrl = this.UseDownload(() =>
    {
      if (string.IsNullOrWhiteSpace(text.Value)) return Array.Empty<byte>();
      var xDimension = GetXDimension(size.Value);
      return GeneratePngBytes(text.Value, encodeType.Value, xDimension);
    }, "image/png", "barcode.png");

    var typeDropDown = new Button(encodeType.Value.ToString()).Primary()
      .Icon(Icons.ChevronDown)
      .WithDropDown(
        MenuItem.Default("QR").HandleSelect(() => encodeType.Value = EncodeTypes.QR),
        MenuItem.Default("Pdf417").HandleSelect(() => encodeType.Value = EncodeTypes.Pdf417),
        MenuItem.Default("Code128").HandleSelect(() => encodeType.Value = EncodeTypes.Code128),
        MenuItem.Default("DataMatrix").HandleSelect(() => encodeType.Value = EncodeTypes.DataMatrix),
        MenuItem.Default("DotCode").HandleSelect(() => encodeType.Value = EncodeTypes.DotCode),
        MenuItem.Default("ISBN").HandleSelect(() => encodeType.Value = EncodeTypes.ISBN)
      );

    var sizeDropDown = new Button(size.Value.ToString()).Primary()
      .Icon(Icons.ChevronDown)
      .WithDropDown(
        MenuItem.Default("Small").HandleSelect(() => size.Value = DemoSize.Small),
        MenuItem.Default("Medium").HandleSelect(() => size.Value = DemoSize.Medium),
        MenuItem.Default("Large").HandleSelect(() => size.Value = DemoSize.Large)
      );

    var controls = Layout.Horizontal().Gap(2).Align(Align.Center)
      | typeDropDown
      | sizeDropDown
      | new Button("Preview").Primary().Icon(Icons.Eye)
        .HandleClick(() =>
        {
          if (string.IsNullOrWhiteSpace(text.Value))
          {
            previewUri.Value = "";
            return;
          }

          var xDimension = GetXDimension(size.Value);
          var bytes = GeneratePngBytes(text.Value, encodeType.Value, xDimension);
          var base64 = Convert.ToBase64String(bytes);
          previewUri.Value = $"data:image/png;base64,{base64}";
        })
      | new Button("Download").Primary().Url(downloadUrl.Value).Icon(Icons.Download)
        .Disabled(string.IsNullOrEmpty(previewUri.Value));

    var leftCard = new Card(
      Layout.Vertical().Gap(6).Padding(3)
      | Text.H2("Input")
      | Text.Muted("Enter text and barcode options")
      | text.ToCodeInput().Language(Languages.Text).Width(Size.Full()).Height(Size.Units(25)).Placeholder("Enter text...")
      | controls
      | Text.Small("This demo uses Aspose.BarCode for .NET to generate barcodes.")
      | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Aspose.BarCode for .NET](https://products.aspose.com/barcode/net/)")
    ).Width(Size.Fraction(0.45f)).Height(130);

    var rightCardBody = Layout.Vertical().Gap(4)
      | Text.H2("Barcode")
      | Text.Muted("Preview")
      | (Layout.Center()
      | (previewUri.Value is string uri && !string.IsNullOrEmpty(uri)
          ? new Image(uri) // Use intrinsic size to avoid scaling blur
          : Text.Muted("No preview")));

    var rightCard = new Card(rightCardBody).Width(Size.Fraction(0.45f)).Height(130);

    return Layout.Horizontal().Gap(6).Align(Align.Center)
          | leftCard
          | rightCard;
  }
}