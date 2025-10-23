namespace HumanizerExample;

[App(icon: Icons.Box)]
public class PlainTextHumanizerApp : ViewBase
{
    readonly string Title = "Plain Text Humanizer";
    readonly string Description = "Enter text and choose an option to transform it";

    public override object? Build()
    {
        var inputText = this.UseState("");
        var humanizedTexts = this.UseState(ImmutableArray.Create<string>());
        var truncateValue = this.UseState(4);
        PlainTextOptions options = new(inputText, humanizedTexts, truncateValue);

        return
            // Container -> Card
            new Card().Title(Title).Description(Description)
            | (Layout.Vertical()
                // Truncate Value
                | Text.Label("Truncate Value: ").Color(Colors.Yellow)
                | new NumberInput<int>(truncateValue)
                    .Min(0)
                    .Max(100)
                    .Size(1)
                // Input Text
                | Text.Label("Plain Text Input").Color(Colors.Yellow)
                | (Layout.Horizontal().Width(Size.Full())
                    | inputText.ToTextInput(placeholder: "New text...").Width(Size.Grow())
                )
                // Options
                | options
                // History
                | Text.Label("History").Color(Colors.Yellow)
                | (Layout.Vertical()
                 | humanizedTexts.Value.Reverse().Select(text => new string(text))
                )
            );
    }
}