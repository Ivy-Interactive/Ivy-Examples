namespace ShowcaseCrm.Apps.Views;

public class ContactEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int contactId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();

        var contactQuery = UseQuery(
            key: (typeof(Contact), contactId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Contacts.FirstAsync(e => e.Id == contactId, ct);
            },
            tags: [(typeof(Contact), contactId)]
        );

        if (contactQuery.Loading || contactQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Contact");

        return contactQuery.Value
            .ToForm()
            .Builder(e => e.FirstName, e => e.ToTextInput())
            .Builder(e => e.LastName, e => e.ToTextInput())
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.Phone, e => e.ToTelInput())
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Contact");

        async Task OnSubmit(Contact? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Contacts.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(Contact), contactId));
        }
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
}