namespace ShowcaseCrm.Apps.Views;

public class DealListBlade : ViewBase
{
    private record DealListRecord(int Id, string CompanyName, string ContactName, decimal? Amount, string StageDescription);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");

        var dealsQuery = UseDealListRecords(Context, filter.Value);

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int dealId)
            {
                blades.Pop(this, true);
                blades.Push(this, new DealDetailsBlade(dealId));
                dealsQuery.Mutator.Revalidate();
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var deal = (DealListRecord)e.Sender.Tag!;
            blades.Push(this, new DealDetailsBlade(deal.Id), $"{deal.CompanyName} - {deal.ContactName}");
        });

        object CreateItem(DealListRecord listRecord) => new FuncView(context =>
        {
            var itemQuery = UseDealListRecord(context, listRecord);
            if (itemQuery.Loading || itemQuery.Value == null)
            {
                return new ListItem();
            }
            var record = itemQuery.Value;
            return new ListItem(
                title: $"{record.CompanyName} - {record.ContactName}",
                subtitle: $"{record.StageDescription} | Amount: {record.Amount:C}",
                onClick: onItemClicked,
                tag: record
            );
        });

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Deal").ToTrigger((isOpen) => new DealCreateDialog(isOpen, refreshToken));

        var items = (dealsQuery.Value ?? []).Select(CreateItem);

        var header = Layout.Horizontal().Gap(1)
                     | filter.ToSearchInput().Placeholder("Search").Width(Size.Grow())
                     | createBtn;

        return new Fragment()
               | new BladeHeader(header)
               | (dealsQuery.Value == null ? Text.Muted("Loading...") : new List(items));
    }

    private static QueryResult<DealListRecord[]> UseDealListRecords(IViewContext context, string filter)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseDealListRecords), filter),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var linq = db.Deals
                    .Include(d => d.Company)
                    .Include(d => d.Contact)
                    .Include(d => d.Stage)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    linq = linq.Where(d =>
                        d.Company.Name.Contains(filter) ||
                        d.Contact.FirstName.Contains(filter) ||
                        d.Contact.LastName.Contains(filter) ||
                        d.Stage.DescriptionText.Contains(filter));
                }

                return await linq
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(50)
                    .Select(d => new DealListRecord(
                        d.Id,
                        d.Company.Name,
                        $"{d.Contact.FirstName} {d.Contact.LastName}",
                        d.Amount,
                        d.Stage.DescriptionText
                    ))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Deal[])],
            options: new QueryOptions()
            {
                KeepPrevious = true
            }
        );
    }

    private static QueryResult<DealListRecord?> UseDealListRecord(IViewContext context, DealListRecord record)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseDealListRecord), record.Id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals
                    .Where(d => d.Id == record.Id)
                    .Include(d => d.Company)
                    .Include(d => d.Contact)
                    .Include(d => d.Stage)
                    .Select(d => new DealListRecord(
                        d.Id,
                        d.Company.Name,
                        $"{d.Contact.FirstName} {d.Contact.LastName}",
                        d.Amount,
                        d.Stage.DescriptionText
                    ))
                    .FirstOrDefaultAsync(ct);
            },
            options: new QueryOptions { RevalidateOnMount = false },
            initialValue: record,
            tags: [(typeof(Deal), record.Id)]
        );
    }
}