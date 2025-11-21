namespace QuestPdfExample.Services;

public class MarkdownParsingService
{
    public List<MarkdownElement> ParseMarkdown(string text)
    {
        var elements = new List<MarkdownElement>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            if (MarkdownHelper.IsHeading(line))
            {
                var level = MarkdownHelper.IsHeading3(line) ? 3 : 
                           MarkdownHelper.IsHeading2(line) ? 2 : 1;
                var content = line.TrimStart('#', ' ').Trim();
                elements.Add(new HeadingElement 
                { 
                    Level = level, 
                    Content = content,
                    IndentLevel = MarkdownHelper.CountIndent(line)
                });
            }
            else if (MarkdownHelper.IsCodeBlock(line))
            {
                var codeBlock = new CodeBlockElement();
                i++; // Skip opening ```
                while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                {
                    codeBlock.Lines.Add(lines[i]);
                    i++;
                }
                elements.Add(codeBlock);
            }
            else if (MarkdownHelper.IsTableRow(line))
            {
                var table = ParseTable(lines, i, out int consumed);
                if (table != null)
                {
                    elements.Add(table);
                    i += consumed - 1; // -1 because for loop will increment
                }
            }
            else if (MarkdownHelper.IsBulletList(line))
            {
                var list = ParseBulletList(lines, i, out int consumed);
                elements.Add(list);
                i += consumed - 1;
            }
            else if (MarkdownHelper.IsNumberedList(line))
            {
                var list = ParseNumberedList(lines, i, out int consumed);
                elements.Add(list);
                i += consumed - 1;
            }
            else if (MarkdownHelper.IsQuote(line))
            {
                var quote = ParseQuote(lines, i, out int consumed);
                elements.Add(quote);
                i += consumed - 1;
            }
            else if (MarkdownHelper.IsCheckbox(line))
            {
                var checkbox = ParseCheckbox(line);
                elements.Add(checkbox);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                elements.Add(new ParagraphElement { Content = line });
            }
        }
        
        return elements;
    }
    
    public void RenderToQuestPdf(IContainer container, MarkdownElement element)
    {
        switch (element)
        {
            case HeadingElement heading:
                RenderHeading(container, heading);
                break;
            case ParagraphElement paragraph:
                RenderParagraph(container, paragraph);
                break;
            case BulletListElement list:
                RenderBulletList(container, list);
                break;
            case NumberedListElement list:
                RenderNumberedList(container, list);
                break;
            case QuoteElement quote:
                RenderQuote(container, quote);
                break;
            case CodeBlockElement code:
                RenderCodeBlock(container, code);
                break;
            case TableElement table:
                RenderTable(container, table);
                break;
            case CheckboxElement checkbox:
                RenderCheckbox(container, checkbox);
                break;
        }
    }
    
    private void RenderHeading(IContainer container, HeadingElement heading)
    {
        container.Text(t =>
        {
            var fontSize = heading.Level switch
            {
                1 => 18,
                2 => 16,
                3 => 14,
                _ => 12
            };
            
            t.Span(heading.Content)
             .FontSize(fontSize)
             .SemiBold();
        });
    }
    
    private void RenderParagraph(IContainer container, ParagraphElement paragraph)
    {
        container.Text(t => RenderInline(t, paragraph.Content));
    }
    
    private void RenderBulletList(IContainer container, BulletListElement list)
    {
        container.Column(c =>
        {
            foreach (var item in list.Items)
            {
                c.Item().PaddingLeft(8).Text(t => RenderInline(t, item));
            }
        });
    }
    
    private void RenderNumberedList(IContainer container, NumberedListElement list)
    {
        container.Column(c =>
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                var number = list.NumberingType == "I." ? 
                    RomanNumerals(i + 1) : 
                    (i + 1).ToString();
                    
                c.Item().PaddingLeft(8).Text(t =>
                {
                    t.Span($"{number}. ").SemiBold();
                    RenderInline(t, list.Items[i]);
                });
            }
        });
    }
    
    private void RenderQuote(IContainer container, QuoteElement quote)
    {
        container.PaddingLeft(16).Text(t =>
        {
            foreach (var line in quote.Lines)
            {
                t.Span(line).Italic();
                t.Span("\n");
            }
        });
    }
    
    private void RenderCodeBlock(IContainer container, CodeBlockElement code)
    {
        container.PaddingLeft(8).Text(t =>
        {
            t.DefaultTextStyle(TextStyle.Default.FontFamily("Courier New"));
            foreach (var line in code.Lines)
            {
                t.Line(line);
            }
        });
    }
    
    private void RenderTable(IContainer container, TableElement table)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c => 
            {
                for (int i = 0; i < table.Headers.Length; i++)
                    c.RelativeColumn();
            });
            
            t.Header(h =>
            {
                foreach (var header in table.Headers)
                    h.Cell().Text(header).SemiBold();
            });
            
            for (int ri = table.DataStartRow; ri < table.Rows.Count; ri++)
            {
                var row = table.Rows[ri];
                for (int ci = 0; ci < table.Headers.Length; ci++)
                {
                    var val = ci < row.Length ? row[ci] : string.Empty;
                    t.Cell().Text(val);
                }
            }
        });
    }
    
    private void RenderCheckbox(IContainer container, CheckboxElement checkbox)
    {
        container.PaddingLeft(8).Text(t =>
        {
            t.Span(checkbox.IsChecked ? "☑" : "☐").FontSize(12);
            t.Span(" ");
            RenderInline(t, checkbox.Text);
        });
    }
    
    private void RenderInline(TextDescriptor t, string text)
    {
        // Simple inline formatting
        var parts = text.Split(new[] { "**", "*", "[", "]" }, StringSplitOptions.None);
        bool isBold = false, isItalic = false, isLink = false;
        
        foreach (var part in parts)
        {
            if (part == "**")
            {
                isBold = !isBold;
                continue;
            }
            if (part == "*")
            {
                isItalic = !isItalic;
                continue;
            }
            
            var span = t.Span(part);
            if (isBold) span.SemiBold();
            if (isItalic) span.Italic();
            if (isLink) span.Underline();
        }
    }
    
    private TableElement? ParseTable(string[] lines, int startIndex, out int consumed)
    {
        var rows = new List<string[]>();
        var k = startIndex;
        
        while (k < lines.Length && MarkdownHelper.IsTableRow(lines[k]))
        {
            var cells = lines[k].Trim();
            if (cells.StartsWith("|")) cells = cells[1..];
            if (cells.EndsWith("|")) cells = cells[..^1];
            var parts = cells.Split('|').Select(x => x.Trim()).ToArray();
            rows.Add(parts);
            k++;
        }
        
        consumed = k - startIndex;
        if (rows.Count == 0) return null;
        
        var headers = rows[0];
        int dataStart = 1;
        if (rows.Count > 1 && rows[1].All(MarkdownHelper.IsSepCell))
            dataStart = 2;
            
        return new TableElement
        {
            Headers = headers,
            Rows = rows,
            DataStartRow = dataStart
        };
    }
    
    private BulletListElement ParseBulletList(string[] lines, int startIndex, out int consumed)
    {
        var items = new List<string>();
        var k = startIndex;
        
        while (k < lines.Length && MarkdownHelper.IsBulletList(lines[k]))
        {
            var line = lines[k].TrimStart('-', ' ').Trim();
            items.Add(line);
            k++;
        }
        
        consumed = k - startIndex;
        return new BulletListElement { Items = items };
    }
    
    private NumberedListElement ParseNumberedList(string[] lines, int startIndex, out int consumed)
    {
        var items = new List<string>();
        var k = startIndex;
        string numberingType = "1.";
        
        while (k < lines.Length && MarkdownHelper.IsNumberedList(lines[k]))
        {
            var line = lines[k].TrimStart();
            if (k == startIndex)
            {
                if (line.StartsWith("I.")) numberingType = "I.";
                else if (line.StartsWith("a.")) numberingType = "a.";
            }
            
            var content = line.Substring(line.IndexOf(' ') + 1);
            items.Add(content);
            k++;
        }
        
        consumed = k - startIndex;
        return new NumberedListElement { Items = items, NumberingType = numberingType };
    }
    
    private QuoteElement ParseQuote(string[] lines, int startIndex, out int consumed)
    {
        var quoteLines = new List<string>();
        var k = startIndex;
        
        while (k < lines.Length && MarkdownHelper.IsQuote(lines[k]))
        {
            var line = lines[k].TrimStart('>', ' ').Trim();
            quoteLines.Add(line);
            k++;
        }
        
        consumed = k - startIndex;
        return new QuoteElement { Lines = quoteLines };
    }
    
    private CheckboxElement ParseCheckbox(string line)
    {
        var trimmed = line.TrimStart('-', ' ').Trim();
        var isChecked = trimmed.StartsWith("[x]") || trimmed.StartsWith("[X]");
        var text = trimmed.Substring(3).Trim();
        
        return new CheckboxElement
        {
            IsChecked = isChecked,
            Text = text
        };
    }
    
    private static string RomanNumerals(int number)
    {
        return number switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
            6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
            _ => number.ToString()
        };
    }
}
