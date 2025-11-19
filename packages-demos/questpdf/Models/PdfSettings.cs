namespace QuestPdfExample.Models;

public class PdfSettings
{
    public string PageSize { get; set; } = "A4";
    public bool Landscape { get; set; } = false;
    public int Margins { get; set; } = 30;
    
    public QuestPDF.Helpers.PageSize GetPageSize()
    {
        var size = PageSize == "Letter" 
            ? QuestPDF.Helpers.PageSizes.Letter 
            : QuestPDF.Helpers.PageSizes.A4;
            
        if (Landscape)
        {
            size = new QuestPDF.Helpers.PageSize(size.Height, size.Width);
        }
        
        return size;
    }
}
