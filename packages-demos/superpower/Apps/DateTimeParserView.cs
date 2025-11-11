namespace SuperpowerExample
{
    internal class DateTimeParserView: ViewBase
    {
        public override object? Build()
        {
            var dateTextState = UseState("2017-01-01 05:28:10");
            var resultDateValueState = UseState(DateTime.MinValue);
            var errorState = UseState<string>("");
            var resultState = UseState(false);
            var parsingState = UseState(false);

            var eventHandler = (Event<Button> e) =>
            {
                errorState.Set("");
                resultState.Set(false);
                resultDateValueState.Set(DateTime.MinValue);
                parsingState.Set(true);

                if (!string.IsNullOrWhiteSpace(dateTextState.Value))
                {
                    try
                    {
                        var resultDateTime = DateTimeTextParser.Parse(dateTextState.Value);
                        resultDateValueState.Set(resultDateTime);
                        resultState.Set(true);
                    }
                    catch (Exception ex)
                    {
                        resultState.Set(false);
                        resultDateValueState.Set(DateTime.MinValue);
                        errorState.Set($"Error: {ex.Message}");
                    }
                }
                parsingState.Set(false);
            };

            // Input Card
            var inputCard = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Enter Date and Time")
                | new Expandable(
                    "Examples",
                    Layout.Vertical().Gap(2)
                        | Text.Code("2017-01-01")
                        | Text.Code("2017-01-01 05:28:10")
                        | Text.Code("2017-01-01 05:28")
                        | Text.Code("2017-01-01T05:28:10")
                        | Text.Code("2017-01-01T05:28")
                        | Text.Code("2023-12-31T23:59:59Z")
                )
                | dateTextState.ToTextInput()
                    .Placeholder("Enter date and time")
                    .Width("100%")
                | new Button("Parse Date", eventHandler)
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
                        | Text.Block("Parsing Error:")
                        | Text.Code(errorState.Value)
                    : resultState.Value 
                        ? Layout.Vertical().Gap(2)
                            | Text.Block("Parsed Date:")
                            | Text.H3(resultDateValueState.Value.ToString("yyyy-MM-dd HH:mm:ss"))
                            | Text.Muted($"Day of Week: {resultDateValueState.Value.DayOfWeek}")
                            | Text.Muted($"Day of Year: {resultDateValueState.Value.DayOfYear}")
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("Waiting for result...")
                            | Text.Muted("Enter date and click 'Parse Date'")
                )
            );

            return Layout.Horizontal().Gap(4)
                | inputCard
                | resultCard;
        }
    }
}
