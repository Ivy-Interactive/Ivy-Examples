namespace ShowcaseCrm.Apps.Views;

public class ContactCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record ContactCreateRequest
    {
        [Required]
        public int? CompanyId { get; init; } = null;

        [Required]
        public string FirstName { get; init; } = "";

        [Required]
        public string LastName { get; init; } = "";

        public string? Email { get; init; } = null;

        public string? Phone { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var contact = UseState(() => new ContactCreateRequest());

        return contact
            .ToForm()
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create Contact", submitTitle: "Create");

        async Task OnSubmit(ContactCreateRequest request)
        {
            var contactId = await CreateContactAsync(factory, request);
            refreshToken.Refresh(contactId);
        }
    }

    private async Task<int> CreateContactAsync(ShowcaseCrmContextFactory factory, ContactCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var contact = new Contact
        {
            CompanyId = request.CompanyId!.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        return contact.Id;
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