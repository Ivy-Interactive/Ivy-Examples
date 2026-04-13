namespace BookLibrary.Apps.Views;

public class BookDetailsBlade(Guid bookId, RefreshToken token) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeContext>();
        var book = UseState<Book?>(() => null);
        var progressState = UseState(0);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var bk = await BookStore.GetByIdAsync(volume, bookId);
            book.Set(bk);
            if (bk?.TotalPages > 0 && bk.PagesRead.HasValue)
                progressState.Set((int)Math.Round((double)bk.PagesRead.Value / bk.TotalPages.Value * 100));
        }, [EffectTrigger.OnMount(), token]);

        if (book.Value is null)
            return new Callout("Loading…").Variant(CalloutVariant.Info);

        var b = book.Value;

        var pagesText = (b.PagesRead.HasValue && b.TotalPages.HasValue)
            ? $"{b.PagesRead} / {b.TotalPages}"
            : b.TotalPages.HasValue ? $"— / {b.TotalPages}" : "—";

        var showProgress = b.TotalPages > 0 && b.PagesRead.HasValue;

        var onDelete = new Action(() =>
        {
            showAlert($"Delete \"{b.Title}\"?", result =>
            {
                if (!result.IsOk()) return;
                BookStore.DeleteAsync(volume, bookId).Wait();
                token.Refresh();
                blades.Pop(refresh: true);
            }, "Delete Book", AlertButtonSet.OkCancel);
        });

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger(isOpen => new BookEditSheet(isOpen, token, bookId));

        var deleteBtn = new Button("Delete")
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .Width(Size.Grow())
            .OnClick(onDelete);

        var detailsCard = new Card(
            content: Layout.Vertical().Gap(4)
                | new
                {
                    Author = b.Author,
                    Genre = b.Genre,
                    Status = BookStore.StatusLabel(b.Status),
                    Rating = BookStore.RatingStars(b.Rating),
                    Pages = pagesText,
                    Added = b.AddedAt.ToString("MMM d, yyyy"),
                    Finished = b.FinishedAt.HasValue ? b.FinishedAt.Value.ToString("MMM d, yyyy") : null,
                    Notes = b.Notes
                }
                .ToDetails()
                .RemoveEmpty()
                | (showProgress
                    ? (object)(Layout.Vertical().Gap(1)
                        | Text.Muted("Reading progress")
                        | new Progress(progressState)
                        | Text.Muted(pagesText + " pages"))
                    : new Empty()),
            footer: Layout.Horizontal().Gap(2).AlignContent(Align.Right)
                | deleteBtn
                | editBtn
        ).Title(b.Title).Width(Size.Units(110));

        return new Fragment()
               | new BladeHeader(Text.H4(b.Title))
               | (Layout.Vertical() | detailsCard)
               | alertView!;
    }
}
