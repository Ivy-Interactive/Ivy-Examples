using SmartFormat;
using System.Text.Json;

namespace SmartFormatNetDemo.Apps;

[App(icon: Icons.Type, title: "SmartFormat.NET")]
public class SmartFormatApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        var templateInput = this.UseState("Hello {Name}! You have {MessageCount:plural:no messages|one message|{} messages}.");
        var jsonInput = this.UseState("{ \"Name\": \"John\", \"MessageCount\": 5 }");
        var outputText = this.UseState("");
        var selectedExample = this.UseState("Basic");

        var examples = new Dictionary<string, (string template, string data)>
        {
            ["Basic"] = ("Hello {Name}!", "{ \"Name\": \"Evans Odiaka\" }"),
            ["Pluralization"] = ("You have {Count:plural:no items|one item|{} items}.", "{ \"Count\": 3 }"),
            ["Conditional"] = ("{Gender:male?Mr.|female?Ms.|Mx.} {LastName}", "{ \"Gender\": \"male\", \"LastName\": \"Odiaka\" }"),
            ["List"] = ("Team: {Members:list:{}|, |, and }", "{ \"Members\": [\"Evans\", \"Sarah\", \"Mike\"] }"),
            ["Numbers"] = ("Temperature: {Temp}°C = {TempF:0.0}°F", "{ \"Temp\": 25, \"TempF\": 77 }"),
        };

        void LoadExample(string exampleName)
        {
            if (examples.TryGetValue(exampleName, out var example))
            {
                templateInput.Value = example.template;
                jsonInput.Value = example.data;
                selectedExample.Value = exampleName;
                outputText.Value = "";
            }
        }

        void FormatString()
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonInput.Value);
                var data = ToNetObject(jsonDoc.RootElement);
                var result = Smart.Format(templateInput.Value, data);
                outputText.Value = result;
                client.Toast("✅ Formatted successfully!");
            }
            catch (Exception ex)
            {
                outputText.Value = $"❌ Error: {ex.Message}";
                client.Toast($"❌ {ex.Message}");
            }
        }

        return Layout.Vertical(
                Text.H1("SmartFormat.NET Demo"),
                Text.P("String formatting with smart pluralization and conditionals"),
                
                Text.H3("Examples"),
                Layout.Horizontal(
                    examples.Keys.Select(name => 
                        new Button(name, onClick: () => LoadExample(name))
                    )
                ).Wrap(),
                
                templateInput
                    .ToTextInput()
                    .Placeholder("Enter template...")
                    .WithLabel("Template"),
                
                jsonInput
                    .ToTextInput()
                    .Placeholder("Enter JSON data...")
                    .WithLabel("Data (JSON)"),
                
                new Button("Format String", onClick: FormatString)
                    .Disabled(string.IsNullOrWhiteSpace(templateInput.Value) || string.IsNullOrWhiteSpace(jsonInput.Value)),
                
                Text.H3("Output"),
                new Box()
                    .Color(outputText.Value.StartsWith("❌") ? Colors.Red : Colors.Gray)
                    .Padding(10)
                    | Text.P(string.IsNullOrEmpty(outputText.Value) 
                        ? "Click 'Format String' to see the result..." 
                        : outputText.Value)
                        .Color(Colors.White),
                
                Text.H3("Features"),
                Layout.Vertical(
                    Text.P("Pluralization - handles singular/plural automatically"),
                    Text.P("Conditionals - different output based on data"),
                    Text.P("List formatting - join arrays with custom separators"),
                    Text.P("Number formatting - control decimal places and units")
                )
            )
            .Width(Size.Units(170))
            .Padding(20);
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
}
