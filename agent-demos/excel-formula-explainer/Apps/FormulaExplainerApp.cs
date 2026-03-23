using Ivy;

namespace Excel.Formula.Explainer.Apps;

[App(icon: Icons.FileSpreadsheet)]
public class FormulaExplainerApp : ViewBase
{
    private static readonly string[] ExampleFormulas =
    [
        "=VLOOKUP(A2, Sheet2!A:C, 3, FALSE)",
        "=IF(SUMIFS(B:B, A:A, \"Sales\") > 1000, \"Over Budget\", \"OK\")",
        "=INDEX(B2:B100, MATCH(MIN(C2:C100), C2:C100, 0))",
        "=IFERROR(VLOOKUP(D2, Products!A:B, 2, FALSE), \"Not Found\")",
        "=SUMPRODUCT((A2:A100=\"East\")*(B2:B100>50))",
        "=XLOOKUP(E2, A2:A100, B2:B100, \"N/A\")",
        "=AVERAGE(IF(A2:A100=\"Q1\", B2:B100))",
        "=COUNTIF(Status, \"Complete\")/COUNTA(Status)"
    ];

    public override object Build()
    {
        var formulaState = UseState("");
        var resultState = UseState<FormulaResult?>(null);

        void Explain()
        {
            var formula = formulaState.Value?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(formula)) return;
            resultState.Set(FormulaParser.Parse(formula));
        }

        void LoadExample(string example)
        {
            formulaState.Set(example);
            resultState.Set(FormulaParser.Parse(example));
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | BuildHeader()
                | BuildInput(formulaState, Explain)
                | BuildResults(resultState.Value)
                | new Separator()
                | BuildExamples(LoadExample)
            );
    }

    private static object BuildHeader()
    {
        return Layout.Vertical().Gap(2).Align(Align.Center)
            | new Icon(Icons.FileSpreadsheet).Large()
            | Text.H1("Excel Formula Explainer")
            | Text.Muted("Paste any Excel formula to get a plain-English explanation");
    }

    private static object BuildInput(IState<string> formulaState, Action onExplain)
    {
        return Layout.Vertical().Gap(3)
            | formulaState.ToTextareaInput()
                .Placeholder("=VLOOKUP(A2, Sheet2!A:C, 3, FALSE)")
                .Rows(3)
            | new Button("Explain Formula", onExplain).Primary().Icon(Icons.Sparkles);
    }

    private static object BuildResults(FormulaResult? result)
    {
        if (result == null) return new Fragment();

        return Layout.Vertical()
            | new Separator()
            | Text.H2("Explanation")
            | new Card(
                Layout.Vertical().Gap(2)
                    | Text.P(result.Summary).Bold()
            ).Icon(new Icon(Icons.Lightbulb))
            | BuildSteps(result.Steps)
            | BuildFunctions(result.FunctionsUsed);
    }

    private static object BuildSteps(List<FormulaStep> steps)
    {
        if (steps.Count == 0) return new Fragment();

        var layout = Layout.Vertical().Gap(2);
        layout |= Text.H3("Step-by-Step Breakdown");

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            layout |= new Card(
                Layout.Vertical().Gap(1)
                    | Text.Monospaced(step.Expression)
                    | Text.P(step.Explanation)
            ).Title($"Step {i + 1}: {step.FunctionName}");
        }

        return layout;
    }

    private static object BuildFunctions(List<FunctionInfo> functions)
    {
        if (functions.Count == 0) return new Fragment();

        var layout = Layout.Vertical().Gap(2);
        layout |= Text.H3("Functions Used");

        var grid = Layout.Grid().Columns(2).Gap(2);
        foreach (var fn in functions)
        {
            grid |= new Card(
                Text.P(fn.Description)
            ).Title(fn.Name).Icon(new Icon(Icons.Code));
        }

        return layout | grid;
    }

    private static object BuildExamples(Action<string> onSelect)
    {
        var layout = Layout.Vertical().Gap(2);
        layout |= Text.H3("Try an Example");

        var wrap = Layout.Wrap().Gap(2);
        foreach (var example in ExampleFormulas)
        {
            wrap |= new Button(example, () => onSelect(example))
                .Outline()
                .Small();
        }

        return layout | wrap;
    }
}
