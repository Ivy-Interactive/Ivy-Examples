namespace Vc.Apps.Views;

public class DealEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int dealId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var deal = UseState(() => factory.CreateDbContext().Deals.FirstOrDefault(e => e.Id == dealId)!);
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                using var db = factory.CreateDbContext();
                deal.Value.UpdatedAt = DateTime.UtcNow;
                db.Deals.Update(deal.Value);
                db.SaveChanges();
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [deal]);

        return deal
            .ToForm()
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Deal");
    }
}