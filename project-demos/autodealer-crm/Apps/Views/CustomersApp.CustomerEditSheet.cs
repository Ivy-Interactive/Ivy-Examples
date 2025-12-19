namespace AutodealerCrm.Apps.Views;

public class CustomerEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int customerId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var customer = UseState(() => factory.CreateDbContext().Customers.FirstOrDefault(e => e.Id == customerId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            customer.Value.UpdatedAt = DateTime.UtcNow;
            db.Customers.Update(customer.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [customer]);

        return customer
            .ToForm()
            .Builder(e => e.FirstName, e => e.ToTextInput())
            .Builder(e => e.LastName, e => e.ToTextInput())
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.ViberId, e => e.ToTextInput())
            .Builder(e => e.WhatsappId, e => e.ToTextInput())
            .Builder(e => e.TelegramId, e => e.ToTextInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Customer");
    }
}