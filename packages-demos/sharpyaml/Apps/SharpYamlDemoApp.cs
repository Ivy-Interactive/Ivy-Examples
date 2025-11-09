namespace Sharpyaml.Apps;

[App(icon: Icons.ScrollText, title: "SharpYaml Demo")]
public class SharpYamlDemoApp : ViewBase
{
    private const string DefaultModel = "{\n" +
        "  \"Metadata\": {\n" +
        "    \"Created\": \"2025-11-09T10:30:00Z\",\n" +
        "    \"Tags\": [\"demo\", \"yaml\", \"sample\"]\n" +
        "  },\n" +
        "  \"Customer\": {\n" +
        "    \"Name\": \"Ada Lovelace\",\n" +
        "    \"Email\": \"ada@example.com\",\n" +
        "    \"Subscribed\": true\n" +
        "  },\n" +
        "  \"Orders\": [\n" +
        "    {\n" +
        "      \"Id\": 101,\n" +
        "      \"Items\": [\n" +
        "        { \"Sku\": \"IVY-001\", \"Quantity\": 2, \"Price\": 19.99 },\n" +
        "        { \"Sku\": \"IVY-007\", \"Quantity\": 1, \"Price\": 149.0 }\n" +
        "      ]\n" +
        "    },\n" +
        "    {\n" +
        "      \"Id\": 102,\n" +
        "      \"Items\": []\n" +
        "    }\n" +
        "  ],\n" +
        "  \"Notes\": \"This sample highlights nested objects, arrays and scalars.\"\n" +
        "}";

    public record DemoState
    {
        public string ModelJson { get; init; } = DefaultModel;
        public string OutputText { get; init; } = "";
        public string Error { get; init; } = "";
    }

    public override object? Build()
    {
        var state = UseState(() => new DemoState());
        var modelState = this.UseState(state.Value.ModelJson);
        var outputState = this.UseState(state.Value.OutputText);

        string GenerateOutput(string modelJson, out string errorMsg)
        {
            errorMsg = "";
            object? model;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(modelJson);
                model = ToNetObject(doc.RootElement);
            }
            catch (Exception ex)
            {
                errorMsg = "Invalid JSON: " + ex.Message;
                return "";
            }
            
            try
            {
                var settings = new SharpYaml.Serialization.SerializerSettings
                {
                    EmitTags = false,
                    EmitDefaultValues = true,
                    SortKeyForMapping = false
                };
                var serializer = new SharpYaml.Serialization.Serializer(settings);
                var yaml = serializer.Serialize(model);
                return yaml ?? string.Empty;
            }
            catch (Exception ex)
            {
                errorMsg = "YAML serialization error: " + ex.Message;
                return "";
            }
        }

		var modelEditor = modelState.ToCodeInput().Language(Languages.Json).Height(Size.Units(40));
        var outputViewer = outputState.ToCodeInput().Language(Languages.Text).Height(Size.Units(40));
        var errorText = state.Value.Error != "" ? Text.Block(state.Value.Error) : null;

        var convertBtn = new Button("Convert")
            .Primary()
            .HandleClick(_ =>
            {
                string error;
                var output = GenerateOutput(modelState.Value, out error);
                state.Set(state.Value with { OutputText = output, Error = error });
                outputState.Set(output);
            });

        var leftColumn = Layout.Vertical()
            | Text.Block("The Model (valid json)")
            | modelEditor
            | convertBtn;

		var rightColumn = Layout.Vertical()
			| Text.Block("Output")
			| outputViewer
			| (errorText != null ? errorText : Text.Block(""));

        return Layout.Vertical()
            .Gap(2)
            | Text.H2("SharpYaml Online Demo")
            | Layout.Horizontal().Gap(2)
                | leftColumn
                | rightColumn;
    }

    private static object? ToNetObject(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ToNetObject(prop.Value);
                }
                return dict;
            case System.Text.Json.JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ToNetObject(item));
                }
                return list;
            case System.Text.Json.JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case System.Text.Json.JsonValueKind.Number:
                if (element.TryGetInt64(out var i64)) return i64;
                if (element.TryGetDouble(out var d)) return d;
                return 0d;
            case System.Text.Json.JsonValueKind.True:
                return true;
            case System.Text.Json.JsonValueKind.False:
                return false;
            default:
                return null!;
        }
    }
}


