namespace ShowcaseCrm.Apps.Views;

public class CompanyContactsEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int contactId) : ViewBase
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
}