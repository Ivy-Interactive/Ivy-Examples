namespace AutodealerCrm.Apps.Views;

public class UserLeadsBlade(int? managerId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var leads = this.UseState<Lead[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            leads.Set(await db.Leads
                .Include(e => e.Customer)
                .Include(e => e.LeadIntent)
                .Include(e => e.LeadStage)
                .Include(e => e.SourceChannel)
                .Where(e => managerId == null || e.ManagerId == managerId)
                .ToArrayAsync());
        }, [ EffectTrigger.OnMount(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this lead?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Lead", AlertButtonSet.OkCancel);
            };
        }

        if (leads.Value == null) return null;

        var table = leads.Value.Select(e => new
            {
                Customer = $"{e.Customer.FirstName} {e.Customer.LastName}",
                Intent = e.LeadIntent.DescriptionText,
                Stage = e.LeadStage.DescriptionText,
                Source = e.SourceChannel.DescriptionText,
                Priority = e.Priority,
                Notes = e.Notes,
                CreatedAt = e.CreatedAt.ToString("yyyy-MM-dd"),
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(e.Id)))
                    | Icons.Pencil
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new UserLeadsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Lead").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new UserLeadsCreateDialog(isOpen, refreshToken, managerId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int leadId)
    {
        using var db2 = factory.CreateDbContext();
        db2.Leads.Remove(db2.Leads.Single(e => e.Id == leadId));
        db2.SaveChanges();
    }
}