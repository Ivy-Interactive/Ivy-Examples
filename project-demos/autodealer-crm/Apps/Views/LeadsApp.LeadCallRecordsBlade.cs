namespace AutodealerCrm.Apps.Views;

public class LeadCallRecordsBlade(int? leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var callRecords = this.UseState<CallRecord[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            callRecords.Set(await db.CallRecords
                .Include(e => e.Customer)
                .Include(e => e.CallDirection)
                .Where(e => e.LeadId == leadId)
                .ToArrayAsync());
        }, [ EffectTrigger.OnMount(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this call record?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Call Record", AlertButtonSet.OkCancel);
            };
        }

        if (callRecords.Value == null) return null;

        var table = callRecords.Value.Select(e => new
            {
                Customer = $"{e.Customer.FirstName} {e.Customer.LastName}",
                Direction = e.CallDirection.DescriptionText,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
                Recording = e.RecordingUrl,
                Sentiment = e.Sentiment,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(e.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new LeadCallRecordsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Call Record").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new LeadCallRecordsCreateDialog(isOpen, refreshToken, leadId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int callRecordId)
    {
        using var db2 = factory.CreateDbContext();
        db2.CallRecords.Remove(db2.CallRecords.Single(e => e.Id == callRecordId));
        db2.SaveChanges();
    }
}