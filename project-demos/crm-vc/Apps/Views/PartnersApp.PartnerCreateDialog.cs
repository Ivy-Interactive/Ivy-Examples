namespace Vc.Apps.Views;

public class PartnerCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record PartnerCreateRequest
    {
        [Required]
        public string FirstName { get; init; } = "";

        [Required]
        public string LastName { get; init; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; init; } = "";

        [Required]
        public int? GenderId { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var partner = UseState(() => new PartnerCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                var partnerId = CreatePartner(factory, partner.Value);
                refreshToken.Refresh(partnerId);
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [partner]);

        return partner
            .ToForm()
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.GenderId, e => e.ToAsyncSelectInput(QueryGenders(factory), LookupGender(factory), placeholder: "Select Gender"))
            .ToDialog(isOpen, title: "Create Partner", submitTitle: "Create");
    }

    private int CreatePartner(VcContextFactory factory, PartnerCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var partner = new Partner()
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            GenderId = request.GenderId!.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Partners.Add(partner);
        db.SaveChanges();

        return partner.Id;
    }

    private static AsyncSelectQueryDelegate<int?> QueryGenders(VcContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Genders
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupGender(VcContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var gender = await db.Genders.FirstOrDefaultAsync(e => e.Id == id);
            if (gender == null) return null;
            return new Option<int?>(gender.DescriptionText, gender.Id);
        };
    }
}