namespace ShowcaseCrm.Apps.Views;

public class CompanyLeadsEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();

        var leadQuery = UseQuery(
            key: (typeof(Lead), leadId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads.FirstAsync(e => e.Id == leadId, ct);
            },
            tags: [(typeof(Lead), leadId)]
        );

        if (leadQuery.Loading || leadQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Lead");

        return leadQuery.Value
            .ToForm()
            .Builder(e => e.Source, e => e.ToTextInput())
            .Builder(e => e.StatusId, e => e.ToAsyncSelectInput(UseStatusSearch, UseStatusLookup, placeholder: "Select Status"))
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(UseContactSearch, UseContactLookup, placeholder: "Select Contact"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Lead");

        async Task OnSubmit(Lead? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Leads.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(Lead), leadId));
        }
    }

    private static QueryResult<Option<int?>[]> UseStatusSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStatusSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadStatuses
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseStatusLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStatusLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var status = await db.LeadStatuses.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (status == null) return null;
                return new Option<int?>(status.DescriptionText, status.Id);
            });
    }

    private static QueryResult<Option<int?>[]> UseCompanySearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanySearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Companies
                        .Where(e => e.Name.Contains(query))
                        .Select(e => new { e.Id, e.Name })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseCompanyLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanyLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var company = await db.Companies.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (company == null) return null;
                return new Option<int?>(company.Name, company.Id);
            });
    }

    private static QueryResult<Option<int?>[]> UseContactSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Contacts
                        .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                        .Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseContactLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var contact = await db.Contacts.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (contact == null) return null;
                return new Option<int?>(contact.FirstName + " " + contact.LastName, contact.Id);
            });
    }
}