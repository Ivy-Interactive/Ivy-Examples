namespace AutodealerCrm.Apps.Views;

public class CustomerCallRecordsBlade(int customerId) : ViewBase
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
            callRecords.Set(await db.CallRecords.Include(e => e.CallDirection)
                .Include(e => e.Manager)
                .Where(e => e.CustomerId == customerId)
                .ToArrayAsync());
        }, [ EffectTrigger.AfterInit(), refreshToken ]);

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
                Direction = e.CallDirection.DescriptionText,
                Manager = e.Manager?.Name ?? "Unassigned",
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
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
                        .ToTrigger((isOpen) => new CustomerCallRecordsEditSheet(isOpen, refreshToken, e.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Call Record").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CustomerCallRecordsCreateDialog(isOpen, refreshToken, customerId));

        return new Fragment()
               | BladeHelper.WithHeader(addBtn, table)
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int callRecordId)
    {
        using var db2 = factory.CreateDbContext();
        db2.CallRecords.Remove(db2.CallRecords.Single(e => e.Id == callRecordId));
        db2.SaveChanges();
    }
}