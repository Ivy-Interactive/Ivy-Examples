namespace AutodealerCrm.Apps.Views;

public class CustomerCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record CustomerCreateRequest
    {
        [Required]
        public string FirstName { get; init; } = "";

        [Required]
        public string LastName { get; init; } = "";

        public string? Email { get; init; }

        public string? ViberId { get; init; }

        public string? WhatsappId { get; init; }

        public string? TelegramId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var customer = UseState(() => new CustomerCreateRequest());

        UseEffect(() =>
        {
            var customerId = CreateCustomer(factory, customer.Value);
            refreshToken.Refresh(customerId);

        }, [customer]);

        return customer
            .ToForm()
            .ToDialog(isOpen, title: "Create Customer", submitTitle: "Create");
    }

    private int CreateCustomer(AutodealerCrmContextFactory factory, CustomerCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var customer = new Customer()
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            ViberId = request.ViberId,
            WhatsappId = request.WhatsappId,
            TelegramId = request.TelegramId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        db.SaveChanges();

        return customer.Id;
    }
}