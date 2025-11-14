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
                | Text.Muted("Provide an ISO-8601 date/time string and click Parse to see its components.")
                | new Expandable(
                    "Examples",
                    Layout.Vertical().Gap(2)
                        | Text.Muted("Date only")
                        | Text.Code("2017-01-01")
                        | Text.Muted("Date and time with seconds")
                        | Text.Code("2017-01-01 05:28:10")
                        | Text.Muted("Date and time without seconds")
                        | Text.Code("2017-01-01 05:28")
                        | Text.Muted("ISO format with 'T'")
                        | Text.Code("2017-01-01T05:28:10")
                        | Text.Muted("ISO format without seconds")
                        | Text.Code("2017-01-01T05:28")
                )
                | Text.Label("Date/time input:")
                | dateTextState.ToTextInput()
                    .Placeholder("Enter date and time")
                    .Width("100%")
                | new Button("Parse Date", eventHandler)
                    .Loading(parsingState.Value)
                    .Variant(ButtonVariant.Primary)
                    .Width("100%")
                | Text.Small("This demo uses Superpower library to parse the date/time.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Superpower](https://github.com/datalust/superpower)")
            );

            // Result Card
            var resultCard = new Card(
                Layout.Vertical().Gap(3).Padding(3)
                | Text.H4("Result")
                | (errorState.Value.Length > 0 
                    ? Layout.Vertical().Gap(2)
                        | Text.Block("Parsing Error:")
                        | Text.Code(errorState.Value)
                    : resultState.Value 
                        ? Layout.Vertical().Gap(2)
                            | Text.Muted("Parsed date/time details:")
                            | Text.Label("Formatted date:")
                            | Text.Code(resultDateValueState.Value.ToString("yyyy-MM-dd HH:mm:ss"))
                            | Text.Label("Day of Week:")
                            | Text.Code(resultDateValueState.Value.DayOfWeek.ToString())
                            | Text.Label("Day of Year:")
                            | Text.Code(resultDateValueState.Value.DayOfYear.ToString())
                        : Layout.Vertical().Gap(2)
                            | Text.Muted("Parser has not run yet.")
                            | Text.Muted("Enter a date/time value and press Parse to inspect the parsed output here.")
                )
            );

            return Layout.Horizontal().Gap(4)
                | inputCard
                | resultCard;
        }
    }
}
