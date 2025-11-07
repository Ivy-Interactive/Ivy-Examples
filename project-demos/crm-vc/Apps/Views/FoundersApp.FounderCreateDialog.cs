namespace Vc.Apps.Views;

public class FounderCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record FounderCreateRequest
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
        var founder = UseState(() => new FounderCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                var founderId = CreateFounder(factory, founder.Value);
                refreshToken.Refresh(founderId);
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [founder]);

        return founder
            .ToForm()
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.GenderId, e => e.ToAsyncSelectInput(QueryGenders(factory), LookupGender(factory), placeholder: "Select Gender"))
            .ToDialog(isOpen, title: "Create Founder", submitTitle: "Create");
    }

    private int CreateFounder(VcContextFactory factory, FounderCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var founder = new Founder
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            GenderId = request.GenderId!.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Founders.Add(founder);
        db.SaveChanges();

        return founder.Id;
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