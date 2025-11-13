namespace XLParserExample.Services;

internal class ParseTreeNodeInfo
{
    public int Depth { get; set; }
    public ParseTreeNode TreeNode { get; set; }

    public string NodeValue => TreeNode.Print();

    public List<NodeMetadata> NodeInfo =>    
        [
            new("Term", TreeNode.Term.ToString()) ,
            new("Is token", (TreeNode.Token is not null).ToString()) ,
            new("Found token", TreeNode.FindToken().ToString()) ,
            new("Found token location", TreeNode.FindToken().Location.ToString()) ,
            new("Is binary non-reference operation", TreeNode.IsBinaryNonReferenceOperation().ToString()) ,
            new("Is binary operation", TreeNode.IsBinaryOperation().ToString()) ,
            new("Is binary reference operation", TreeNode.IsBinaryReferenceOperation().ToString()) ,
            new("Is built-in function", TreeNode.IsBuiltinFunction().ToString()) ,
            new("Is external function", TreeNode.IsExternalUDFunction().ToString()) ,
            new("Is function", TreeNode.IsFunction().ToString()) ,
            new("Is intersection", TreeNode.IsIntersection().ToString()) ,
            new("Is named function", TreeNode.IsNamedFunction().ToString()) ,
            new("Is number with sign", TreeNode.IsNumberWithSign().ToString()) ,
            new("Is operation", TreeNode.IsOperation().ToString()) ,
            new("Is operator", TreeNode.IsOperator().ToString()) ,
            new("Is parentheses", TreeNode.IsParentheses().ToString()) ,
            new("Is range", TreeNode.IsRange().ToString()) ,
            new("Is unary operation", TreeNode.IsUnaryOperation().ToString()) ,
            new("Is unary postfix operation", TreeNode.IsUnaryPostfixOperation().ToString()) ,
            new("Is unary prefix operation", TreeNode.IsUnaryPrefixOperation().ToString()) ,
            new("Is union", TreeNode.IsUnion().ToString()) ,
        ];    
}

internal class NodeMetadata
{
    public string Key { get; private set; }
    public string Value { get; private set; }

    public NodeMetadata(string key, string value)
    {
        Key = key;
        Value = value;
    }
}