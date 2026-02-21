namespace ShowcaseCrm.Apps.Views;

public class CompanyDealsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int companyId, Action? onSuccess = null) : ViewBase
{
    private record DealCreateRequest
    {
        [Required]
        public int ContactId { get; init; }

        [Required]
        public int StageId { get; init; }

        public int? LeadId { get; init; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; init; }

        public DateTime? CloseDate { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();
        var dealState = UseState(() => new DealCreateRequest());

        return dealState
            .ToForm()
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(UseContactSearchForCompany, UseContactLookup, placeholder: "Select Contact"))
            .Builder(e => e.StageId, e => e.ToAsyncSelectInput(UseStageSearch, UseStageLookup, placeholder: "Select Stage"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(UseLeadSearchForCompany, UseLeadLookup, placeholder: "Select Lead"))
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.CloseDate, e => e.ToDateInput())
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create Deal", submitTitle: "Create");

        async Task OnSubmit(DealCreateRequest request)
        {
            var dealId = await CreateDealAsync(factory, request);
            queryService.RevalidateByTag(typeof(Deal[]));
            queryService.RevalidateByTag((typeof(Company), companyId));
            onSuccess?.Invoke();
            refreshToken.Refresh(dealId);
        }
    }

    private async Task<int> CreateDealAsync(ShowcaseCrmContextFactory factory, DealCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var deal = new Deal
        {
            CompanyId = companyId,
            ContactId = request.ContactId,
            StageId = request.StageId,
            LeadId = request.LeadId,
            Amount = request.Amount,
            CloseDate = request.CloseDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Deals.Add(deal);
        await db.SaveChangesAsync();

        return deal.Id;
    }

    private QueryResult<Option<int?>[]> UseContactSearchForCompany(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        var cid = companyId;
        return context.UseQuery(
            key: (nameof(UseContactSearchForCompany), cid, query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Contacts
                        .Where(c => c.CompanyId == cid && (c.FirstName.Contains(query) || c.LastName.Contains(query)))
                        .Select(c => new { c.Id, Name = $"{c.FirstName} {c.LastName}" })
                        .ToArrayAsync(ct))
                    .Select(c => new Option<int?>(c.Name, c.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseContactLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct);
                if (contact == null) return null;
                return new Option<int?>(contact.FirstName + " " + contact.LastName, contact.Id);
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
                        .Where(s => s.DescriptionText.Contains(query))
                        .Select(s => new { s.Id, s.DescriptionText })
                        .ToArrayAsync(ct))
                    .Select(s => new Option<int?>(s.DescriptionText, s.Id))
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
                var stage = await db.DealStages.FirstOrDefaultAsync(s => s.Id == id, ct);
                if (stage == null) return null;
                return new Option<int?>(stage.DescriptionText, stage.Id);
            });
    }

    private QueryResult<Option<int?>[]> UseLeadSearchForCompany(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        var cid = companyId;
        return context.UseQuery(
            key: (nameof(UseLeadSearchForCompany), cid, query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(l => l.CompanyId == cid && (l.Source != null && l.Source.Contains(query)))
                        .Select(l => new { l.Id, l.Source })
                        .ToArrayAsync(ct))
                    .Select(l => new Option<int?>(l.Source ?? "Unknown", l.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseLeadLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
                if (lead == null) return null;
                return new Option<int?>(lead.Source ?? "Unknown", lead.Id);
            });
    }
}