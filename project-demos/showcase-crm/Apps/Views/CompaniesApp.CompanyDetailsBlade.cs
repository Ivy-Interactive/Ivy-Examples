namespace ShowcaseCrm.Apps.Views;

public class CompanyDetailsBlade(int companyId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var companyQuery = UseQuery(
            key: (nameof(CompanyDetailsBlade), companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Companies
                    .Include(e => e.Contacts)
                    .Include(e => e.Deals)
                    .Include(e => e.Leads)
                    .SingleOrDefaultAsync(e => e.Id == companyId, ct);
            },
            tags: [(typeof(Company), companyId)]
        );

        var contactCountQuery = UseQuery(
            key: (nameof(CompanyDetailsBlade), "contactCount", companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Contacts.CountAsync(e => e.CompanyId == companyId, ct);
            },
            tags: [(typeof(Company), companyId), typeof(Contact[])]
        );

        var dealCountQuery = UseQuery(
            key: (nameof(CompanyDetailsBlade), "dealCount", companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals.CountAsync(e => e.CompanyId == companyId, ct);
            },
            tags: [(typeof(Company), companyId), typeof(Deal[])]
        );

        var leadCountQuery = UseQuery(
            key: (nameof(CompanyDetailsBlade), "leadCount", companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads.CountAsync(e => e.CompanyId == companyId, ct);
            },
            tags: [(typeof(Company), companyId), typeof(Lead[])]
        );

        if (companyQuery.Loading) return Skeleton.Card();

        if (companyQuery.Value == null)
        {
            return new Callout($"Company '{companyId}' not found. It may have been deleted.")
                .Variant(CalloutVariant.Warning);
        }

        var companyValue = companyQuery.Value;

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                blades.Pop(refresh: true);
                await DeleteAsync(factory);
                queryService.RevalidateByTag(typeof(Company[]));
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .WithConfirm("Are you sure you want to delete this company?", "Delete Company");

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new CompanyEditSheet(isOpen, refreshToken, companyId));

        var detailsCard = new Card(
            content: new
                {
                    companyValue.Id,
                    companyValue.Name,
                    companyValue.Address,
                    companyValue.Phone,
                    companyValue.Website
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | deleteBtn
                    | editBtn
        ).Title("Company Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Contacts", onClick: _ =>
                {
                    blades.Push(this, new CompanyContactsBlade(companyId), "Contacts", width: Size.Units(200));
                }, badge: contactCountQuery.Value.ToString("N0")),
                new ListItem("Deals", onClick: _ =>
                {
                    blades.Push(this, new CompanyDealsBlade(companyId), "Deals", width: Size.Units(200));
                }, badge: dealCountQuery.Value.ToString("N0")),
                new ListItem("Leads", onClick: _ =>
                {
                    blades.Push(this, new CompanyLeadsBlade(companyId), "Leads", width: Size.Units(200));
                }, badge: leadCountQuery.Value.ToString("N0"))
            ));

        return new Fragment()
               | new BladeHeader(Text.Literal($"Company: {companyValue.Name}"))
               | (Layout.Vertical() | detailsCard | relatedCard);
    }

    private async Task DeleteAsync(ShowcaseCrmContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var company = await db.Companies.FirstOrDefaultAsync(e => e.Id == companyId);
        if (company != null)
        {
            db.Companies.Remove(company);
            await db.SaveChangesAsync();
        }
    }
}