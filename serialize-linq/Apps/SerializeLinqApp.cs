namespace SerializeLinqExample;

[App(icon: Icons.Pencil, title: "Serialize.Linq")]
public class SerializeLinqApp : ViewBase
{
    public override object? Build()
    {
        //Input states
        var value1State = this.UseState<int>();
        var value2State = this.UseState<int>();
        var operatorState = this.UseState<string>();

        //Serialization state
        var jsonState = this.UseState<string>();

        //Deserialization states
        var expressionState = this.UseState<string>();
        var comparisonResultState = this.UseState<string>();

        // Left card - Inputs and buttons
        var leftCard = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H3("Input Data")
            | Text.Block("Value 1:")
            | value1State.ToNumberInput().Width(Size.Full())
            | Text.Block("Operator:")
            | operatorState.ToSelectInput(new string[] { "=", "<", "<=", ">", ">=", "!=" }.ToOptions()).Width(Size.Full())
            | Text.Block("Value 2:")
            | value2State.ToNumberInput().Width(Size.Full())
            | new Separator()
            | new Button("Serialize", () =>
            {
                Expression<Func<int, bool>>? expression = null;
                switch (operatorState.Value)
                {
                    case "=":
                        expression = val => value1State.Value == val;
                        break;
                    case "<":
                        expression = val => value1State.Value < val;
                        break;
                    case "<=":
                        expression = val => value1State.Value <= val;
                        break;
                    case ">":
                        expression = val => value1State.Value > val;
                        break;
                    case ">=":
                        expression = val => value1State.Value >= val;
                        break;
                    case "!=":
                        expression = val => value1State.Value != val;
                        break;
                }
                if (expression != null)
                {
                    var serializer = new ExpressionSerializer(new JsonSerializer());

                    //The result is a json representation of the expression
                    var json = serializer.SerializeText(expression);
                    
                    // Format JSON with indentation
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        json = System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                    }
                    catch
                    {
                        // If formatting fails, use original JSON
                    }
                    
                    jsonState.Set(json);
                }
                else
                {
                    jsonState.Set("Invalid expression");
                }
            }).Primary().Width(Size.Full())
            | new Button("Deserialize Result", () =>
            {
                try
                {
                    var serializer = new ExpressionSerializer(new JsonSerializer());
                    Expression<Func<int, bool>> expression = (Expression<Func<int, bool>>)serializer.DeserializeText(jsonState.Value);

                    //Expression definition (value1 + operator)
                    expressionState.Set($"Expression: {expression}");

                    //Result of the expresion when using value2
                    comparisonResultState.Set($"The comparison is {expression.Compile()(value2State.Value).ToString().ToLower()}");
                }
                catch { }
            }).Secondary().Width(Size.Full())
        ).Width(Size.Fraction(0.4f));

        // Right card - Results
        var rightCard = new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H3("Results")
            | Text.Block("Serialization JSON Result:")
            | (string.IsNullOrEmpty(jsonState.Value) 
                ? Text.Muted("JSON result will appear here after serialization...")
                : new Code(jsonState.Value, Languages.Json)
                    .ShowLineNumbers()
                    .ShowCopyButton()
                    .Width(Size.Full())
                    .Height(Size.Fit().Min(Size.Units(10))))
            | new Separator()
            | Text.Block("Expression Definition:")
            | (string.IsNullOrEmpty(expressionState.Value)
                ? Text.Muted("Expression definition will appear here after deserialization...")
                : Text.Block(expressionState.Value))
            | Text.Block("Comparison Result:")
            | (string.IsNullOrEmpty(comparisonResultState.Value)
                ? Text.Muted("Comparison result will appear here after deserialization...")
                : Text.Block(comparisonResultState.Value))
        ).Width(Size.Fraction(0.6f));

        return Layout.Vertical()
            | Text.H2("Serialize.Linq Example")
            | (Layout.Horizontal().Gap(8)
                | leftCard
                | rightCard);
    }
}