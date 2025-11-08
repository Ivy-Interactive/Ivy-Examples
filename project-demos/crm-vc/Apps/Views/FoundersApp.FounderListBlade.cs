namespace Vc.Apps.Views;

public class FounderListBlade : ViewBase
{
    private record FounderListRecord(int Id, string FullName, string Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int founderId)
            {
                blades.Pop(this, true);
                blades.Push(this, new FounderDetailsBlade(founderId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var founder = (FounderListRecord)e.Sender.Tag!;
            blades.Push(this, new FounderDetailsBlade(founder.Id), founder.FullName);
        });

        ListItem CreateItem(FounderListRecord record) =>
            new(title: record.FullName, subtitle: record.Email, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Outline().Tooltip("Create Founder").ToTrigger((isOpen) => new FounderCreateDialog(isOpen, refreshToken));

        return new FilteredListView<FounderListRecord>(
            fetchRecords: (filter) => FetchFounders(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<FounderListRecord[]> FetchFounders(VcContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Founders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.FirstName.Contains(filter) || e.LastName.Contains(filter) || e.Email.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new FounderListRecord(e.Id, $"{e.FirstName} {e.LastName}", e.Email))
            .ToArrayAsync();
    }
}