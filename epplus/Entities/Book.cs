namespace EPPlusExample.Entities;

[AttributeUsage(AttributeTargets.All)]
public class Column : System.Attribute
{
    public int ColumnIndex { get; set; }

    public Column(int column)
    {
        ColumnIndex = column;
    }
}
public class Book
{
    [@Column(1)]
    [Required]
    public string ID { get; set; }

    [@Column(2)]
    [Required]
    public string Title { get; set; }
    [@Column(3)]
    [Required]
    public string Author { get; set; }
    [@Column(4)]
    [Required]
    public int Year { get; set; }

    public Book(string title, string author, int year)
    {
        Title = title;
        Author = author;
        Year = year;
    }

    public Book()
    {

    }
};
