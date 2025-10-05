using Serialize.Linq.Serializers;
using System.Linq.Expressions;

namespace SerializeLinq.Apps;

[App(icon: Icons.Pencil, title: "Serialize Linq")]
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

        return Layout.Vertical()
            | Text.Block("Hello world!")
            //Inputs 
            | (Layout.Horizontal()
                | value1State.ToNumberInput().Width(Size.Grow())
                | operatorState.ToSelectInput(new string[] { "=", "<", "<=", ">", ">=", "!=" }.ToOptions()).Width(Size.Third())
                | value2State.ToNumberInput().Width(Size.Grow()))
            //Serialize button
            | new Button("Serialize", () =>
            {
                Expression<Func<int, bool>> expression = null;
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
                    jsonState.Set(serializer.SerializeText(expression));
                }
                else
                {
                    jsonState.Set("Invalid expression");
                }
            })
            //Serialization result
            | Text.Block(jsonState)
            //Deserialize button (only works with serialization result, works like a validation for the json result)
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
            })
            //Deserialization results
            | Text.Block(expressionState.Value)
            | Text.Block(comparisonResultState);
    }
}