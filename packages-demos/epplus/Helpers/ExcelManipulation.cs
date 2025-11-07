using Ivy.Core.Hooks;

namespace EPPlusExample.Helpers;

public static class ExcelManipulation
{
    //using the EPPlus library to generate an Excel file
    public static void WriteExcel(IState<List<Book>> booksState)
    {
        Book[] books = booksState.Value.ToArray();
        if (books is null || books.Count() == 0)
        {
            books = new[]
     {
            new Book("The Great Gatsby", "F. Scott Fitzgerald", 1925),
            new Book("To Kill a Mockingbird", "Harper Lee", 1960),
            new Book("1984", "George Orwell", 1949),
            new Book("Pride and Prejudice", "Jane Austen", 1813),
            new Book("The Catcher in the Rye", "J.D. Salinger", 1951)
        };
        }
        //Creating an instance of ExcelPackage
        ExcelPackage excel = new ExcelPackage();

        //name of the sheet
        var worksheet = excel.Workbook.Worksheets.Add("Books");

        //setting the properties of the sheet
        worksheet.TabColor = System.Drawing.Color.Black;
        worksheet.DefaultRowHeight = 12;

        // Setting the properties of the first row
        worksheet.Row(1).Height = 20;
        worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Row(1).Style.Font.Bold = true;

        // Header of the Excel sheet
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Title";
        worksheet.Cells[1, 3].Value = "Author";
        worksheet.Cells[1, 4].Value = "Year";

        // Inserting the article data into excel sheet by using the for each loop
        // As we have values to the first row we will start with second row
        int recordIndex = 2;

        foreach (var article in books)
        {
            worksheet.Cells[recordIndex, 1].Value = (recordIndex - 1).ToString();
            worksheet.Cells[recordIndex, 2].Value = article.Title;
            worksheet.Cells[recordIndex, 3].Value = article.Author;
            worksheet.Cells[recordIndex, 4].Value = article.Year;
            recordIndex++;
        }

        // By default, the column width is not set to auto fit for the content of the range, so we are using AutoFit() method here. 
        worksheet.Column(1).AutoFit();
        worksheet.Column(2).AutoFit();
        worksheet.Column(3).AutoFit();
        worksheet.Column(4).AutoFit();

        // file name with .xlsx extension 
        string p_strPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "books.xlsx");

        if (File.Exists(p_strPath))
            File.Delete(p_strPath);

        // Create excel file on physical disk 
        FileStream objFileStrm = File.Create(p_strPath);
        objFileStrm.Close();

        // Write content to excel file 
        File.WriteAllBytes(p_strPath, excel.GetAsByteArray());
        //Close Excel package
        excel.Dispose();
        // refresh reactive books list
        booksState.Value = ExcelManipulation.ReadExcel();
    }

    //using the EPPlus library to read an Excel file
    public static List<Book> ReadExcel()
    {
        string _fileName = "books.xlsx";
        string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), _fileName);
        var RESOURCES_WORKSHEET = 0;
        //Creating an instance of ExcelPackage
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
        {
            ExcelPackage excel = new ExcelPackage(fileStream);
            var workSheet = excel.Workbook.Worksheets[RESOURCES_WORKSHEET];

            IEnumerable<Book> newcollection = workSheet.ConvertSheetToObjects<Book>();
            return newcollection.ToList();
        }
    }


}
