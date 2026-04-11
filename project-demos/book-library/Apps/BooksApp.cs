using BookLibrary.Apps.Views;

namespace BookLibrary.Apps;

[App(icon: Icons.BookOpen, title: "My Books")]
public class BooksApp : ViewBase
{
    public override object? Build() => this.UseBlades(() => new BookListBlade(), "My Books");
}
