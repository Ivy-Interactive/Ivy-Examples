namespace AutodealerCrm.Apps.Views;

public class LeadListBlade : ViewBase
{
    private record LeadListRecord(int Id, string CustomerName, string LeadStage);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int leadId)
            {
                blades.Pop(this, true);
                blades.Push(this, new LeadDetailsBlade(leadId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var lead = (LeadListRecord)e.Sender.Tag!;
            blades.Push(this, new LeadDetailsBlade(lead.Id), lead.CustomerName);
        });

        ListItem CreateItem(LeadListRecord record) =>
            new(title: record.CustomerName, subtitle: record.LeadStage, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Lead").ToTrigger((isOpen) => new LeadCreateDialog(isOpen, refreshToken));

        return new FilteredListView<LeadListRecord>(
            fetchRecords: (filter) => FetchLeads(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<LeadListRecord[]> FetchLeads(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Leads
            .Include(l => l.Customer)
            .Include(l => l.LeadStage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(l => l.Customer.FirstName.Contains(filter) || l.Customer.LastName.Contains(filter));
        }

        return await linq
            .OrderByDescending(l => l.CreatedAt)
            .Take(50)
            .Select(l => new LeadListRecord(
                l.Id,
                $"{l.Customer.FirstName} {l.Customer.LastName}",
                l.LeadStage.DescriptionText
            ))
            .ToArrayAsync();
    }
}