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
            Layout.Vertical()
            | Text.Block("Value 1:")
            | value1State.ToNumberInput().Width(Size.Full())
            | Text.Block("Operator:")
            | operatorState.ToSelectInput(new string[] { "=", "<", "<=", ">", ">=", "!=" }.ToOptions()).Width(Size.Full())
            | Text.Block("Value 2:")
            | value2State.ToNumberInput().Width(Size.Full())
            | new Button("Serialize", () =>
            {
                Expression<Func<int, bool>>? expression = null;
                switch (operatorState.Value)
                {
                    case "=":
                        expression = value_2 => value1State.Value == value_2;
                        break;
                    case "<":
                        expression = value_2 => value1State.Value < value_2;
                        break;
                    case "<=":
                        expression = value_2 => value1State.Value <= value_2;
                        break;
                    case ">":
                        expression = value_2 => value1State.Value > value_2;
                        break;
                    case ">=":
                        expression = value_2 => value1State.Value >= value_2;
                        break;
                    case "!=":
                        expression = value_2 => value1State.Value != value_2;
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
            | new Button("Deserialize", () =>
            {
                try
                {
                    var serializer = new ExpressionSerializer(new JsonSerializer());
                    Expression<Func<int, bool>> expression = (Expression<Func<int, bool>>)serializer.DeserializeText(jsonState.Value);

                    // Get the operator symbol for display
                    var operatorSymbol = operatorState.Value ?? "=";

                    //Expression definition with substituted values (Value1 operator Value2)
                    expressionState.Set($"Expression: {value1State.Value} {operatorSymbol} {value2State.Value}");

                    //Result of the expresion when using value2
                    var result = expression.Compile()(value2State.Value);
                    comparisonResultState.Set(result ? "true" : "false");
                }
                catch { }
            }).Secondary().Width(Size.Full()).Disabled(string.IsNullOrEmpty(jsonState.Value))
            | new Spacer().Height(Size.Units(5))
            | Text.Small("This demo demonstrates the use of Serialize.Linq to serialize and deserialize LINQ expressions.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Serialize.Linq](https://github.com/esskar/Serialize.Linq)")
             
        ).Title("Input Data").Width(Size.Fraction(0.4f));

        // Right card - Results
        var rightCard = new Card(
            Layout.Vertical()
            | Text.H4("Comparison Result")
            | Text.Small("The result of evaluating the deserialized expression with Value 2:")
            | (string.IsNullOrEmpty(comparisonResultState.Value)
                ? Text.Muted("Click 'Deserialize' to see the comparison result...")
                : (comparisonResultState.Value == "true"
                    ? Callout.Success("The comparison evaluated to true", "Success")
                    : Callout.Error("The comparison evaluated to false", "Failed")))
            | (string.IsNullOrEmpty(expressionState.Value)
                ? null
                : Callout.Info(expressionState.Value, "Expression Definition"))
            | Text.H4("Serialized JSON")
            | Text.Small("The LINQ expression serialized as JSON:")
            | (string.IsNullOrEmpty(jsonState.Value)
                ? Text.Muted("Click 'Serialize' to generate the JSON representation of the expression...")
                : new Code(jsonState.Value, Languages.Json)
                    .ShowLineNumbers()
                    .ShowCopyButton()
                    .Width(Size.Full())
                    .Height(Size.Fit().Min(Size.Units(10)).Max(Size.Units(150))))
        ).Title("Results").Width(Size.Fraction(0.6f));

        return Layout.Vertical()
            | Text.H2("Serialize.Linq Example")
            | Text.Small("Create comparison expressions with two values and an operator. Serialize them to JSON format and deserialize back to evaluate the comparison result.")
            | (Layout.Horizontal().Gap(8)
                | leftCard
                | rightCard);
    }
}