namespace BookLibrary.Apps.Views;

public class BookListBlade : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeContext>();
        var refreshToken = UseRefreshToken();
        var refreshKey = UseState(0);

        UseEffect(() =>
        {
            refreshKey.Set(refreshKey.Value + 1);
        }, [refreshToken]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var book = (Book)e.Sender.Tag!;
            blades.Push(this, new BookDetailsBlade(book.Id, refreshToken), book.Title);
        });

        ListItem CreateItem(Book book)
        {
            var subtitle = $"{book.Author}  ·  {BookStore.StatusLabel(book.Status)}";
            Icons icon = book.Status switch
            {
                BookStatus.Reading => Icons.BookMarked,
                BookStatus.Completed => Icons.BookCheck,
                BookStatus.Paused => Icons.BookDashed,
                _ => Icons.BookOpen
            };
            return new ListItem(title: book.Title, subtitle: subtitle, onClick: onItemClick, tag: book, icon: icon);
        }

        var createBtn = Icons.Plus.ToButton(_ => blades.Pop(this))
            .Ghost()
            .Tooltip("Add Book")
            .ToTrigger(isOpen => new BookCreateDialog(isOpen, refreshToken));

        return new Fragment { Key = $"book-list-{refreshKey.Value}" }
               | new FilteredListView<Book>(
                   fetchRecords: async filter =>
                   {
                       var all = await BookStore.GetAllAsync(volume);
                       var sorted = all.OrderByDescending(b => b.AddedAt).ToArray();
                       if (string.IsNullOrWhiteSpace(filter)) return sorted;
                       filter = filter.Trim();
                       return sorted.Where(b =>
                           b.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           b.Author.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           b.Genre.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                   },
                   createItem: CreateItem,
                   toolButtons: createBtn,
                   onFilterChanged: _ => blades.Pop(this));
    }
}
