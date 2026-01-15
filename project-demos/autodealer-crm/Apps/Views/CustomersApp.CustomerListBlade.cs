namespace AutodealerCrm.Apps.Views;

public class CustomerListBlade : ViewBase
{
    private record CustomerListRecord(int Id, string Name, string Email);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int customerId)
            {
                blades.Pop(this, true);
                blades.Push(this, new CustomerDetailsBlade(customerId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var customer = (CustomerListRecord)e.Sender.Tag!;
            blades.Push(this, new CustomerDetailsBlade(customer.Id), customer.Name);
        });

        ListItem CreateItem(CustomerListRecord record) =>
            new(title: record.Name, subtitle: record.Email, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Customer").ToTrigger((isOpen) => new CustomerCreateDialog(isOpen, refreshToken));

        return new FilteredListView<CustomerListRecord>(
            fetchRecords: (filter) => FetchCustomers(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<CustomerListRecord[]> FetchCustomers(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(e => e.FirstName.Contains(filter) || e.LastName.Contains(filter) || e.Email.Contains(filter));
        }

        return await linq
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .Select(e => new CustomerListRecord(e.Id, $"{e.FirstName} {e.LastName}", e.Email ?? ""))
            .ToArrayAsync();
    }
}