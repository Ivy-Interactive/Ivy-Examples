namespace AutodealerCrm.Apps.Views;

public class LeadMediaBlade(int? leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var media = this.UseState<Medium[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            media.Set(await db.Media.Include(e => e.Lead).Where(e => e.LeadId == leadId).ToArrayAsync());
        }, [ EffectTrigger.OnMount(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this media?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Media", AlertButtonSet.OkCancel);
            };
        }

        if (media.Value == null) return null;

        var table = media.Value.Select(e => new
            {
                FilePath = e.FilePath,
                FileType = e.FileType,
                UploadedAt = e.UploadedAt,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(e.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new LeadMediaEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Media").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new LeadMediaCreateDialog(isOpen, refreshToken, leadId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int mediaId)
    {
        using var db2 = factory.CreateDbContext();
        db2.Media.Remove(db2.Media.Single(e => e.Id == mediaId));
        db2.SaveChanges();
    }
}