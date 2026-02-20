namespace ShowcaseCrm.Apps.Views;

public class CompanyListBlade : ViewBase
{
    private record CompanyListRecord(int Id, string Name, string? Address);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");

        var companiesQuery = UseCompanyListRecords(Context, filter.Value);

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int companyId)
            {
                blades.Pop(this, true);
                companiesQuery.Mutator.Revalidate();
                blades.Push(this, new CompanyDetailsBlade(companyId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var company = (CompanyListRecord)e.Sender.Tag!;
            blades.Push(this, new CompanyDetailsBlade(company.Id), company.Name);
        });

        object CreateItem(CompanyListRecord listRecord) => new FuncView(context =>
        {
            var itemQuery = UseCompanyListRecord(context, listRecord);
            if (itemQuery.Loading || itemQuery.Value == null)
            {
                return new ListItem();
            }
            var record = itemQuery.Value;
            return new ListItem(title: record.Name, subtitle: record.Address, onClick: onItemClicked, tag: record);
        });

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Company").ToTrigger((isOpen) => new CompanyCreateDialog(isOpen, refreshToken));

        var items = (companiesQuery.Value ?? []).Select(CreateItem);

        var header = Layout.Horizontal().Gap(1)
                     | filter.ToSearchInput().Placeholder("Search").Width(Size.Grow())
                     | createBtn;

        return new Fragment()
               | new BladeHeader(header)
               | (companiesQuery.Value == null ? Text.Muted("Loading...") : new List(items));
    }

    private static QueryResult<CompanyListRecord[]> UseCompanyListRecords(IViewContext context, string filter)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanyListRecords), filter),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var linq = db.Companies.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    linq = linq.Where(e => e.Name.Contains(filter) || (e.Address != null && e.Address.Contains(filter)));
                }

                return await linq
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(50)
                    .Select(e => new CompanyListRecord(e.Id, e.Name, e.Address))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Company[])],
            options: new QueryOptions()
            {
                KeepPrevious = true
            }
        );
    }

    private static QueryResult<CompanyListRecord?> UseCompanyListRecord(IViewContext context, CompanyListRecord record)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanyListRecord), record.Id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Companies
                    .Where(e => e.Id == record.Id)
                    .Select(e => new CompanyListRecord(e.Id, e.Name, e.Address))
                    .FirstOrDefaultAsync(ct);
            },
            options: new QueryOptions { RevalidateOnMount = false },
            initialValue: record,
            tags: [(typeof(Company), record.Id)]
        );
    }
}