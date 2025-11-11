namespace SuperpowerExample
{
    internal class IntegerCalculatorView: ViewBase
    {
        public override object? Build()
        {
            var expressionState = UseState("1 + 2 * 3");
            var errorState = UseState<string>("");
            var resultState = UseState(false);
            var resultValueState = UseState(0);
            var parsingState = UseState(false);

            var eventHandler = (Event<Button> e) =>
            {
                errorState.Set("");
                resultState.Set(false);
                resultValueState.Set(0);
                parsingState.Set(true);

                if (!string.IsNullOrWhiteSpace(expressionState.Value))
                {
                    try
                    {
                        var tokenizer = new ArithmeticExpressionTokenizer();
                        var tokens = tokenizer.Tokenize(expressionState.Value);
                        var expression = ArithmeticExpressionParser.Lambda.Parse(tokens);
                        var compiled = expression.Compile();
                        var result = compiled();
                        resultValueState.Set(result);
                        resultState.Set(true);
                    }
                    catch (Exception ex)
                    {
                        resultState.Set(false);
                        resultValueState.Set(0);
                        errorState.Set($"Error: {ex.Message}");
                    }

                }
                parsingState.Set(false);
            };

            // Input Card
            var inputCard = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Enter Expression")
                | Text.Muted("Type any arithmetic expression and click Calculate to evaluate it.")
                | new Expandable(
                    "Examples",
                    Layout.Vertical().Gap(2)
                        | Text.Muted("Order of operations")
                        | Text.Code("1 + 2 * 3")
                        | Text.Muted("Parentheses")
                        | Text.Code("(10 + 5) * 2")
                        | Text.Muted("Division and subtraction")
                        | Text.Code("100 / 4 - 5")
                        | Text.Muted("Combined operations")
                        | Text.Code("(3 + 7) / 2 + 5")
                )
                | Text.Label("Expression editor:")
                | expressionState.ToTextInput()
                    .Placeholder("Enter arithmetic expression")
                    .Width("100%")
                | new Button("Calculate", eventHandler)
                    .Loading(parsingState.Value)
                    .Variant(ButtonVariant.Primary)
                    .Width("100%")
                | Text.Small("This demo uses Superpower library to evaluate the expression.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Superpower](https://github.com/datalust/superpower)")
            );

            // Result Card
            var resultCard = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Result")
                | (errorState.Value.Length > 0 
                    ? Layout.Vertical().Gap(2)
                        | Text.Block("Calculation Error:")
                        | Text.Code(errorState.Value)
                    : resultState.Value 
                        ? Layout.Vertical().Gap(2)
                            | Text.Muted("Evaluation output:")
                            | Text.Code(resultValueState.Value.ToString())
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("Calculator has not run yet.")
                            | Text.Muted("Enter an expression and press Calculate to view the result here.")
                )
            );

            return Layout.Horizontal().Gap(4)
                | inputCard
                | resultCard;
        }
    }
}
