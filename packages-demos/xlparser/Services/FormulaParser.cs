namespace XLParserExample.Services;

internal class FormulaParser
{
    public static List<ParseTreeNodeInfo> ParseFormula(string formula)
    {
        var parseTreeNodeRoot = ExcelFormulaParser.Parse(formula);

        var nodes = new List<ParseTreeNodeInfo>();
        TraverseNode(parseTreeNodeRoot, 0, nodes);

        return nodes.GroupBy(x => $"{x.TreeNode.Print()} {x.TreeNode.FindToken().Location}").Select(x => x.Last()).ToList();
    }

    private static void TraverseNode(ParseTreeNode node, int depth, List<ParseTreeNodeInfo> nodes)
    {
        if (node == null) return;
        nodes.Add(new ParseTreeNodeInfo
        {
            Depth = depth,
            TreeNode = node
        });
        foreach (var child in node.ChildNodes)
        {
            TraverseNode(child, depth + 1, nodes);
        }
    }
}