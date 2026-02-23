namespace ShowcaseCrm.Apps.Views;

public class LeadListBlade : ViewBase
{
    private record LeadListRecord(int Id, string? CompanyName, string? ContactName, string Status);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var refreshToken = UseRefreshToken();

        var filter = UseState("");

        var leadsQuery = UseLeadListRecords(Context, filter.Value);

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int leadId)
            {
                blades.Pop(this, true);
                blades.Push(this, new LeadDetailsBlade(leadId));
                leadsQuery.Mutator.Revalidate();
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var lead = (LeadListRecord)e.Sender.Tag!;
            blades.Push(this, new LeadDetailsBlade(lead.Id), lead.CompanyName ?? lead.ContactName ?? "Lead");
        });

        object CreateItem(LeadListRecord listRecord) => new FuncView(context =>
        {
            var itemQuery = UseLeadListRecord(context, listRecord);
            if (itemQuery.Loading || itemQuery.Value == null)
            {
                return new ListItem();
            }
            var record = itemQuery.Value;
            return new ListItem(
                title: record.CompanyName ?? record.ContactName ?? "Unknown Lead",
                subtitle: record.Status,
                onClick: onItemClicked,
                tag: record
            );
        });

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Lead").ToTrigger((isOpen) => new LeadCreateDialog(isOpen, refreshToken));

        var items = (leadsQuery.Value ?? []).Select(CreateItem);

        var header = Layout.Horizontal().Gap(1)
                     | filter.ToSearchInput().Placeholder("Search Leads").Width(Size.Grow())
                     | createBtn;

        return new Fragment()
               | new BladeHeader(header)
               | (leadsQuery.Value == null ? Text.Muted("Loading...") : new List(items));
    }

    private static QueryResult<LeadListRecord[]> UseLeadListRecords(IViewContext context, string filter)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadListRecords), filter),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();

                var linq = db.Leads
                    .Include(l => l.Company)
                    .Include(l => l.Contact)
                    .Include(l => l.Status)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filter = filter.Trim();
                    linq = linq.Where(l =>
                        (l.Company != null && l.Company.Name.Contains(filter)) ||
                        (l.Contact != null && (l.Contact.FirstName.Contains(filter) || l.Contact.LastName.Contains(filter))) ||
                        l.Status.DescriptionText.Contains(filter));
                }

                return await linq
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new LeadListRecord(
                        l.Id,
                        l.Company != null ? l.Company.Name : null,
                        l.Contact != null ? $"{l.Contact.FirstName} {l.Contact.LastName}" : null,
                        l.Status.DescriptionText
                    ))
                    .ToArrayAsync(ct);
            },
            tags: [typeof(Lead[])],
            options: new QueryOptions()
            {
                KeepPrevious = true
            }
        );
    }

    private static QueryResult<LeadListRecord?> UseLeadListRecord(IViewContext context, LeadListRecord record)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadListRecord), record.Id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads
                    .Where(l => l.Id == record.Id)
                    .Select(l => new LeadListRecord(
                        l.Id,
                        l.Company != null ? l.Company.Name : null,
                        l.Contact != null ? $"{l.Contact.FirstName} {l.Contact.LastName}" : null,
                        l.Status.DescriptionText
                    ))
                    .FirstOrDefaultAsync(ct);
            },
            options: new QueryOptions { RevalidateOnMount = false },
            initialValue: record,
            tags: [(typeof(Lead), record.Id)]
        );
    }
}