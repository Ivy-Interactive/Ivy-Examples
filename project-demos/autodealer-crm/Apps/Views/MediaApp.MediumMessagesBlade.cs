namespace AutodealerCrm.Apps.Views;

public class MediumMessagesBlade(int? mediaId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var messages = this.UseState<Message[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            messages.Set(await db.Messages
                .Include(m => m.MessageChannel)
                .Include(m => m.MessageDirection)
                .Include(m => m.MessageType)
                .Where(m => m.MediaId == mediaId)
                .ToArrayAsync());
        }, [ EffectTrigger.AfterInit(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this message?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Message", AlertButtonSet.OkCancel);
            };
        }

        if (messages.Value == null) return null;

        var table = messages.Value.Select(m => new
            {
                Channel = m.MessageChannel.DescriptionText,
                Direction = m.MessageDirection.DescriptionText,
                Type = m.MessageType.DescriptionText,
                Content = m.Content,
                SentAt = m.SentAt,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(m.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new MediumMessagesEditSheet(isOpen, refreshToken, m.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Message").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new MediumMessagesCreateDialog(isOpen, refreshToken, mediaId));

        return new Fragment()
               | BladeHelper.WithHeader(addBtn, table)
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int messageId)
    {
        using var db = factory.CreateDbContext();
        db.Messages.Remove(db.Messages.Single(m => m.Id == messageId));
        db.SaveChanges();
    }
}