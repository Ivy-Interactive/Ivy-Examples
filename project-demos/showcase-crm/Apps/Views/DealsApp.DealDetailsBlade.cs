namespace ShowcaseCrm.Apps.Views;

public class DealDetailsBlade(int dealId) : ViewBase
{
    public override object? Build()
    {
        var isDeleting = UseState(false);
        var factory = UseService<ShowcaseCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var dealQuery = UseQuery(
            key: (nameof(DealDetailsBlade), dealId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals
                    .Include(e => e.Company)
                    .Include(e => e.Contact)
                    .Include(e => e.Lead)
                    .Include(e => e.Stage)
                    .SingleOrDefaultAsync(e => e.Id == dealId, ct);
            },
            tags: [(typeof(Deal), dealId)]
        );

        if (dealQuery.Loading) return Skeleton.Card();

        if (dealQuery.Value == null)
        {
            return new Callout($"Deal '{dealId}' not found. It may have been deleted.")
                .Variant(CalloutVariant.Warning);
        }

        var dealValue = dealQuery.Value;

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                isDeleting.Set(true);
                await Task.Delay(50);
                try
                {
                    await DeleteAsync(factory);
                    queryService.RevalidateByTag(typeof(Deal[]));
                    blades.Pop(refresh: true);
                }
                finally
                {
                    isDeleting.Set(false);
                }
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .Loading(isDeleting.Value)
            .Disabled(isDeleting.Value)
            .WithConfirm("Are you sure you want to delete this deal?", "Delete Deal");

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new DealEditSheet(isOpen, refreshToken, dealId));

        var detailsCard = new Card(
            content: new
            {
                Id = dealValue.Id,
                Company = dealValue.Company.Name,
                Contact = $"{dealValue.Contact.FirstName} {dealValue.Contact.LastName}",
                Lead = dealValue.Lead?.Source ?? "N/A",
                Stage = dealValue.Stage.DescriptionText,
                Amount = dealValue.Amount?.ToString("C") ?? "N/A",
                CloseDate = dealValue.CloseDate?.ToString("d") ?? "N/A"
            }
                .ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).AlignContent(Align.Right)
                    | deleteBtn
                    | editBtn
        ).Title("Deal Details").Width(Size.Units(100));

        return new Fragment()
               | new BladeHeader(Text.Literal($"Deal: {dealValue.Id}"))
               | (Layout.Vertical() | detailsCard);
    }

    private async Task DeleteAsync(ShowcaseCrmContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var deal = await db.Deals.FirstOrDefaultAsync(e => e.Id == dealId);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            await db.SaveChangesAsync();
        }
    }
}