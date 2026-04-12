namespace Excel.Formula.Explainer.Apps;

public record FormulaResult(string Summary, List<FormulaStep> Steps, List<FunctionInfo> FunctionsUsed);
public record FormulaStep(string FunctionName, string Expression, string Explanation);
public record FunctionInfo(string Name, string Description);

public static class FormulaParser
{
    private static readonly Dictionary<string, string> FunctionDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VLOOKUP"] = "Looks up a value in the first column of a range and returns a value from another column.",
        ["HLOOKUP"] = "Looks up a value in the first row of a range and returns a value from another row.",
        ["XLOOKUP"] = "Searches a range for a match and returns the corresponding item from a second range.",
        ["INDEX"] = "Returns the value at a given row and column in a range.",
        ["MATCH"] = "Returns the position of a value in a range.",
        ["IF"] = "Returns one value if a condition is true and another if false.",
        ["IFS"] = "Checks multiple conditions and returns the first matching result.",
        ["IFERROR"] = "Returns a custom value if an expression results in an error, otherwise returns the expression result.",
        ["IFNA"] = "Returns a custom value if a formula results in #N/A, otherwise returns the formula result.",
        ["SUM"] = "Adds up a range of numbers.",
        ["SUMIF"] = "Sums values that meet a single condition.",
        ["SUMIFS"] = "Sums values that meet multiple conditions.",
        ["SUMPRODUCT"] = "Multiplies corresponding elements in arrays and returns the sum of the products.",
        ["COUNT"] = "Counts the number of cells containing numbers.",
        ["COUNTA"] = "Counts non-empty cells in a range.",
        ["COUNTIF"] = "Counts cells that meet a single condition.",
        ["COUNTIFS"] = "Counts cells that meet multiple conditions.",
        ["AVERAGE"] = "Returns the arithmetic mean of a set of numbers.",
        ["AVERAGEIF"] = "Averages values that meet a condition.",
        ["AVERAGEIFS"] = "Averages values that meet multiple conditions.",
        ["MIN"] = "Returns the smallest value in a range.",
        ["MAX"] = "Returns the largest value in a range.",
        ["ROUND"] = "Rounds a number to a specified number of digits.",
        ["ROUNDUP"] = "Rounds a number up, away from zero.",
        ["ROUNDDOWN"] = "Rounds a number down, toward zero.",
        ["ABS"] = "Returns the absolute value of a number.",
        ["LEFT"] = "Returns the leftmost characters from a text string.",
        ["RIGHT"] = "Returns the rightmost characters from a text string.",
        ["MID"] = "Extracts characters from the middle of a text string.",
        ["LEN"] = "Returns the number of characters in a text string.",
        ["TRIM"] = "Removes extra spaces from text.",
        ["UPPER"] = "Converts text to uppercase.",
        ["LOWER"] = "Converts text to lowercase.",
        ["PROPER"] = "Capitalizes the first letter of each word.",
        ["CONCATENATE"] = "Joins multiple text strings into one.",
        ["CONCAT"] = "Joins multiple text strings or ranges into one.",
        ["TEXTJOIN"] = "Joins text with a delimiter, optionally ignoring empty values.",
        ["TEXT"] = "Formats a number as text using a format string.",
        ["VALUE"] = "Converts a text string that looks like a number into a number.",
        ["FIND"] = "Finds one text string within another (case-sensitive).",
        ["SEARCH"] = "Finds one text string within another (case-insensitive).",
        ["SUBSTITUTE"] = "Replaces occurrences of a text string with new text.",
        ["REPLACE"] = "Replaces part of a text string with different text by position.",
        ["AND"] = "Returns TRUE if all conditions are true.",
        ["OR"] = "Returns TRUE if any condition is true.",
        ["NOT"] = "Reverses a logical value.",
        ["TODAY"] = "Returns the current date.",
        ["NOW"] = "Returns the current date and time.",
        ["DATE"] = "Creates a date from year, month, and day components.",
        ["YEAR"] = "Extracts the year from a date.",
        ["MONTH"] = "Extracts the month from a date.",
        ["DAY"] = "Extracts the day from a date.",
        ["UNIQUE"] = "Returns a list of unique values from a range.",
        ["SORT"] = "Sorts the contents of a range.",
        ["FILTER"] = "Filters a range of data based on criteria.",
        ["CHOOSE"] = "Picks a value from a list based on an index number.",
        ["OFFSET"] = "Returns a reference offset from a starting cell.",
        ["INDIRECT"] = "Returns a reference specified by a text string.",
    };

    public static FormulaResult Parse(string formula)
    {
        var trimmed = formula.Trim();
        if (trimmed.StartsWith('='))
            trimmed = trimmed[1..];

        var steps = new List<FormulaStep>();
        var functionsUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ParseExpression(trimmed, steps, functionsUsed);

        var functionInfos = functionsUsed
            .OrderBy(f => f)
            .Select(f => new FunctionInfo(f.ToUpperInvariant(), GetDescription(f)))
            .ToList();

        var summary = BuildSummary(trimmed, functionsUsed);

        return new FormulaResult(summary, steps, functionInfos);
    }

    private static void ParseExpression(string expr, List<FormulaStep> steps, HashSet<string> functionsUsed)
    {
        var pos = 0;
        while (pos < expr.Length)
        {

            var funcStart = FindNextFunction(expr, pos);
            if (funcStart < 0) break;

            var funcName = ExtractFunctionName(expr, funcStart);
            if (string.IsNullOrEmpty(funcName))
            {
                pos = funcStart + 1;
                continue;
            }

            var parenStart = funcStart + funcName.Length;
            if (parenStart >= expr.Length || expr[parenStart] != '(')
            {
                pos = parenStart;
                continue;
            }

            var parenEnd = FindMatchingParen(expr, parenStart);
            if (parenEnd < 0)
            {
                pos = parenStart + 1;
                continue;
            }

            var fullExpr = expr[funcStart..(parenEnd + 1)];
            var innerArgs = expr[(parenStart + 1)..parenEnd];

            functionsUsed.Add(funcName);


            ParseExpression(innerArgs, steps, functionsUsed);

            var explanation = ExplainFunctionCall(funcName, innerArgs);
            steps.Add(new FormulaStep(funcName.ToUpperInvariant(), fullExpr, explanation));

            pos = parenEnd + 1;
        }
    }

    private static int FindNextFunction(string expr, int startPos)
    {
        for (var i = startPos; i < expr.Length; i++)
        {
            if (char.IsLetter(expr[i]) || expr[i] == '_')
            {
                var end = i;
                while (end < expr.Length && (char.IsLetterOrDigit(expr[end]) || expr[end] == '_' || expr[end] == '.'))
                    end++;

                if (end < expr.Length && expr[end] == '(')
                {
                    var name = expr[i..end];

                    if (!name.Contains('!'))
                        return i;
                }
                i = end;
            }
        }
        return -1;
    }

    private static string ExtractFunctionName(string expr, int start)
    {
        var end = start;
        while (end < expr.Length && (char.IsLetterOrDigit(expr[end]) || expr[end] == '_' || expr[end] == '.'))
            end++;
        return expr[start..end];
    }

    private static int FindMatchingParen(string expr, int openPos)
    {
        var depth = 0;
        var inString = false;
        for (var i = openPos; i < expr.Length; i++)
        {
            var c = expr[i];
            if (c == '"') inString = !inString;
            if (inString) continue;
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static string ExplainFunctionCall(string funcName, string args)
    {
        var upper = funcName.ToUpperInvariant();
        var argList = SplitTopLevelArgs(args);
        var desc = GetDescription(funcName);

        return upper switch
        {
            "VLOOKUP" => argList.Count >= 3
                ? $"Look up the value {Trim(argList[0])} in the range {Trim(argList[1])}, and return the value from column {Trim(argList[2])}{(argList.Count > 3 && Trim(argList[3]).Equals("FALSE", StringComparison.OrdinalIgnoreCase) ? " (exact match)" : "")}."
                : desc,
            "XLOOKUP" => argList.Count >= 3
                ? $"Search for {Trim(argList[0])} in {Trim(argList[1])}, and return the matching value from {Trim(argList[2])}{(argList.Count > 3 ? $", defaulting to {Trim(argList[3])} if not found" : "")}."
                : desc,
            "IF" => argList.Count >= 2
                ? $"If {Trim(argList[0])} is true, return {Trim(argList[1])}{(argList.Count > 2 ? $"; otherwise return {Trim(argList[2])}" : "")}."
                : desc,
            "IFERROR" => argList.Count >= 2
                ? $"Try to evaluate {Trim(argList[0])}; if it results in an error, return {Trim(argList[1])} instead."
                : desc,
            "INDEX" => argList.Count >= 2
                ? $"Return the value from {Trim(argList[0])} at the position given by {Trim(argList[1])}{(argList.Count > 2 ? $", column {Trim(argList[2])}" : "")}."
                : desc,
            "MATCH" => argList.Count >= 2
                ? $"Find the position of {Trim(argList[0])} within {Trim(argList[1])}{(argList.Count > 2 ? $" (match type: {Trim(argList[2])})" : "")}."
                : desc,
            "SUMIFS" => argList.Count >= 3
                ? $"Sum the values in {Trim(argList[0])} where {FormatCriteriaPairs(argList.Skip(1).ToList())}."
                : desc,
            "SUMIF" => argList.Count >= 2
                ? $"Sum values in {(argList.Count > 2 ? Trim(argList[2]) : Trim(argList[0]))} where {Trim(argList[0])} meets the criteria {Trim(argList[1])}."
                : desc,
            "COUNTIF" => argList.Count >= 2
                ? $"Count cells in {Trim(argList[0])} that match {Trim(argList[1])}."
                : desc,
            "COUNTIFS" => argList.Count >= 2
                ? $"Count cells where {FormatCriteriaPairs(argList)}."
                : desc,
            "SUMPRODUCT" => $"Multiply corresponding elements of the given arrays and sum the results: {Trim(args)}."
                ,
            "AVERAGE" => $"Calculate the average of {Trim(args)}.",
            "MIN" => $"Find the minimum value in {Trim(args)}.",
            "MAX" => $"Find the maximum value in {Trim(args)}.",
            "SUM" => $"Add up all values in {Trim(args)}.",
            "COUNT" => $"Count numeric values in {Trim(args)}.",
            "COUNTA" => $"Count non-empty cells in {Trim(args)}.",
            "CONCATENATE" or "CONCAT" => $"Join together: {Trim(args)}.",
            "LEN" => $"Get the length of {Trim(args)}.",
            "TRIM" => $"Remove extra spaces from {Trim(args)}.",
            "TEXT" => argList.Count >= 2
                ? $"Format {Trim(argList[0])} using the format \"{Trim(argList[1])}\"."
                : desc,
            "LEFT" => argList.Count >= 2
                ? $"Get the first {Trim(argList[1])} characters from {Trim(argList[0])}."
                : desc,
            "RIGHT" => argList.Count >= 2
                ? $"Get the last {Trim(argList[1])} characters from {Trim(argList[0])}."
                : desc,
            "AND" => $"Check that all of the following are true: {Trim(args)}.",
            "OR" => $"Check if any of the following are true: {Trim(args)}.",
            "NOT" => $"Reverse the logical value of {Trim(args)}.",
            "ROUND" => argList.Count >= 2
                ? $"Round {Trim(argList[0])} to {Trim(argList[1])} decimal places."
                : desc,
            "UNIQUE" => $"Extract unique values from {Trim(args)}.",
            "SORT" => $"Sort the values in {Trim(args)}.",
            "FILTER" => argList.Count >= 2
                ? $"Filter {Trim(argList[0])} where {Trim(argList[1])}."
                : desc,
            "TODAY" => "Get today's date.",
            "NOW" => "Get the current date and time.",
            _ => desc
        };
    }

    private static List<string> SplitTopLevelArgs(string args)
    {
        var result = new List<string>();
        var depth = 0;
        var inString = false;
        var start = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var c = args[i];
            if (c == '"') inString = !inString;
            if (inString) continue;
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(args[start..i]);
                start = i + 1;
            }
        }
        if (start < args.Length)
            result.Add(args[start..]);

        return result;
    }

    private static string FormatCriteriaPairs(List<string> args)
    {
        var parts = new List<string>();
        for (var i = 0; i + 1 < args.Count; i += 2)
            parts.Add($"{Trim(args[i])} matches {Trim(args[i + 1])}");
        return string.Join(" and ", parts);
    }

    private static string Trim(string s) => s.Trim();

    private static string GetDescription(string funcName)
    {
        return FunctionDescriptions.TryGetValue(funcName, out var desc)
            ? desc
            : $"Performs the {funcName.ToUpperInvariant()} operation.";
    }

    private static string BuildSummary(string formula, HashSet<string> functions)
    {
        if (functions.Count == 0)
            return "This is a simple expression or value reference.";

        var funcList = string.Join(", ", functions.Select(f => f.ToUpperInvariant()).OrderBy(f => f));
        var nested = functions.Count > 1 ? "nested " : "";
        return $"This formula uses {nested}{funcList} to compute a result. It {DescribeIntent(functions)}.";
    }

    private static string DescribeIntent(HashSet<string> functions)
    {
        var upper = new HashSet<string>(functions.Select(f => f.ToUpperInvariant()));

        if (upper.Contains("VLOOKUP") || upper.Contains("XLOOKUP") || upper.Contains("INDEX"))
            return "performs a lookup to retrieve specific data";
        if (upper.Contains("SUMIFS") || upper.Contains("SUMIF") || upper.Contains("SUMPRODUCT"))
            return "calculates a conditional sum";
        if (upper.Contains("COUNTIF") || upper.Contains("COUNTIFS"))
            return "counts values matching specific criteria";
        if (upper.Contains("IF") || upper.Contains("IFS"))
            return "applies conditional logic to determine the output";
        if (upper.Contains("AVERAGE") || upper.Contains("AVERAGEIF"))
            return "computes an average of selected values";
        return "processes the data through a series of calculations";
    }
}
