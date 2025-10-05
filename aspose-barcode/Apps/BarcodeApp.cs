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

  public override object? Build()
  {
    var text = UseState("");
    var encodeType = UseState(EncodeTypes.QR);
    var size = UseState(DemoSize.Medium);
    var previewUri = UseState("");

    var downloadUrl = this.UseDownload(() =>
    {
      if (string.IsNullOrWhiteSpace(text.Value)) return Array.Empty<byte>();

      var xDimension = size.Value switch
      {
        DemoSize.Small => 3f,
        DemoSize.Medium => 10f,
        DemoSize.Large => 30f,
        _ => 10f
      };

      using var generator = new BarcodeGenerator(encodeType.Value, text.Value);
      generator.Parameters.Barcode.XDimension.Pixels = xDimension;

      using var ms = new MemoryStream();
      generator.Save(ms, BarCodeImageFormat.Png);
      return ms.ToArray();
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

          var xDimension = size.Value switch
          {
            DemoSize.Small => 3f,
            DemoSize.Medium => 10f,
            DemoSize.Large => 30f,
            _ => 10f
          };

          using var generator = new BarcodeGenerator(encodeType.Value, text.Value);
          generator.Parameters.Barcode.XDimension.Pixels = xDimension;

          using var ms = new MemoryStream();
          generator.Save(ms, BarCodeImageFormat.Png);
          var base64 = Convert.ToBase64String(ms.ToArray());
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
    ).Width(Size.Fraction(0.45f)).Height(130);

    var previewPixels = size.Value switch
    {
      DemoSize.Small => 50,
      DemoSize.Medium => 65,
      DemoSize.Large => 80,
      _ => 60
    };

    var rightCardBody = Layout.Vertical().Gap(4)
      | Text.H2("Barcode")
      | Text.Muted("Preview")
      | (Layout.Center()
      | (!string.IsNullOrEmpty(previewUri.Value)
          ? new Image(previewUri.Value!).Width(Size.Units(previewPixels)).Height(Size.Units(previewPixels))
          : Text.Muted("No preview")));

    var rightCard = new Card(rightCardBody).Width(Size.Fraction(0.35f)).Height(130);

    return Layout.Horizontal().Gap(6).Align(Align.Center)
          | leftCard
          | rightCard;
  }
}