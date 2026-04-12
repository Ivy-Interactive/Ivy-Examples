namespace BookLibrary.Apps.Views;

public class BookCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record BookCreateRequest
    {
        [Required] public string Title { get; init; } = "";
        [Required] public string Author { get; init; } = "";
        public string Genre { get; init; } = "Fiction";
        public BookStatus Status { get; init; } = BookStatus.WantToRead;
        public int? TotalPages { get; init; }
        public string? Notes { get; init; }
    }

    private static readonly Option<string>[] GenreOptions =
    [
        new("Fiction",     "Fiction"),
        new("Non-Fiction", "Non-Fiction"),
        new("Fantasy",     "Fantasy"),
        new("Sci-Fi",      "Sci-Fi"),
        new("Mystery",     "Mystery"),
        new("Biography",   "Biography"),
        new("Self-Help",   "Self-Help"),
        new("History",     "History"),
        new("Technology",  "Technology"),
        new("Other",       "Other"),
    ];

    private static readonly Option<BookStatus>[] StatusOptions =
    [
        new("Want to Read", BookStatus.WantToRead),
        new("Reading",      BookStatus.Reading),
        new("Completed",    BookStatus.Completed),
        new("Paused",       BookStatus.Paused),
    ];

    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var form = UseState(() => new BookCreateRequest());

        return form
            .ToForm()
            .Builder(f => f.Title, e => e.ToTextInput().Placeholder("e.g. The Great Gatsby"))
            .Builder(f => f.Author, e => e.ToTextInput().Placeholder("e.g. F. Scott Fitzgerald"))
            .Builder(f => f.Genre, e => e.ToSelectInput(GenreOptions))
            .Builder(f => f.Status, e => e.ToSelectInput(StatusOptions))
            .Builder(f => f.TotalPages, e => e.ToNumberInput().Placeholder("Total pages (optional)"))
            .Builder(f => f.Notes, e => e.ToTextareaInput().Placeholder("Your thoughts…"))
            .OnSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Add Book", submitTitle: "Add");

        async Task OnSubmit(BookCreateRequest req)
        {
            await BookStore.AddAsync(volume, new Book
            {
                Id = Guid.NewGuid(),
                Title = req.Title.Trim(),
                Author = req.Author.Trim(),
                Genre = req.Genre,
                Status = req.Status,
                TotalPages = req.TotalPages,
                Notes = req.Notes?.Trim(),
                AddedAt = DateTime.UtcNow,
                FinishedAt = req.Status == BookStatus.Completed ? DateTime.UtcNow : null
            });
            refreshToken.Refresh();
        }
    }
}
