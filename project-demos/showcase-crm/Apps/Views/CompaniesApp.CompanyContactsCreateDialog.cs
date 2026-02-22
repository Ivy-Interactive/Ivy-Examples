namespace ShowcaseCrm.Apps.Views;

public class CompanyContactsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int companyId) : ViewBase
{
    private record ContactCreateRequest
    {
        [Required]
        public string FirstName { get; init; } = "";

        [Required]
        public string LastName { get; init; } = "";

        [Required]
        public string? Email { get; init; } = null;

        [Required]
        public string? Phone { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var contact = UseState(() => new ContactCreateRequest());

        return contact
            .ToForm()
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
            CompanyId = companyId,
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
}