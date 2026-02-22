namespace ShowcaseCrm.Apps.Views;

public class CompanyEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int companyId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();

        var companyQuery = UseQuery(
            key: (typeof(Company), companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Companies.FirstAsync(e => e.Id == companyId, ct);
            },
            tags: [(typeof(Company), companyId)]
        );

        if (companyQuery.Loading || companyQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Company");

        return companyQuery.Value
            .ToForm()
            .Builder(e => e.Name, e => e.ToTextInput())
            .Builder(e => e.Address, e => e.ToTextAreaInput())
            .Builder(e => e.Phone, e => e.ToTelInput())
            .Builder(e => e.Website, e => e.ToUrlInput())
            .Place(e => e.Name, e => e.Address, e => e.Phone, e => e.Website)
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Company");

        async Task OnSubmit(Company? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Companies.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(Company), companyId));
        }
    }
}