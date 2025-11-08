namespace Vc.Apps.Views;

public class DealListBlade : ViewBase
{
    private record DealListRecord(int Id, string StartupName, string Round, decimal? Amount, DateTime? DealDate);

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int dealId)
            {
                blades.Pop(this, true);
                blades.Push(this, new DealDetailsBlade(dealId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var deal = (DealListRecord)e.Sender.Tag!;
            blades.Push(this, new DealDetailsBlade(deal.Id), deal.StartupName);
        });

        ListItem CreateItem(DealListRecord record) =>
            new(title: record.StartupName, subtitle: $"{record.Round} - {record.Amount:C}", onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Outline().Tooltip("Create Deal").ToTrigger((isOpen) => new DealCreateDialog(isOpen, refreshToken));

        return new FilteredListView<DealListRecord>(
            fetchRecords: (filter) => FetchDeals(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<DealListRecord[]> FetchDeals(VcContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Deals.Include(d => d.Startup).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(d => d.Startup.Name.Contains(filter) || d.Round.Contains(filter));
        }

        return await linq
            .OrderByDescending(d => d.CreatedAt)
            .Take(50)
            .Select(d => new DealListRecord(d.Id, d.Startup.Name, d.Round, d.Amount, d.DealDate))
            .ToArrayAsync();
    }
}