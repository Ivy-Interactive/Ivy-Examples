namespace XLParserExample;

[App(title: "XLParser", icon: Icons.Sheet)]
public class XLParserApp : ViewBase
{
    // Example formulas
    private readonly List<string> ExampleFormulas = new()
    {
        "SUM(A1:A10)",
        "IF(B1>10, MAX(B1:B10), MIN(B1:B10))",
        "SUM(A1:A10) + IF(B1>10, MAX(B1:B10), MIN(B1:B10))",
        "VLOOKUP(A1, Sheet2!A:B, 2, FALSE)",
        "INDEX(MATCH(A1, B:B, 0), 1)"
    };

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
        var parserState = new ParserState(
            Formula: UseState("SUM(A1:A10) + IF(B1>10, MAX(B1:B10), MIN(B1:B10))"),
            Result: UseState(FormulaParseResult.Unknown),
            Tokens: UseState(new List<ParseTreeNodeInfo>()),
            SelectedToken: UseState<ParseTreeNodeInfo?>()
        );

        return Layout.Vertical()
        | new Card(
            Layout.Vertical(
                Text.H4("Excel Formula Parser"),
                Text.Muted("Parse and analyze Excel formulas to understand their structure and components"),
                Layout.Horizontal(
                    // Left Card - Input Section
                    new Card()
                    | Layout.Vertical(
                        Text.H4("Input & Formula Structure"),
                        Text.Muted("Enter or select an Excel formula to parse and view its token structure"),
                        new Expandable(
                            "Example Formulas",
                            Layout.Vertical(
                                ExampleFormulas.Select(example =>
                                    new Code(example)
                                        .ShowCopyButton()
                                )
                            )
                            .Gap(1)
                        ),
                        Text.Label("Excel Formula: "),
                        new TextInput(parserState.Formula),
                        new Button("Parse Formula", onClick: _ => HandleParse(parserState)),
                        parserState.Result.Value == FormulaParseResult.Parsed && parserState.Tokens.Value.Count > 0
                            ? new Expandable(
                                "Parsed Tokens",
                                Layout.Vertical(
                                    parserState.Tokens.Value.Select(token =>
                                        new Button(
                                            title: token.NodeValue,
                                            onClick: _ => parserState.SelectedToken.Set(token)
                                        )
                                        .Outline()
                                        .Secondary()
                                        .WithMargin(left: token.Depth * 2, top: 0, right: 0, bottom: 0)
                                    )
                                )
                                .Gap(2)
                            )
                            : null
                    ),
                // Right Card - Result Section
                new Card()
                    | Layout.Vertical(
                        Text.H4("Token Analysis"),
                        Text.Muted("Select a token from the parsed formula to view its detailed properties and metadata"),
                        parserState.Result.Value switch
                        {
                            FormulaParseResult.Unknown => null,
                            FormulaParseResult.Parsed => Callout.Success("Formula parsed successfully!"),
                            FormulaParseResult.NotParsed => Callout.Error("Failed to parse the formula. Please check the syntax."),
                            FormulaParseResult.UnexpectedError => Callout.Error("An unexpected error occurred during parsing."),
                            _ => null
                        },
                        parserState.Result.Value switch
                        {
                            FormulaParseResult.Unknown => Text.Label("Enter a formula and click 'Parse Formula' to see the analysis."),
                            FormulaParseResult.Parsed => Layout.Vertical(
                                Layout.Vertical(
                                    Text.Label("Token Properties:"),
                                    GetFilteredNodeInfo(parserState.SelectedToken?.Value?.NodeInfo) is List<NodeMetadata> filteredInfo && filteredInfo.Count > 0
                                        ? filteredInfo.ToTable().Width(Size.Full())
                                        : Text.Label("Select a token from the parsed formula to view its properties")
                                )
                            ),
                            _ => null
                        }
                    )
                )
            )
            | Layout.Vertical(
                Text.Small("This demo uses XLParser library to parse and analyze Excel formulas."),
                Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [XLParser](https://github.com/spreadsheetlab/XLParser)")
            ));
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

    private List<NodeMetadata>? GetFilteredNodeInfo(List<NodeMetadata>? nodeInfo)
    {
        if (nodeInfo == null) return null;
        return nodeInfo.Where(metadata => metadata.Value != "False").ToList();
    }
}