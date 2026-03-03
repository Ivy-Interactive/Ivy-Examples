namespace ShowcaseCrm.Apps.Views;

public class CompanyCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record CompanyCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";

        public string? Address { get; init; }

        public string? Phone { get; init; }

        public string? Website { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var company = UseState(() => new CompanyCreateRequest());

        return company
            .ToForm()
            .Builder(e => e.Name, e => e.ToTextInput())
            .Builder(e => e.Address, e => e.ToTextAreaInput())
            .Builder(e => e.Phone, e => e.ToTelInput())
            .Builder(e => e.Website, e => e.ToUrlInput())
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create Company", submitTitle: "Create");

        async Task OnSubmit(CompanyCreateRequest request)
        {
            var companyId = await CreateCompanyAsync(factory, request);
            refreshToken.Refresh(companyId);
        }
    }

    private async Task<int> CreateCompanyAsync(ShowcaseCrmContextFactory factory, CompanyCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var company = new Company
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Website = request.Website,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return company.Id;
    }
}