namespace Vc.Apps.Views;

public class StartupEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int startupId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var startup = UseState(() => factory.CreateDbContext().Startups.FirstOrDefault(e => e.Id == startupId)!);
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                using var db = factory.CreateDbContext();
                startup.Value.UpdatedAt = DateTime.UtcNow;
                db.Startups.Update(startup.Value);
                db.SaveChanges();
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [startup]);

        return startup
            .ToForm()
            .Builder(e => e.Description, e => e.ToTextAreaInput())
            .Builder(e => e.FoundedAt, e => e.ToDateInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Startup");
    }
}