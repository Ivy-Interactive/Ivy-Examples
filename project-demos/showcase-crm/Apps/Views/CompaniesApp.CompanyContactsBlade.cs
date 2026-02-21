namespace ShowcaseCrm.Apps.Views;

public class CompanyContactsBlade(int companyId) : ViewBase
{
    private record ContactTableRecord(int Id, string FirstName, string LastName, string Email, string? Phone);

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();
        var (sheetView, showSheet) = UseTrigger((IState<bool> isOpen, int id) => new CompanyContactsEditSheet(isOpen, refreshToken, id));

        var contactsQuery = UseQuery(
            key: (nameof(CompanyContactsBlade), companyId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Contacts.Where(c => c.CompanyId == companyId).ToArrayAsync(ct);
            },
            tags: [typeof(Contact[]), (typeof(Company), companyId)]
        );

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int)
            {
                contactsQuery.Mutator.Revalidate();
                queryService.RevalidateByTag((typeof(Company), companyId));
            }
        }, [refreshToken]);

        if (contactsQuery.Loading) return Skeleton.Card();

        if (contactsQuery.Value == null || contactsQuery.Value.Length == 0)
        {
            var addBtnEmpty = new Button("Add Contact").Icon(Icons.Plus).Outline()
                .ToTrigger((isOpen) => new CompanyContactsCreateDialog(isOpen, refreshToken, companyId));

            return new Fragment()
                   | new BladeHeader(addBtnEmpty)
                   | new Callout("No contacts found for this company. Add a contact to get started.").Variant(CalloutVariant.Info);
        }

        var tableData = contactsQuery.Value.Select(c => new ContactTableRecord(c.Id, c.FirstName, c.LastName, c.Email ?? "", c.Phone)).ToArray();
        var dataTableKey = $"contacts-{companyId}-{tableData.Length}-{tableData.Aggregate(0, (h, c) => HashCode.Combine(h, c.Id))}";
        var dataTable = tableData.AsQueryable()
            .ToDataTable(idSelector: c => c.Id)
            .Key(dataTableKey)
            .Header(c => c.Id, "Id")
            .Header(c => c.FirstName, "First Name")
            .Header(c => c.LastName, "Last Name")
            .Header(c => c.Email, "Email")
            .Header(c => c.Phone, "Phone")
            .Width(c => c.Id, Size.Px(40))
            .LoadAllRows(true)
            .Config(config =>
            {
                config.AllowSorting = true;
                config.AllowFiltering = true;
                config.ShowSearch = true;
            })
            .RowActions(
                MenuItem.Default(Icons.Pencil, "edit").Tag("edit"),
                MenuItem.Default(Icons.Trash2, "delete").Tag("delete")
            )
            .HandleRowAction(e =>
            {
                var args = e.Value;
                var tag = args.Tag?.ToString();
                var idStr = args.Id?.ToString();
                if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id)) return ValueTask.CompletedTask;

                if (tag == "edit")
                {
                    showSheet(id);
                }
                else if (tag == "delete")
                {
                    showAlert("Are you sure you want to delete this contact?", async result =>
                    {
                        if (result.IsOk())
                        {
                            await Delete(factory, id);
                            contactsQuery.Mutator.Revalidate();
                            queryService.RevalidateByTag((typeof(Company), companyId));
                        }
                    }, "Delete Contact", AlertButtonSet.OkCancel);
                }
                return ValueTask.CompletedTask;
            })
            .Width(Size.Full())
            .Height(Size.Full());

        var addBtn = new Button("Add Contact").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CompanyContactsCreateDialog(isOpen, refreshToken, companyId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | dataTable
               | sheetView
               | alertView;
    }

    private async Task Delete(ShowcaseCrmContextFactory factory, int contactId)
    {
        await using var db = factory.CreateDbContext();
        var contact = await db.Contacts.SingleOrDefaultAsync(c => c.Id == contactId);
        if (contact != null)
        {
            db.Contacts.Remove(contact);
            await db.SaveChangesAsync();
        }
    }
}