namespace AutodealerCrm.Apps.Views;

public class CustomerMessagesBlade(int customerId) : ViewBase
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
            messages.Set(await db.Messages.Include(e => e.Media).Where(e => e.CustomerId == customerId).ToArrayAsync());
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
        };
        
        if (messages.Value == null) return null;
        
        var table = messages.Value.Select(e => new
            {
                Content = e.Content,
                SentAt = e.SentAt,
                MediaFilePath = e.Media?.FilePath,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(e.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new CustomerMessagesEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns()
        ;

        var addBtn = new Button("Add Message").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CustomerMessagesCreateDialog(isOpen, refreshToken, customerId));

        return new Fragment()
               | BladeHelper.WithHeader(addBtn, table)
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int messageId)
    {
        using var db2 = factory.CreateDbContext();
        db2.Messages.Remove(db2.Messages.Single(e => e.Id == messageId));
        db2.SaveChanges();
    }
}