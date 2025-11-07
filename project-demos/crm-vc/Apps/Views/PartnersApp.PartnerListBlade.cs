namespace Vc.Apps.Views;

public class PartnerListBlade : ViewBase
{
    private record PartnerListRecord(int Id, string Name, string Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int partnerId)
            {
                blades.Pop(this, true);
                blades.Push(this, new PartnerDetailsBlade(partnerId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var partner = (PartnerListRecord)e.Sender.Tag!;
            blades.Push(this, new PartnerDetailsBlade(partner.Id), partner.Name);
        });

        ListItem CreateItem(PartnerListRecord record) =>
            new(title: record.Name, subtitle: record.Email, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Outline().Tooltip("Create Partner").ToTrigger((isOpen) => new PartnerCreateDialog(isOpen, refreshToken));

        return new FilteredListView<PartnerListRecord>(
            fetchRecords: (filter) => FetchPartners(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<PartnerListRecord[]> FetchPartners(VcContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Partners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.FirstName.Contains(filter) || e.LastName.Contains(filter) || e.Email.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new PartnerListRecord(e.Id, $"{e.FirstName} {e.LastName}", e.Email))
            .ToArrayAsync();
    }
}