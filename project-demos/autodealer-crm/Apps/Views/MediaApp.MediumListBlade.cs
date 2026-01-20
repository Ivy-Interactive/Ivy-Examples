namespace AutodealerCrm.Apps.Views;

public class MediumListBlade : ViewBase
{
    private record MediumListRecord(int Id, string FilePath, string FileType);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int mediumId)
            {
                blades.Pop(this, true);
                blades.Push(this, new MediumDetailsBlade(mediumId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var medium = (MediumListRecord)e.Sender.Tag!;
            blades.Push(this, new MediumDetailsBlade(medium.Id), medium.FilePath);
        });

        ListItem CreateItem(MediumListRecord record) =>
            new(title: record.FilePath, subtitle: record.FileType, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Media").ToTrigger((isOpen) => new MediumCreateDialog(isOpen, refreshToken));

        return new FilteredListView<MediumListRecord>(
            fetchRecords: (filter) => FetchMedia(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<MediumListRecord[]> FetchMedia(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Media.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.FilePath.Contains(filter) || e.FileType.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new MediumListRecord(e.Id, e.FilePath, e.FileType))
            .ToArrayAsync();
    }
}