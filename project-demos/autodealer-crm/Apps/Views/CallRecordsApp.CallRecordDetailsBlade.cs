namespace AutodealerCrm.Apps.Views;

public class CallRecordDetailsBlade(int callRecordId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();
        var callRecord = UseState<CallRecord?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            callRecord.Set(await db.CallRecords
                .Include(e => e.Customer)
                .Include(e => e.Lead)
                .Include(e => e.Manager)
                .Include(e => e.CallDirection)
                .SingleOrDefaultAsync(e => e.Id == callRecordId));
        }, [EffectTrigger.OnMount(), refreshToken]);

        if (callRecord.Value == null) return null;

        var callRecordValue = callRecord.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this call record?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Call Record", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(onDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new CallRecordEditSheet(isOpen, refreshToken, callRecordId));

        var detailsCard = new Card(
            content: new
            {
                Id = callRecordValue.Id,
                Customer = $"{callRecordValue.Customer.FirstName} {callRecordValue.Customer.LastName}",
                Lead = callRecordValue.Lead?.Id.ToString() ?? "N/A",
                Manager = callRecordValue.Manager?.Name ?? "N/A",
                CallDirection = callRecordValue.CallDirection.DescriptionText,
                StartTime = callRecordValue.StartTime,
                EndTime = callRecordValue.EndTime,
                Duration = callRecordValue.Duration?.ToString() ?? "N/A",
                RecordingUrl = callRecordValue.RecordingUrl ?? "N/A",
                ScriptScore = callRecordValue.ScriptScore ?? "N/A",
                Sentiment = callRecordValue.Sentiment ?? "N/A"
            }
            .ToDetails()
            .MultiLine(e => e.RecordingUrl)
            .RemoveEmpty()
            .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                | dropDown
                | editBtn
        ).Title("Call Record Details").Width(Size.Units(100));

        return new Fragment()
               | (Layout.Vertical() | detailsCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var callRecord = db.CallRecords.FirstOrDefault(e => e.Id == callRecordId)!;
        db.CallRecords.Remove(callRecord);
        db.SaveChanges();
    }
}