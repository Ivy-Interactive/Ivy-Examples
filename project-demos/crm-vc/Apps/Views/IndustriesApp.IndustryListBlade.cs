namespace Vc.Apps.Views;

public class IndustryListBlade : ViewBase
{
    private record IndustryListRecord(int Id, string Name);

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int industryId)
            {
                blades.Pop(this, true);
                blades.Push(this, new IndustryDetailsBlade(industryId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var industry = (IndustryListRecord)e.Sender.Tag!;
            blades.Push(this, new IndustryDetailsBlade(industry.Id), industry.Name);
        });

        ListItem CreateItem(IndustryListRecord record) =>
            new(title: record.Name, subtitle: null, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Outline().Tooltip("Create Industry").ToTrigger((isOpen) => new IndustryCreateDialog(isOpen, refreshToken));

        return new FilteredListView<IndustryListRecord>(
            fetchRecords: (filter) => FetchIndustries(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<IndustryListRecord[]> FetchIndustries(VcContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Industries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.Name.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new IndustryListRecord(e.Id, e.Name))
            .ToArrayAsync();
    }
}