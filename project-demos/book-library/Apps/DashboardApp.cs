namespace BookLibrary.Apps;

[App(icon: Icons.ChartBar, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var volume       = UseService<IVolume>();
        var books        = UseState<List<Book>>(() => []);
        var refreshToken = UseRefreshToken();

        UseEffect(async () =>
        {
            books.Set(await BookStore.GetAllAsync(volume));
        }, [EffectTrigger.OnMount(), refreshToken]);

        UseEffect(() =>
        {
            void OnChanged() => refreshToken.Refresh();
            BookStore.DataChanged += OnChanged;
        }, [EffectTrigger.OnMount()]);

        var all               = books.Value;
        var total             = all.Count;
        var reading           = all.Count(b => b.Status == BookStatus.Reading);
        var completed         = all.Count(b => b.Status == BookStatus.Completed);
        var wantToRead        = all.Count(b => b.Status == BookStatus.WantToRead);
        var paused            = all.Count(b => b.Status == BookStatus.Paused);
        var completedThisYear = all.Count(b => b.Status == BookStatus.Completed && b.FinishedAt?.Year == DateTime.UtcNow.Year);
        var avgRating         = all.Where(b => b.Rating.HasValue).Select(b => (double)b.Rating!.Value).DefaultIfEmpty(0).Average();

        // Books by genre — pie chart
        var genreData  = all.GroupBy(b => b.Genre)
                            .Select(g => new { Genre = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count)
                            .ToList();

        var genreChart = genreData.Count > 0
            ? (object)genreData.ToPieChart(
                dimension: x => x.Genre,
                measure:   x => x.Sum(f => f.Count),
                PieChartStyles.Dashboard,
                new PieChartTotal(total.ToString(), "Books"))
            : Text.Muted("No data yet");

        // Library overview — bar chart
        var statusData  = all.GroupBy(b => BookStore.StatusLabel(b.Status))
                             .Select(g => new { Status = g.Key, Count = g.Count() })
                             .ToList();
        var statusChart = statusData.ToBarChart()
                                    .Dimension("Status", x => x.Status)
                                    .Measure("Books",    x => x.Sum(f => f.Count));

        // Currently reading — progress cards
        var readingBooks = all.Where(b => b.Status == BookStatus.Reading).ToList();
        object readingSection = readingBooks.Count == 0
            ? new Callout("No books in progress right now. Add one in My Books!").Variant(CalloutVariant.Info)
            : (object)(Layout.Grid().Columns(2)
                | readingBooks.Select(b =>
                    // Key includes PagesRead so the card remounts (and resets its state) when progress changes
                    (object)new ReadingBookCard(b).Key(b.Id, b.PagesRead)));

        return Layout.Horizontal().AlignContent(Align.TopCenter)
            | (Layout.Vertical().Gap(6).Padding(6).Width(Size.Full().Max(320))
                | Text.H1("My Book Library")
                | (Layout.Grid().Columns(4)
                    | new Card(
                        Layout.Vertical().Gap(1)
                            | Text.H2(total.ToString())
                            | Text.Muted($"{completedThisYear} finished this year")
                    ).Title("Total Books").Icon(Icons.BookOpen)
                    | new Card(
                        Layout.Vertical().Gap(1)
                            | Text.H2(reading.ToString())
                            | Text.Muted($"{paused} paused")
                    ).Title("Reading Now").Icon(Icons.BookMarked)
                    | new Card(
                        Layout.Vertical().Gap(1)
                            | Text.H2(completed.ToString())
                            | Text.Muted($"Avg rating: {(avgRating > 0 ? avgRating.ToString("F1") + " ★" : "—")}")
                    ).Title("Completed").Icon(Icons.Check)
                    | new Card(
                        Layout.Vertical().Gap(1)
                            | Text.H2(wantToRead.ToString())
                            | Text.Muted("on the reading list")
                    ).Title("Want to Read").Icon(Icons.Bookmark))
                | (Layout.Grid().Columns(2)
                    | new Card(genreChart).Title("Books by Genre")
                    | new Card(statusChart).Title("Library Overview"))
                | Text.H2("Currently Reading")
                | readingSection);
    }
}

public class ReadingBookCard(Book book) : ViewBase
{
    public override object? Build()
    {
        var progressState = UseState(() =>
            book.TotalPages > 0 && book.PagesRead.HasValue
                ? (int)Math.Round((double)book.PagesRead.Value / book.TotalPages.Value * 100)
                : 0);

        var pagesText = book.PagesRead.HasValue && book.TotalPages.HasValue
            ? $"{book.PagesRead} / {book.TotalPages} pages"
            : "Pages not tracked";

        return new Card(
            Layout.Vertical().Gap(2)
                | Text.H4(book.Title)
                | Text.Muted(book.Author)
                | new Progress(progressState)
                | Text.Muted(pagesText)
        );
    }
}
