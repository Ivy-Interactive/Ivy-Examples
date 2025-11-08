namespace Vc.Apps.Views;

public class StartupDealsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int startupId) : ViewBase
{
    private record DealCreateRequest
    {
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
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .ToDialog(isOpen, title: "Create Deal", submitTitle: "Create");
    }

    private int CreateDeal(VcContextFactory factory, DealCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var deal = new Deal
        {
            Round = request.Round,
            Amount = request.Amount,
            DealDate = request.DealDate,
            StartupId = startupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Deals.Add(deal);
        db.SaveChanges();

        return deal.Id;
    }
}