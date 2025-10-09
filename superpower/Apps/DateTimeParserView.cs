using SuperpowerExample.Helpers;

namespace Superpower.Apps
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
                        errorState.Set($"Error occurred: {ex.Message}");
                    }
                }
                parsingState.Set(false);
            };

            return Layout.Vertical().Gap(2).Padding(2)
                | dateTextState.ToTextInput().WithLabel("Type a date and time string in ISO-8601 format")
                | new Button("Parse", eventHandler).Loading(parsingState.Value)
                | (errorState.Value.Length > 0 ? Text.Block(errorState.Value) : null)
                | (resultState.Value ? new Card(
                    Layout.Vertical().Gap(1).Padding(1)
                    | Text.Block("Result:")
                    | Text.P(resultDateValueState.Value.ToString())
                ).Width(300) : null)
                ;
        }
    }
}
