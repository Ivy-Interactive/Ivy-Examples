namespace SmartFormatNetExample;

[App(icon: Icons.Type, title: "SmartFormat.NET")]
public class SmartFormatNetApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        var templateInput = this.UseState("Hello {Name}! You have {MessageCount:plural:no messages|one message|{} messages}.");
        var jsonInput = this.UseState(FormatJson("{ \"Name\": \"John\", \"MessageCount\": 5 }"));
        var outputText = this.UseState("");
        var selectedExampleIndex = this.UseState(-1);

        var examples = new Dictionary<string, (string template, string data)>
        {
            ["Pluralization"] = ("You have {Count:plural:no items|one item|{} items}.", "{ \"Count\": 3 }"),
            ["Choose"] = ("{Gender:choose(male|female):Mr.|Ms.|Mx.} {LastName}", "{ \"Gender\": \"male\", \"LastName\": \"Odiaka\" }"),
            ["Conditional"] = ("{Age:cond:>=55?Senior Citizen|>=30?Adult|>=18?Young Adult|>12?Teenager|>2?Child|Baby}", "{ \"Age\": 32 }"),
            ["List"] = ("Team: {Members:list:{}|, |, and }", "{ \"Members\": [\"Evans\", \"Sarah\", \"Mike\"] }"),
            ["Numbers"] = ("Temperature: {Temp}°C = {TempF:0.0}°F", "{ \"Temp\": 25, \"TempF\": 77 }"),
        };
        var exampleEntries = examples.ToList();
        var exampleOptions = exampleEntries
            .Select((entry, index) => new Option<int>(entry.Key, index))
            .ToArray();

        UseEffect(() => LoadExample(selectedExampleIndex.Value), selectedExampleIndex);

        void LoadExample(int exampleIndex)
        {
            if (exampleIndex < 0 || exampleIndex >= exampleEntries.Count)
            {
                return;
            }

            var example = exampleEntries[exampleIndex].Value;
            templateInput.Value = example.template;
            jsonInput.Value = FormatJson(example.data);
            outputText.Value = "";
        }

        void FormatString()
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonInput.Value);
                var data = ToNetObject(jsonDoc.RootElement);
                var result = Smart.Format(templateInput.Value, data);
                outputText.Value = result;
                client.Toast("Formatted successfully!", "Success");
            }
            catch (Exception ex)
            {
                outputText.Value = $"Error: {ex.Message}";
                client.Toast($"{ex.Message}", "Error");
            }
        }

        return
        Layout.Horizontal().Align(Align.TopCenter)
            | new Card(
            Layout.Vertical()
                | Text.H3("SmartFormat.NET")
                | Text.Muted("Experiment with SmartFormat.NET templates, plug in JSON data, and see pluralization, conditional logic, and list formatting in action.")

                | Text.Label("Examples")
                | selectedExampleIndex
                    .ToSelectInput(exampleOptions)
                    .Variant(SelectInputs.Toggle)

                | Text.Label("Template")
                | templateInput
                    .ToCodeInput(placeholder: "Enter template...")
                    .Language(Languages.Text)
                    .ShowCopyButton()
                    .Height(Size.Fit())

                | Text.Label("Data (JSON)")
                | jsonInput
                    .ToCodeInput(placeholder: "Enter JSON data...")
                    .Language(Languages.Json)
                    .ShowCopyButton()
                    .Height(Size.Fit())

                | new Button("Format String").HandleClick(new Action(FormatString))
                    .HandleClick(new Action(FormatString))
                    .Disabled(string.IsNullOrWhiteSpace(templateInput.Value) || string.IsNullOrWhiteSpace(jsonInput.Value))
                    .Width(Size.Full())

                | (Layout.Horizontal()
                    | Text.Label("Output: ")
                    | new Code(string.IsNullOrEmpty(outputText.Value) ? "Click 'Format String' to see the result..." : outputText.Value)
                        .ShowCopyButton()
                    )
                | new Separator()
                | Text.Small("This demo uses SmartFormat.NET library to format strings.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [SmartFormat.NET](https://github.com/axuno/SmartFormat)")
            ).Width(Size.Fraction(0.4f)).Height(Size.Fit().Min(Size.Full()));
    }

    private static object? ToNetObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ToNetObject(prop.Value);
                }
                return dict;
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ToNetObject(item));
                }
                return list;
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var i64)) return i64;
                if (element.TryGetDouble(out var d)) return d;
                return 0d;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null!;
        }
    }

    private static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }
}
