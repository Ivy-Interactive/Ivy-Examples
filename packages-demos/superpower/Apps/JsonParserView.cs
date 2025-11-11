namespace SuperpowerExample
{
    internal class JsonParserView : ViewBase
    {
        public override object? Build()
        {
            var jsonState = UseState(@"{
  ""name"": ""John"",
  ""age"": 30,
  ""city"": ""New York""
}");
            var errorState = UseState<string>("");
            var parsedDataState = UseState("");
            var parsingState = UseState(false);

            var eventHandler = (Event<Button> e) =>
            {
                errorState.Set("");
                parsedDataState.Set("");
                parsingState.Set(true);

                if (!string.IsNullOrWhiteSpace(jsonState.Value))
                {
                    if (JsonParser.TryParse(jsonState.Value, out var value, out var error, out var errorPosition))
                    {
                        parsedDataState.Set(GetIndentedText(value));
                    }
                    else
                    {
                        parsedDataState.Set("");
                        parsingState.Set(false);
                        errorState.Set($"Error: {error}\nPosition: {errorPosition.Column}");                        
                    }
                }
                parsingState.Set(false);
            };

            // Input Card
            var inputCard = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Enter JSON")
                | new TextInput(jsonState)
                    .Placeholder("Type or paste JSON here")
                    .Variant(TextInputs.Textarea)
                    .Height(300)
                | new Button("🔍 Parse JSON", eventHandler)
                    .Loading(parsingState.Value)
                    .Variant(ButtonVariant.Primary)
                    .Width("100%")
            );

            // Result Card
            var resultCard = new Card(
                Layout.Vertical().Gap(2).Padding(3)
                | Text.H4("Result")
                | (errorState.Value.Length > 0 
                    ? Layout.Vertical().Gap(2)
                        | Text.Block("❌ Parsing Error:")
                        | Text.Code(errorState.Value)
                    : parsedDataState.Value.Length > 0 
                        ? Layout.Vertical().Gap(2)
                            | Text.Block("✅ Parsed Data:")
                            | Text.Code(parsedDataState.Value)
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("Waiting for result...")
                            | Text.Muted("Click 'Parse JSON' button")
                )
            );

            return Layout.Horizontal().Gap(4)
                | inputCard
                | resultCard;
        }

        static string GetIndentedText(object? value, int indent = 0)
        {
            string result = "";
            
            void Indent(int amount, string text)
            {
                result += $"{new string(' ', amount)}{text}\n";
            }
            
            switch (value)
            {
                case null:
                    Indent(indent, "Null");
                    break;
                case true:
                    Indent(indent, "True");
                    break;
                case false:
                    Indent(indent, "False");
                    break;
                case double n:
                    Indent(indent, $"Number: {n}");
                    break;
                case string s:
                    Indent(indent, $"String: {s}");
                    break;
                case object[] a:
                    Indent(indent, "Array:");
                    foreach (var el in a)
                        result += GetIndentedText(el, indent + 2);
                    break;
                case Dictionary<string, object> o:
                    Indent(indent, "Object:");
                    foreach (var p in o)
                    {
                        Indent(indent + 2, p.Key);
                        result += GetIndentedText(p.Value, indent + 4);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }
    }
}
