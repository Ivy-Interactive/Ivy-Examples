namespace ScribanExample;

[App(icon: Icons.ScrollText, title: "Scriban")]
public class ScribanApp : ViewBase
{
    private const string DefaultModel = "{\n  \"name\": \"Bob Smith\",\n  \"address\": \"1 Smith St, Smithville\",\n  \"orderId\": \"123455\",\n  \"total\": 23435.34,\n  \"items\": [\n    {\n      \"name\": \"1kg carrots\",\n      \"quantity\": 1,\n      \"total\": 4.99\n    },\n    {\n      \"name\": \"2L Milk\",\n      \"quantity\": 1,\n      \"total\": 3.5\n    }\n  ]\n}";

	private const string DefaultTemplate = """
	Dear {{ model.name }},

	Your order, {{ model.orderId }}, is now ready to be collected.

	Your order shall be delivered to {{ model.address }}. If you need it delivered to another location, please contact us ASAP.

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

		var modelEditor = modelState.ToCodeInput().Language(Languages.Json).Height(Size.Fit());
		var templateEditor = templateState.ToCodeInput().Language(Languages.Markdown).Height(Size.Fit());
		var outputViewer = string.IsNullOrEmpty(outputState.Value)
			? outputState.ToTextAreaInput("Output will appear here after generation...").Disabled().Height(Size.Units(50))
			: outputState.ToTextAreaInput().Height(Size.Units(50));
		

        var generateBtn = new Button("Generate")
            .Primary()
            .HandleClick(_ =>
            {
				string error;
				var output = GenerateOutput(modelState.Value, templateState.Value, out error);
				state.Set(state.Value with { OutputText = output, Error = error });
				outputState.Set(output);
            });

		// Header section with title and description
		var headerSection = Layout.Vertical()
			| Text.H2("Scriban Template Engine")
			| Text.Muted("Scriban is a powerful, fast, and secure templating engine for .NET. Use this tool to create templates with JSON model support. Enter a model in JSON format and a Scriban template to generate the output.");

        // Left card - Model input, Template input and Generate button
        var leftCardContent = Layout.Vertical()
            | Text.H4("Model (JSON)")
            | Text.Muted("Enter the model in JSON format. This will be used to generate the output.")
            | modelEditor
            | generateBtn
            | Text.Small("This demo uses Scriban to generate the output.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Scriban](https://github.com/scriban/scriban)")
        ;
			

		var leftCard = new Card(leftCardContent).Width(Size.Fraction(0.45f));

        // Right card - Output (markdown/text)
        var rightCardContent = Layout.Vertical()
            | Text.H4("Scriban Template")
            | Text.Muted("Enter the Scriban template to generate the output. Use the model variables in the template to generate the output.")
            | templateEditor
            | Text.H4("Output")
            | outputViewer;

		var rightCard = new Card(rightCardContent).Width(Size.Fraction(0.55f));

		// Two horizontal cards
		var cardsRow = Layout.Horizontal().Gap(6).Padding(3)
			| leftCard
			| rightCard;

		return Layout.Vertical()
			.Gap(3)
			| headerSection
			| cardsRow;
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
