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
                        errorState.Set($"Error occurred: {ex.Message}");
                    }

                }
                parsingState.Set(false);
            };

            return Layout.Vertical().Gap(2).Padding(2)
                | expressionState.ToTextInput().WithLabel("Type a simple arithmetic expression like (1 + 2 * 3)")
                | new Button("Calculate", eventHandler).Loading(parsingState.Value)
                | (errorState.Value.Length > 0 ? Text.Block(errorState.Value) : null)
                | (resultState.Value ? new Card(
                    Layout.Vertical().Gap(1).Padding(1)
                    | Text.Block("Result:")
                    | Text.P(resultValueState.Value.ToString())
                ).Width(300) : null)
                ;
        }
    }
}
