namespace Vc.Apps.Views;

public class IndustryCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record IndustryCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";
    }

    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var industry = UseState(() => new IndustryCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                var industryId = CreateIndustry(factory, industry.Value);
                refreshToken.Refresh(industryId);
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [industry]);

        return industry
            .ToForm()
            .ToDialog(isOpen, title: "Create Industry", submitTitle: "Create");
    }

    private int CreateIndustry(VcContextFactory factory, IndustryCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var industry = new Industry()
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Industries.Add(industry);
        db.SaveChanges();

        return industry.Id;
    }
}