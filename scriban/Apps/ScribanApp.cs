namespace ScribanDemo.Apps;

[App(icon: Icons.ScrollText, title: "Scriban Demo")]
public class ScribanApp : ViewBase
{
    private const string DefaultModel = """
	{ "name": "Bob Smith", "address": "1 Smith St, Smithville", "orderId": "123455", "total": 23435.34, "items": [ { "name": "1kg carrots", "quantity": 1, "total": 4.99 }, { "name": "2L Milk", "quantity": 1, "total": 3.5 } ] }
""";

	private const string DefaultTemplate = """
	Dear {{ model.name }},

	Your order, {{ model.orderId }}, is now ready to be collected.

	Your order shall be delivered to {{ model.address }}. If you need it delivered to another location, please contact as ASAP.

	Order: {{ model.orderId }}
	Total: {{ model.total | math.format "c" "en-US" }}
	""";

    public record DemoState
    {
        public string ModelJson { get; init; } = DefaultModel;
        public string TemplateText { get; init; } = DefaultTemplate;
        public string OutputText { get; init; } = "";
        public string Error { get; init; } = "";
    }

    public override object? Build()
    {
		var state = UseState(() => new DemoState());
		var modelState = this.UseState(state.Value.ModelJson);
		var templateState = this.UseState(state.Value.TemplateText);
		var outputState = this.UseState(state.Value.OutputText);

        string GenerateOutput(string modelJson, string templateText, out string errorMsg)
        {
            errorMsg = "";
            object model;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(modelJson);
                model = ToScriptValue(doc.RootElement);
            }
            catch (Exception ex)
            {
                errorMsg = "Invalid JSON: " + ex.Message;
                return "";
            }

            var template = global::Scriban.Template.Parse(templateText);
            if (template.HasErrors)
            {
                errorMsg = string.Join("\n", template.Messages.Select(m => m.ToString()));
                return "";
            }

			var context = new global::Scriban.TemplateContext();
			var globals = new global::Scriban.Runtime.ScriptObject();
			globals.Add("model", model);
			global::Scriban.Runtime.ScriptObjectExtensions.Import(globals, typeof(global::Scriban.Functions.MathFunctions));
            context.PushGlobal(globals);

            try
            {
                var result = template.Render(context);
                return result;
            }
            catch (Exception ex)
            {
                errorMsg = "Rendering error: " + ex.Message;
                return "";
            }
        }

		var modelEditor = modelState.ToTextAreaInput();
		var templateEditor = templateState.ToTextAreaInput();
		var errorText = state.Value.Error != "" ? Text.Block(state.Value.Error) : null;

        var generateBtn = new Button("Generate")
            .Primary()
            .HandleClick(_ =>
            {
				string error;
				var output = GenerateOutput(modelState.Value, templateState.Value, out error);
				state.Set(state.Value with { OutputText = output, Error = error });
				outputState.Set(output);
            });

		var leftColumn = Layout.Vertical()
			| Text.Markdown("Scriban Template ([Template docs](https://github.com/scriban/scriban))")
			| templateEditor
			| generateBtn;

		var rightColumn = Layout.Vertical()
			| Text.Block("Output")
			| Text.Markdown("```text\n" + outputState.Value + "\n```")
			| (errorText != null ? errorText : Text.Block(""));

		return Layout.Vertical()
			.Gap(2)
			| Text.H2("Scriban Online Demo")
			| Text.Block("The Model (valid json)") | modelEditor
			| Layout.Horizontal().Gap(2)
				| leftColumn
				| rightColumn;
    }

    private static object ToScriptValue(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                var so = new global::Scriban.Runtime.ScriptObject();
                foreach (var prop in element.EnumerateObject())
                {
                    so.Add(prop.Name, ToScriptValue(prop.Value));
                }
                return so;
            case System.Text.Json.JsonValueKind.Array:
                var sa = new global::Scriban.Runtime.ScriptArray();
                foreach (var item in element.EnumerateArray())
                {
                    sa.Add(ToScriptValue(item));
                }
                return sa;
            case System.Text.Json.JsonValueKind.String:
                return element.GetString() ?? "";
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
