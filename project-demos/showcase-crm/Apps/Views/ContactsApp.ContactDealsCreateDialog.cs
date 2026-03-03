namespace ShowcaseCrm.Apps.Views;

public class ContactDealsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int contactId) : ViewBase
{
    private record DealCreateRequest
    {
        [Required]
        public int CompanyId { get; init; }

        [Required]
        public int StageId { get; init; }

        [Required]
        public decimal Amount { get; init; }

        [Required]
        public DateTime CloseDate { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var deal = UseState(() => new DealCreateRequest());

        return deal
            .ToForm()
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Builder(e => e.StageId, e => e.ToAsyncSelectInput(UseStageSearch, UseStageLookup, placeholder: "Select Stage"))
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.CloseDate, e => e.ToDateInput())
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create Deal", submitTitle: "Create");

        async Task OnSubmit(DealCreateRequest request)
        {
            var dealId = await CreateDealAsync(factory, request);
            refreshToken.Refresh(dealId);
        }
    }

    private async Task<int> CreateDealAsync(ShowcaseCrmContextFactory factory, DealCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var deal = new Deal
        {
            CompanyId = request.CompanyId,
            ContactId = contactId,
            StageId = request.StageId,
            Amount = request.Amount,
            CloseDate = request.CloseDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Deals.Add(deal);
        await db.SaveChangesAsync();

        return deal.Id;
    }

    private static QueryResult<Option<int?>[]> UseCompanySearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanySearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Companies
                        .Where(e => e.Name.Contains(query))
                        .Select(e => new { e.Id, e.Name })
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseCompanyLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseCompanyLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var company = await db.Companies.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (company == null) return null;
                return new Option<int?>(company.Name, company.Id);
            });
    }

    private static QueryResult<Option<int?>[]> UseStageSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStageSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.DealStages
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseStageLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStageLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var stage = await db.DealStages.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (stage == null) return null;
                return new Option<int?>(stage.DescriptionText, stage.Id);
            });
    }
}