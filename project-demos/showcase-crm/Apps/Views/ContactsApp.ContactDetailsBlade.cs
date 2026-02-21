namespace ShowcaseCrm.Apps.Views;

public class ContactDetailsBlade(int contactId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var contactQuery = UseQuery(
            key: (nameof(ContactDetailsBlade), contactId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Contacts
                    .Include(c => c.Company)
                    .SingleOrDefaultAsync(c => c.Id == contactId, ct);
            },
            tags: [(typeof(Contact), contactId)]
        );

        var dealCountQuery = UseQuery(
            key: (nameof(ContactDetailsBlade), "dealCount", contactId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals.CountAsync(d => d.ContactId == contactId, ct);
            },
            tags: [(typeof(Contact), contactId), typeof(Deal[])]
        );

        var leadCountQuery = UseQuery(
            key: (nameof(ContactDetailsBlade), "leadCount", contactId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads.CountAsync(l => l.ContactId == contactId, ct);
            },
            tags: [(typeof(Contact), contactId), typeof(Lead[])]
        );

        if (contactQuery.Loading) return Skeleton.Card();

        if (contactQuery.Value == null)
        {
            return new Callout($"Contact '{contactId}' not found. It may have been deleted.")
                .Variant(CalloutVariant.Warning);
        }

        var contactValue = contactQuery.Value;

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                blades.Pop(refresh: true);
                await DeleteAsync(factory);
                queryService.RevalidateByTag(typeof(Contact[]));
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .WithConfirm("Are you sure you want to delete this contact?", "Delete Contact");

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new ContactEditSheet(isOpen, refreshToken, contactId));

        var detailsCard = new Card(
            content: new
                {
                    FullName = $"{contactValue.FirstName} {contactValue.LastName}",
                    contactValue.Email,
                    contactValue.Phone,
                    CompanyName = contactValue.Company.Name
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.FullName, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | deleteBtn
                    | editBtn
        ).Title("Contact Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Deals", onClick: _ =>
                {
                    blades.Push(this, new ContactDealsBlade(contactId), "Deals", width: Size.Units(200));
                }, badge: dealCountQuery.Value.ToString("N0")),
                new ListItem("Leads", onClick: _ =>
                {
                    blades.Push(this, new ContactLeadsBlade(contactId), "Leads", width: Size.Units(200));
                }, badge: leadCountQuery.Value.ToString("N0"))
            ));

        return new Fragment()
               | new BladeHeader(Text.H4($"{contactValue.FirstName} {contactValue.LastName}"))
               | (Layout.Vertical() | detailsCard | relatedCard);
    }

    private async Task DeleteAsync(ShowcaseCrmContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);
        if (contact != null)
        {
            db.Contacts.Remove(contact);
            await db.SaveChangesAsync();
        }
    }
}