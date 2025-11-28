namespace AutodealerCrm.Apps.Views;

public class CallRecordListBlade : ViewBase
{
    private record CallRecordListRecord(int Id, DateTime StartTime, DateTime EndTime);
    
    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        
        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int callRecordId)
            {
                blades.Pop(this, true);
                blades.Push(this, new CallRecordDetailsBlade(callRecordId));
            }
        }, [refreshToken]);
        
        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var callRecord = (CallRecordListRecord)e.Sender.Tag!;
            blades.Push(this, new CallRecordDetailsBlade(callRecord.Id), callRecord.StartTime.ToString("g"));
        });
        
        ListItem CreateItem(CallRecordListRecord record) => 
            new(title: record.StartTime.ToString("g"), subtitle: record.EndTime.ToString("g"), onClick: onItemClicked, tag: record);
        
        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Call Record").ToTrigger((isOpen) => new CallRecordCreateDialog(isOpen, refreshToken));
        
        return new FilteredListView<CallRecordListRecord>(
            fetchRecords: (filter) => FetchCallRecords(factory, filter), 
            createItem: CreateItem, 
            toolButtons: createBtn, 
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<CallRecordListRecord[]> FetchCallRecords(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();
        
        var callRecords = await db.CallRecords
            .OrderByDescending(e => e.CreatedAt) 
            .Take(50)
            .ToArrayAsync();
        
        var records = callRecords
            .Select(e => new CallRecordListRecord(e.Id, e.StartTime, e.EndTime))
            .AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            records = records.Where(e => e.StartTime.ToString("g").Contains(filter, StringComparison.OrdinalIgnoreCase) || 
                                        e.EndTime.ToString("g").Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return records.ToArray();
    }
}