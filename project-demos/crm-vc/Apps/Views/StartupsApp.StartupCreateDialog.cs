namespace Vc.Apps.Views;

public class StartupCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record StartupCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";

        [Required]
        public string Description { get; init; } = "";

        public DateTime? FoundedAt { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var startup = UseState(() => new StartupCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                var startupId = CreateStartup(factory, startup.Value);
                refreshToken.Refresh(startupId);
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [startup]);

        return startup
            .ToForm()
            .Builder(e => e.FoundedAt, e => e.ToDateInput())
            .ToDialog(isOpen, title: "Create Startup", submitTitle: "Create");
    }

    private int CreateStartup(VcContextFactory factory, StartupCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var startup = new Startup
        {
            Name = request.Name,
            Description = request.Description,
            FoundedAt = request.FoundedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Startups.Add(startup);
        db.SaveChanges();

        return startup.Id;
    }
}