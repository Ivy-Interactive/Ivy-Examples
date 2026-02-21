namespace ShowcaseCrm.Apps.Views;

public class LeadDetailsBlade(int leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var blades = UseContext<IBladeService>();
        var queryService = UseService<IQueryService>();
        var refreshToken = UseRefreshToken();

        var leadQuery = UseQuery(
            key: (nameof(LeadDetailsBlade), leadId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Leads
                    .Include(e => e.Company)
                    .Include(e => e.Contact)
                    .Include(e => e.Status)
                    .SingleOrDefaultAsync(e => e.Id == leadId, ct);
            },
            tags: [(typeof(Lead), leadId)]
        );

        var dealCountQuery = UseQuery(
            key: (nameof(LeadDetailsBlade), "dealCount", leadId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals.CountAsync(e => e.LeadId == leadId, ct);
            },
            tags: [(typeof(Lead), leadId), typeof(Deal[])]
        );

        if (leadQuery.Loading) return Skeleton.Card();

        if (leadQuery.Value == null)
        {
            return new Callout($"Lead '{leadId}' not found. It may have been deleted.")
                .Variant(CalloutVariant.Warning);
        }

        var leadValue = leadQuery.Value;

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                blades.Pop(refresh: true);
                await DeleteAsync(factory);
                queryService.RevalidateByTag(typeof(Lead[]));
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash)
            .WithConfirm("Are you sure you want to delete this lead?", "Delete Lead");

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new LeadEditSheet(isOpen, refreshToken, leadId));

        var detailsCard = new Card(
            content: new
                {
                    leadValue.Id,
                    CompanyName = leadValue.Company?.Name ?? "N/A",
                    ContactName = $"{leadValue.Contact?.FirstName} {leadValue.Contact?.LastName}".Trim(),
                    Status = leadValue.Status.DescriptionText,
                    Source = leadValue.Source ?? "Unknown"
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | deleteBtn
                    | editBtn
        ).Title("Lead Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Deals", onClick: _ =>
                {
                    blades.Push(this, new LeadDealsBlade(leadId), "Deals", width: Size.Units(200));
                }, badge: dealCountQuery.Value.ToString("N0"))
            ));

        var leadTitle = leadValue.Company?.Name ?? $"{leadValue.Contact?.FirstName} {leadValue.Contact?.LastName}".Trim() ?? $"Lead #{leadValue.Id}";

        return new Fragment()
               | new BladeHeader(Text.H4(leadTitle))
               | (Layout.Vertical() | detailsCard | relatedCard);
    }

    private async Task DeleteAsync(ShowcaseCrmContextFactory dbFactory)
    {
        await using var db = dbFactory.CreateDbContext();
        var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == leadId);
        if (lead != null)
        {
            await db.Deals.Where(d => d.LeadId == leadId).ExecuteUpdateAsync(s => s.SetProperty(d => d.LeadId, (int?)null));
            db.Leads.Remove(lead);
            await db.SaveChangesAsync();
        }
    }
}