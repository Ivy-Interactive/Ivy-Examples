namespace Vc.Apps.Views;

public class DealCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record DealCreateRequest
    {
        [Required]
        public int? StartupId { get; init; } = null;

        [Required]
        public string Round { get; init; } = "";

        [Required]
        public decimal? Amount { get; init; } = null;

        [Required]
        public DateTime? DealDate { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var dealState = UseState(() => new DealCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                var dealId = CreateDeal(factory, dealState.Value);
                refreshToken.Refresh(dealId);
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [dealState]);

        return dealState
            .ToForm()
            .Builder(e => e.StartupId, e => e.ToAsyncSelectInput(QueryStartups(factory), LookupStartup(factory), placeholder: "Select Startup"))
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .ToDialog(isOpen, title: "Create Deal", submitTitle: "Create");
    }

    private int CreateDeal(VcContextFactory factory, DealCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var deal = new Deal
        {
            StartupId = request.StartupId!.Value,
            Round = request.Round,
            Amount = request.Amount,
            DealDate = request.DealDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Deals.Add(deal);
        db.SaveChanges();

        return deal.Id;
    }

    private static AsyncSelectQueryDelegate<int?> QueryStartups(VcContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Startups
                    .Where(e => e.Name.Contains(query))
                    .Select(e => new { e.Id, e.Name })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupStartup(VcContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var startup = await db.Startups.FirstOrDefaultAsync(e => e.Id == id);
            if (startup == null) return null;
            return new Option<int?>(startup.Name, startup.Id);
        };
    }
}