using XLParserDemo.Services;

namespace XLParserDemo.Apps;

[App(title: "XLParser", icon: Icons.Sheet)]
public class XLParserApp : ViewBase
{
    private readonly string Title = "XLParser Demo";
    private readonly string Description = "Enter Excel formula and parse it";    

    // Component state for token coloring, preserved across renders.
    private readonly Queue<Colors> _chromaticColors = new([
        Colors.Red, Colors.Orange, Colors.Amber, Colors.Yellow, Colors.Lime,
        Colors.Green, Colors.Emerald, Colors.Teal, Colors.Cyan, Colors.Sky,
        Colors.Blue, Colors.Indigo, Colors.Violet, Colors.Purple, Colors.Fuchsia,
        Colors.Pink, Colors.Rose
    ]);
    private readonly Dictionary<string, Colors> _foundTokenTypes = [];

    private record ParserState(
        IState<string> Formula,
        IState<FormulaParseResult> Result,
        IState<List<ParseTreeNodeInfo>> Tokens,
        IState<ParseTreeNodeInfo?> SelectedToken
    );

    private enum FormulaParseResult
    {
        Unknown,
        Parsed,
        NotParsed,
        UnexpectedError
    };

    public override object? Build()
    {
        // State management        
        var parserState = new ParserState(
            Formula: UseState("SUM(A1:A10) + IF(B1>10, MAX(B1:B10), MIN(B1:B10))"),
            Result: UseState(FormulaParseResult.Unknown),
            Tokens: UseState(new List<ParseTreeNodeInfo>()),
            SelectedToken: UseState<ParseTreeNodeInfo?>()
        );
        
        return new Card()
            .Title(Title)
            .Description(Description)
            | Layout.Vertical(
                // Formula Input Section
                Layout.Vertical(
                    Text.Label("Excel Formula: "),
                    new TextInput(parserState.Formula),
                    new Button("Parse Formula", onClick: _ => HandleParse(parserState))
                ),
                new Separator(),
                // Parse Result Section
                parserState.Result.Value switch
                {
                    FormulaParseResult.Unknown => Text.Label("Click 'Parse Formula' to see the result."),
                    FormulaParseResult.Parsed => Layout.Horizontal(
                        Layout.Vertical(                         
                            Text.Small("Click on tokens to see details."),
                            Layout.Vertical(parserState.Tokens.Value.Select(token =>
                            {
                                return new Button(title: token.NodeValue, onClick: _ => parserState.SelectedToken.Set(token))
                                    .Outline()
                                    .Secondary()
                                    .Foreground(GetTokenColor(token.NodeValue))                                  
                                    .WithMargin(left: token.Depth, top: 0, right: 0, bottom: 0);
                            }))
                            .Gap(1)),
                            Layout.Vertical(
                                Text.Label("Selected Token Details:"),
                                parserState.SelectedToken?.Value?.NodeInfo)
                        ),
                    FormulaParseResult.NotParsed => Callout.Error("The formula could not be parsed. Please check the syntax."),
                    FormulaParseResult.UnexpectedError => Callout.Error("An unexpected error occurred during parsing."),
                    _ => null
                }
            );
    }

    private void HandleParse(ParserState state)
    {
        try
        {
            var parseTree = FormulaParser.ParseFormula(state.Formula.Value);

            state.Tokens.Set([.. parseTree]);
            state.Result.Set(FormulaParseResult.Parsed);
            state.SelectedToken.Set(parseTree.FirstOrDefault());
        }
        catch (ArgumentException)
        {            
            state.Result.Set(FormulaParseResult.NotParsed);
        }
        catch (Exception)
        {         
            state.Result.Set(FormulaParseResult.UnexpectedError);
        }
    }

    private Colors GetTokenColor(string tokenName)
    {
        if (!_foundTokenTypes.TryGetValue(tokenName, out var color))
        {
            color = _chromaticColors.Count > 0 ? _chromaticColors.Dequeue() : Colors.Gray;
            _foundTokenTypes[tokenName] = color;
        }
        return color;
    }
}