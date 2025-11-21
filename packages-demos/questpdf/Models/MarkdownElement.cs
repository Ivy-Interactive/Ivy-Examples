namespace QuestPdfExample.Models;

public abstract class MarkdownElement
{
    public string Content { get; set; } = string.Empty;
    public int IndentLevel { get; set; } = 0;
}

public class HeadingElement : MarkdownElement
{
    public int Level { get; set; } // 1, 2, 3
}

public class ParagraphElement : MarkdownElement
{
}

public class BulletListElement : MarkdownElement
{
    public List<string> Items { get; set; } = new();
}

public class NumberedListElement : MarkdownElement
{
    public List<string> Items { get; set; } = new();
    public string NumberingType { get; set; } = "1."; // "1.", "I.", "a."
}

public class QuoteElement : MarkdownElement
{
    public List<string> Lines { get; set; } = new();
}

public class CodeBlockElement : MarkdownElement
{
    public List<string> Lines { get; set; } = new();
    public string Language { get; set; } = string.Empty;
}

public class TableElement : MarkdownElement
{
    public List<string[]> Rows { get; set; } = new();
    public string[] Headers { get; set; } = Array.Empty<string>();
    public int DataStartRow { get; set; } = 1;
}

public class CheckboxElement : MarkdownElement
{
    public bool IsChecked { get; set; }
    public string Text { get; set; } = string.Empty;
}
