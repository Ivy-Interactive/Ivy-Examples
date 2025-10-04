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

    var controls = Layout.Horizontal().Gap(2)
      | typeDropDown
      | sizeDropDown
      | new Button("Download").Primary().Url(downloadUrl.Value).Icon(Icons.Download);

    return Layout.Center()
      | new Card(
        Layout.Vertical().Gap(6).Padding(3)
        | Text.H2("Try Now")
        | Text.Muted("Generate barcodes using Aspose.BarCode")
        | text.ToCodeInput().Language(Languages.Text).Width(Size.Full()).Height(Size.Units(25)).Placeholder("Enter your text here...")
        | controls
      ).Width(Size.Units(140).Max(900));
  }
}