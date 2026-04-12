namespace BookLibrary.Apps.Views;

public class BookEditSheet(IState<bool> isOpen, RefreshToken refreshToken, Guid bookId) : ViewBase
{
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

    private static readonly Option<int?>[] RatingOptions =
    [
        new("— (no rating)", null),
        new("★☆☆☆☆  (1)",   1),
        new("★★☆☆☆  (2)",   2),
        new("★★★☆☆  (3)",   3),
        new("★★★★☆  (4)",   4),
        new("★★★★★  (5)",   5),
    ];

    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var bookState = UseState<Book?>(() => null);
        var prevStatus = UseState(BookStatus.WantToRead);

        UseEffect(async () =>
        {
            var bk = await BookStore.GetByIdAsync(volume, bookId);
            bookState.Set(bk);
            if (bk is not null) prevStatus.Set(bk.Status);
        }, [EffectTrigger.OnMount()]);

        if (bookState.Value is null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Book");

        return bookState
            .ToForm()
            .Builder(b => b!.Title, e => e.ToTextInput())
            .Builder(b => b!.Author, e => e.ToTextInput())
            .Builder(b => b!.Genre, e => e.ToSelectInput(GenreOptions))
            .Builder(b => b!.Status, e => e.ToSelectInput(StatusOptions))
            .Builder(b => b!.Rating, e => e.ToSelectInput(RatingOptions))
            .Builder(b => b!.TotalPages, e => e.ToNumberInput())
            .Builder(b => b!.PagesRead, e => e.ToNumberInput())
            .Builder(b => b!.Notes, e => e.ToTextareaInput())
            .Remove(b => b!.Id, b => b!.AddedAt, b => b!.FinishedAt)
            .OnSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Book");

        async Task OnSubmit(Book? book)
        {
            if (book is null) return;
            var justCompleted = book.Status == BookStatus.Completed && prevStatus.Value != BookStatus.Completed;
            await BookStore.UpdateAsync(volume, book with
            {
                FinishedAt = justCompleted ? DateTime.UtcNow : bookState.Value?.FinishedAt
            });
            refreshToken.Refresh();
        }
    }
}
