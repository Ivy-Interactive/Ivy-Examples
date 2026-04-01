using Ivy;

namespace GradientGenerator.Apps;

public record ColorStop(string Color, int Position);

[App(icon: Icons.Palette)]
public class GradientGeneratorApp : ViewBase
{
    private enum GradientType { Linear, Radial, Conic }

    public override object? Build()
    {
        var gradientType = UseState(GradientType.Linear);
        var angle = UseState(135);
        var stops = UseState(() => new List<ColorStop>
        {
            new("#ff6b6b", 0),
            new("#4ecdc4", 100)
        });
        var client = UseService<IClientProvider>();

        var cssCode = BuildCssCode(gradientType.Value, angle.Value, stops.Value);

        var typeSelect = gradientType
            .ToSelectInput(new[] { GradientType.Linear, GradientType.Radial, GradientType.Conic }.ToOptions())
            .WithField().Label("Gradient Type");

        var showAngle = gradientType.Value is GradientType.Linear or GradientType.Conic;

        var stopsLayout = Layout.Vertical().Gap(2);
        for (var i = 0; i < stops.Value.Count; i++)
        {
            var index = i;
            stopsLayout |= new ColorStopRow(
                stop: stops.Value[index],
                index: index,
                canRemove: stops.Value.Count > 2,
                onUpdate: updated =>
                {
                    var list = new List<ColorStop>(stops.Value);
                    list[index] = updated;
                    stops.Set(list);
                },
                onRemove: () =>
                {
                    var list = new List<ColorStop>(stops.Value);
                    list.RemoveAt(index);
                    stops.Set(list);
                }
            );
        }

        var controlsPanel = Layout.Vertical()
            | Text.H2("Controls")
            | typeSelect
            | (showAngle
                ? angle.ToNumberInput(min: 0, max: 360).WithField().Label("Angle (degrees)")
                : (object)new Fragment())
            | new Separator()
            | Text.H2("Color Stops")
            | stopsLayout
            | new Button("Add Stop", () =>
            {
                var list = new List<ColorStop>(stops.Value)
                {
                    new("#ffffff", 50)
                };
                stops.Set(list);
            }).Small();

        var previewHtml = $"""
            <div style="width:100%;height:300px;border-radius:12px;background:{cssCode};border:1px solid #e2e8f0;"></div>
            """;

        var previewPanel = Layout.Vertical()
            | Text.H2("Preview")
            | new Html(previewHtml).DangerouslyAllowScripts()
            | new Separator()
            | Text.H2("CSS Code")
            | new CodeBlock($"background: {cssCode};", Languages.Css)
                .ShowCopyButton()
                .WrapLines()
                .Width(Size.Full())
            | new Button("Copy CSS", () =>
            {
                client.CopyToClipboard($"background: {cssCode};");
                client.Toast("CSS copied to clipboard!");
            }).Small();

        return Layout.Vertical().Height(Size.Full())
            | (Layout.Vertical().Padding(6)
                | Text.H1("Gradient Generator").NoWrap()
                | Text.Lead("Build beautiful CSS gradients visually")
                | (Layout.Horizontal().Gap(6).AlignContent(Align.TopLeft)
                    | (Layout.Vertical().Width(Size.Units(80)) | controlsPanel)
                    | (Layout.Vertical().Width(Size.Full()) | previewPanel)));
    }

    private static string BuildCssCode(GradientType type, int angle, List<ColorStop> stops)
    {
        var sortedStops = stops.OrderBy(s => s.Position).ToList();
        var stopsStr = string.Join(", ", sortedStops.Select(s => $"{s.Color} {s.Position}%"));

        return type switch
        {
            GradientType.Linear => $"linear-gradient({angle}deg, {stopsStr})",
            GradientType.Radial => $"radial-gradient(circle, {stopsStr})",
            GradientType.Conic => $"conic-gradient(from {angle}deg, {stopsStr})",
            _ => $"linear-gradient({angle}deg, {stopsStr})"
        };
    }
}

public class ColorStopRow : ViewBase
{
    private readonly ColorStop _stop;
    private readonly int _index;
    private readonly bool _canRemove;
    private readonly Action<ColorStop> _onUpdate;
    private readonly Action _onRemove;

    public ColorStopRow(ColorStop stop, int index, bool canRemove, Action<ColorStop> onUpdate, Action onRemove)
    {
        _stop = stop;
        _index = index;
        _canRemove = canRemove;
        _onUpdate = onUpdate;
        _onRemove = onRemove;
    }

    public override object? Build()
    {
        var color = UseState(_stop.Color);
        var position = UseState(_stop.Position);

        UseEffect(() =>
        {
            _onUpdate(new ColorStop(color.Value, position.Value));
        }, color, position);

        return Layout.Horizontal().AlignContent(Align.BottomLeft).Gap(2)
            | color.ToColorInput().Variant(ColorInputVariant.TextAndPicker)
                .WithField().Label($"Color {_index + 1}")
            | position.ToNumberInput(min: 0, max: 100)
                .WithField().Label("Position %")
            | (_canRemove
                ? Icons.Trash2.ToButton().Destructive().Ghost().Small().OnClick(_onRemove)
                : (object)new Fragment());
    }
}
