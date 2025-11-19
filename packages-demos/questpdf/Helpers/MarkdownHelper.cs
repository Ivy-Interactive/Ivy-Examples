namespace QuestPdfExample.Helpers;

public static class MarkdownHelper
{
    public static bool IsHeading(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("#") && !trimmed.StartsWith("###");
    }
    
    public static bool IsHeading2(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("##") && !trimmed.StartsWith("###");
    }
    
    public static bool IsHeading3(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("###");
    }
    
    public static bool IsCodeBlock(string line)
    {
        return line.Trim().StartsWith("```");
    }
    
    public static bool IsTableRow(string line)
    {
        return line.TrimStart().StartsWith("|") && line.Contains('|');
    }
    
    public static bool IsBulletList(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- ") && !IsCheckbox(line);
    }
    
    public static bool IsNumberedList(string line)
    {
        var trimmed = line.TrimStart();
        return (trimmed.StartsWith("1. ") || 
                trimmed.StartsWith("I. ") || 
                trimmed.StartsWith("a. ")) && !IsCheckbox(line);
    }
    
    public static bool IsQuote(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith(">");
    }
    
    public static bool IsCheckbox(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- [") && trimmed.Contains("]");
    }
    
    public static int CountIndent(string line)
    {
        int total = 0;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == ' ') total += 1;
            else if (c == '\t') total += 2;
            else break;
        }
        return total;
    }
    
    public static bool IsSepCell(string cell)
    {
        cell = cell.Trim();
        if (cell.Length == 0) return false;
        foreach (var ch in cell)
            if (ch != '-' && ch != ':' && ch != ' ') return false;
        return true;
    }
    
    public static (string text, bool isBold, bool isItalic, bool isLink) ParseInlineFormatting(string text)
    {
        bool isBold = text.Contains("**") && text.IndexOf("**") != text.LastIndexOf("**");
        bool isItalic = text.Contains("*") && !text.Contains("**");
        bool isLink = text.Contains("[") && text.Contains("]") && text.Contains("(") && text.Contains(")");
        
        return (text, isBold, isItalic, isLink);
    }
    
    public static string ExtractLinkText(string text)
    {
        var start = text.IndexOf('[');
        var end = text.IndexOf(']');
        if (start >= 0 && end > start)
            return text.Substring(start + 1, end - start - 1);
        return text;
    }
    
    public static string ExtractLinkUrl(string text)
    {
        var start = text.IndexOf('(');
        var end = text.LastIndexOf(')');
        if (start >= 0 && end > start)
            return text.Substring(start + 1, end - start - 1);
        return text;
    }
}
