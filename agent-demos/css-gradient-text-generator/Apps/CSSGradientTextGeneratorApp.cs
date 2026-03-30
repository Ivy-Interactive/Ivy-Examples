using Ivy;

namespace CSSGradientTextGenerator.Apps;

[App(icon: Icons.Paintbrush)]
public class CSSGradientTextGeneratorApp : ViewBase
{
    public override object? Build()
    {
        var text = UseState("Gradient Text");
        var colors = UseState(new[] { "#ff0080", "#7928ca" });
        var direction = UseState("to right");
        var fontSize = UseState(48);
        var fontWeight = UseState("700");
        var client = UseService<IClientProvider>();

        var colorStops = string.Join(", ", colors.Value);
        var gradientCss = $"linear-gradient({direction.Value}, {colorStops})";

        var cssOutput = $@".gradient-text {{
  background: {gradientCss};
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  font-size: {fontSize.Value}px;
  font-weight: {fontWeight.Value};
}}";

        var previewHtml = $@"<div style=""
  background: {gradientCss};
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  font-size: {fontSize.Value}px;
  font-weight: {fontWeight.Value};
  line-height: 1.2;
  word-break: break-word;
"">{System.Net.WebUtility.HtmlEncode(text.Value)}</div>";

        var directionOptions = new[]
        {
            "to right", "to left", "to bottom", "to top",
            "to bottom right", "to top right",
            "135deg", "45deg", "90deg", "180deg"
        }.ToOptions();

        var weightOptions = new[]
        {
            new Option<string>("400", "400 (Normal)"),
            new Option<string>("700", "700 (Bold)"),
            new Option<string>("800", "800 (Extra Bold)")
        };

        var colorInputs = Layout.Vertical();
        for (var i = 0; i < colors.Value.Length; i++)
        {
            var index = i;
            colorInputs |= new ColorStopView(colors, index);
        }

        var leftPanel = Layout.Vertical().Width(Size.Full())
            | Text.H3("Controls")
            | text.ToTextareaInput().WithField().Label("Text")
            | new Separator()
            | Text.H3("Gradient Colors")
            | colorInputs
            | new Button("Add Color").Icon(Icons.Plus).OnClick(() =>
            {
                var arr = colors.Value.ToList();
                arr.Add("#000000");
                colors.Set(arr.ToArray());
            })
            | new Separator()
            | direction.ToSelectInput(directionOptions).WithField().Label("Gradient Direction")
            | fontSize.ToNumberInput().Min(12).Max(200).WithField().Label("Font Size (px)")
            | fontWeight.ToSelectInput(weightOptions).WithField().Label("Font Weight");

        var rightPanel = Layout.Vertical().Width(Size.Full())
            | Text.H3("Preview")
            | new Html(previewHtml).DangerouslyAllowScripts()
            | new Separator()
            | Text.H3("Generated CSS")
            | new CodeBlock(cssOutput, Languages.Css).ShowCopyButton().WrapLines()
            | new Button("Copy CSS").Icon(Icons.Copy).Primary().OnClick(() =>
            {
                client.CopyToClipboard(cssOutput);
                client.Toast("CSS copied to clipboard!");
            });

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | Text.H1("CSS Gradient Text Generator")
                | (Layout.Horizontal().Gap(6)
                    | leftPanel
                    | rightPanel));
    }
}

public class ColorStopView : ViewBase
{
    private readonly IState<string[]> _colors;
    private readonly int _index;

    public ColorStopView(IState<string[]> colors, int index)
    {
        _colors = colors;
        _index = index;
    }

    public override object? Build()
    {
        var colorState = UseState(_colors.Value[_index]);

        UseEffect(() =>
        {
            if (_index < _colors.Value.Length && _colors.Value[_index] != colorState.Value)
            {
                var arr = _colors.Value.ToArray();
                arr[_index] = colorState.Value;
                _colors.Set(arr);
            }
        }, colorState);

        UseEffect(() =>
        {
            if (_index < _colors.Value.Length && _colors.Value[_index] != colorState.Value)
            {
                colorState.Set(_colors.Value[_index]);
            }
        }, _colors);

        var removeBtn = _colors.Value.Length > 2
            ? (object)new Button().Icon(Icons.Minus).Small().OnClick(() =>
            {
                var arr = _colors.Value.ToList();
                if (arr.Count > 2 && _index < arr.Count)
                {
                    arr.RemoveAt(_index);
                    _colors.Set(arr.ToArray());
                }
            })
            : null;

        return Layout.Horizontal().Align(Align.Left)
            | colorState.ToColorInput().WithField().Label($"Color {_index + 1}")
            | removeBtn;
    }
}
