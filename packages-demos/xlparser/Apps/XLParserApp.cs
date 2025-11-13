namespace XLParserExample;

[App(title: "XLParser", icon: Icons.Sheet)]
public class XLParserApp : ViewBase
{
    private readonly string Title = "XLParser Demo";
    private readonly string Description = "Enter Excel formula and parse it";    

    // Example formulas
    private readonly List<string> ExampleFormulas = new()
    {
        "SUM(A1:A10)",
        "IF(B1>10, MAX(B1:B10), MIN(B1:B10))",
        "SUM(A1:A10) + IF(B1>10, MAX(B1:B10), MIN(B1:B10))",
        "VLOOKUP(A1, Sheet2!A:B, 2, FALSE)",
        "INDEX(MATCH(A1, B:B, 0), 1)"
    };

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
        IState<ParseTreeNodeInfo?> SelectedToken,
        IState<Dictionary<string, List<ParseTreeNodeInfo>>> ParsedExamples
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
            SelectedToken: UseState<ParseTreeNodeInfo?>(),
            ParsedExamples: UseState(new Dictionary<string, List<ParseTreeNodeInfo>>())
        );
        
        return new Card()
            .Title(Title)
            .Description(Description)
            | Layout.Horizontal(
                // Left Card - Input Section
                new Card()
                    .Title("Formula Input")
                    | Layout.Vertical(
                        // Example formulas in Expandable
                        new Expandable(
                            "Example Formulas",
                            Layout.Vertical(
                                ExampleFormulas.Select(example => 
                                    new Button(title: example, onClick: _ => 
                                    {
                                        parserState.Formula.Set(example);
                                        HandleParse(parserState);
                                    })
                                    .Outline()
                                    .Secondary()
                                    .Width(Size.Full())
                                )
                            )
                            .Gap(1)
                        ),
                        // Formula Input
                        Text.Label("Excel Formula: "),
                        new TextInput(parserState.Formula),
                        new Button("Parse Formula", onClick: _ => HandleParse(parserState)),
                        // Parse individual elements in Expandable
                        parserState.Tokens.Value.Count > 0 
                            ? new Expandable(
                                "Parse Individual Elements",
                                Layout.Vertical(
                                    parserState.Tokens.Value.Select(token =>
                                    {
                                        var isParsed = parserState.ParsedExamples.Value.ContainsKey(token.NodeValue);
                                        return Layout.Horizontal(
                                            new Button(
                                                title: isParsed ? $"[Parsed] {token.NodeValue}" : token.NodeValue,
                                                onClick: _ => HandleParseElement(token.NodeValue, parserState)
                                            )
                                            .Outline()
                                            .Secondary()
                                            .Foreground(GetTokenColor(token.NodeValue)),
                                            isParsed ? Callout.Success("Parsed") : null
                                        )
                                        .Gap(1)
                                        .WithMargin(left: token.Depth * 2, top: 0, right: 0, bottom: 0);
                                    })
                                )
                                .Gap(1)
                            )
                            : null
                    )
                    .WithMargin(left: 0, top: 0, right: 1, bottom: 0),
                // Right Card - Result Section
                new Card()
                    .Title("Parse Result")
                    | Layout.Vertical(
                        // Result Callout
                        parserState.Result.Value switch
                        {
                            FormulaParseResult.Unknown => null,
                            FormulaParseResult.Parsed => Callout.Success("Formula parsed successfully!"),
                            FormulaParseResult.NotParsed => Callout.Error("Failed to parse the formula. Please check the syntax."),
                            FormulaParseResult.UnexpectedError => Callout.Error("An unexpected error occurred during parsing."),
                            _ => null
                        },
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
                                    GetFilteredNodeInfo(parserState.SelectedToken?.Value?.NodeInfo) is List<NodeMetadata> filteredInfo && filteredInfo.Count > 0
                                        ? (object)filteredInfo
                                        : Text.Label("Select a token to see details")
                                )
                            ),
                            _ => null
                        }
                    )
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
            state.ParsedExamples.Set(new Dictionary<string, List<ParseTreeNodeInfo>>());
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

    private void HandleParseElement(string element, ParserState state)
    {
        try
        {
            var parseTree = FormulaParser.ParseFormula(element);
            var currentExamples = new Dictionary<string, List<ParseTreeNodeInfo>>(state.ParsedExamples.Value)
            {
                [element] = parseTree
            };
            state.ParsedExamples.Set(currentExamples);
        }
        catch
        {
            // Ignore errors for individual element parsing
        }
    }

    private List<NodeMetadata>? GetFilteredNodeInfo(List<NodeMetadata>? nodeInfo)
    {
        if (nodeInfo == null) return null;
        return nodeInfo.Where(metadata => metadata.Value != "False").ToList();
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