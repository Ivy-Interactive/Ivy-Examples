namespace Vc.Apps.Views;

public class IndustryEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int industryId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var industry = UseState(() => factory.CreateDbContext().Industries.FirstOrDefault(e => e.Id == industryId)!);
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                using var db = factory.CreateDbContext();
                industry.Value.UpdatedAt = DateTime.UtcNow;
                db.Industries.Update(industry.Value);
                db.SaveChanges();
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [industry]);

        return industry
            .ToForm()
            .Place(e => e.Name)
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Industry");
    }
}