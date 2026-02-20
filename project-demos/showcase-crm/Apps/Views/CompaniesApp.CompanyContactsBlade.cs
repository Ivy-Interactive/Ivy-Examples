namespace ShowcaseCrm.Apps.Views;

public class CompanyContactsBlade(int companyId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();
        var (alertView, showAlert) = this.UseAlert();

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

        var table = contactsQuery.Value.Select(c => new
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                _ = Layout.Horizontal().Gap(2)
                    | new Button("Delete", onClick: async _ =>
                        {
                            showAlert("Are you sure you want to delete this contact?", async result =>
                            {
                                if (result.IsOk())
                                {
                                    await Delete(factory, c.Id);
                                    contactsQuery.Mutator.Revalidate();
                                    queryService.RevalidateByTag((typeof(Company), companyId));
                                }
                            }, "Delete Contact", AlertButtonSet.OkCancel);
                        })
                        .Variant(ButtonVariant.Ghost)
                        .Icon(Icons.Trash)
                        .WithConfirm("Are you sure you want to delete this contact?", "Delete Contact")
                    | Icons.Pencil
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new CompanyContactsEditSheet(isOpen, refreshToken, c.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Contact").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new CompanyContactsCreateDialog(isOpen, refreshToken, companyId));

        return new Fragment()
               | new BladeHeader(addBtn)
               | table
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