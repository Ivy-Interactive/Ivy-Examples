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
                | Layout.Vertical().Gap(2)
                    | Text.Muted("Examples:")
                    | Text.Code("1 + 2 * 3")
                    | Text.Code("(10 + 5) * 2")
                    | Text.Code("100 / 4 - 5")
                | expressionState.ToTextInput()
                    .Placeholder("Enter arithmetic expression")
                    .Width("100%")
                | new Button("🧮 Calculate", eventHandler)
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
                        | Text.Block("❌ Calculation Error:")
                        | Text.Code(errorState.Value)
                    : resultState.Value 
                        ? Layout.Vertical().Gap(2)
                            | Text.Block("✅ Result:")
                            | Text.H2(resultValueState.Value.ToString())
                            | Text.Muted($"Expression: {expressionState.Value}")
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("Waiting for calculation...")
                            | Text.Muted("Enter expression and click 'Calculate'")
                )
            );

            return Layout.Horizontal().Gap(4)
                | inputCard
                | resultCard;
        }
    }
}
