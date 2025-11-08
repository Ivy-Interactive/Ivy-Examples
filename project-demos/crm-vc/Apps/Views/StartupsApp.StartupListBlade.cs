namespace Vc.Apps.Views;

public class StartupListBlade : ViewBase
{
    private record StartupListRecord(int Id, string Name, string? Description);

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var factory = UseService<VcContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int startupId)
            {
                blades.Pop(this, true);
                blades.Push(this, new StartupDetailsBlade(startupId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var startup = (StartupListRecord)e.Sender.Tag!;
            blades.Push(this, new StartupDetailsBlade(startup.Id), startup.Name);
        });

        ListItem CreateItem(StartupListRecord record) =>
            new(title: record.Name, subtitle: null, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Outline().Tooltip("Create Startup").ToTrigger((isOpen) => new StartupCreateDialog(isOpen, refreshToken));

        return new FilteredListView<StartupListRecord>(
            fetchRecords: (filter) => FetchStartups(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<StartupListRecord[]> FetchStartups(VcContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Startups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.Name.Contains(filter) || e.Description.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new StartupListRecord(e.Id, e.Name, e.Description))
            .ToArrayAsync();
    }
}