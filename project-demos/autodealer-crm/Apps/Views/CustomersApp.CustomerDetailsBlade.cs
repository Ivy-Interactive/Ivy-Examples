namespace AutodealerCrm.Apps.Views;

public class CustomerDetailsBlade(int customerId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<AutodealerCrmContextFactory>();
        var blades = this.UseContext<IBladeService>();
        var refreshToken = this.UseRefreshToken();
        var customer = this.UseState<Customer?>();
        var callRecordCount = this.UseState<int>();
        var leadCount = this.UseState<int>();
        var mediaCount = this.UseState<int>();
        var messageCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            customer.Set(await db.Customers.SingleOrDefaultAsync(e => e.Id == customerId));
            callRecordCount.Set(await db.CallRecords.CountAsync(e => e.CustomerId == customerId));
            leadCount.Set(await db.Leads.CountAsync(e => e.CustomerId == customerId));
            mediaCount.Set(await db.Media.CountAsync(e => e.CustomerId == customerId));
            messageCount.Set(await db.Messages.CountAsync(e => e.CustomerId == customerId));
        }, [EffectTrigger.OnMount(), refreshToken]);

        if (customer.Value == null) return null;

        var customerValue = customer.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this customer?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Customer", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete)
            );

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new CustomerEditSheet(isOpen, refreshToken, customerId));

        var detailsCard = new Card(
            content: new
                {
                    customerValue.Id,
                    FullName = $"{customerValue.FirstName} {customerValue.LastName}",
                    customerValue.Email,
                    customerValue.ViberId,
                    customerValue.WhatsappId,
                    customerValue.TelegramId
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Customer Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Call Records", onClick: _ =>
                {
                    blades.Push(this, new CustomerCallRecordsBlade(customerId), "Call Records");
                }, badge: callRecordCount.Value.ToString("N0")),
                new ListItem("Leads", onClick: _ =>
                {
                    blades.Push(this, new CustomerLeadsBlade(customerId), "Leads");
                }, badge: leadCount.Value.ToString("N0")),
                new ListItem("Media", onClick: _ =>
                {
                    blades.Push(this, new CustomerMediaBlade(customerId), "Media");
                }, badge: mediaCount.Value.ToString("N0")),
                new ListItem("Messages", onClick: _ =>
                {
                    blades.Push(this, new CustomerMessagesBlade(customerId), "Messages");
                }, badge: messageCount.Value.ToString("N0"))
            ));

        return new Fragment()
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var customer = db.Customers.FirstOrDefault(e => e.Id == customerId)!;
        db.Customers.Remove(customer);
        db.SaveChanges();
    }
}