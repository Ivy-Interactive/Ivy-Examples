namespace ShowcaseCrm.Apps.Views;

public class DealCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record DealCreateRequest
    {
        [Required]
        public int CompanyId { get; init; }

        [Required]
        public int ContactId { get; init; }

        public int? LeadId { get; init; }

        [Required]
        public decimal Amount { get; init; }

        public DateTime? CloseDate { get; init; }

        [Required]
        public int StageId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var deal = UseState(() => new DealCreateRequest());
        var prevCompanyId = UseState(0);

        UseEffect(() =>
        {
            if (prevCompanyId.Value != 0 && prevCompanyId.Value != deal.Value.CompanyId)
                deal.Set(deal.Value with { ContactId = 0, LeadId = null });
            prevCompanyId.Set(deal.Value.CompanyId);
        }, EffectTrigger.OnStateChange(deal));

        return deal
            .ToForm()
            .Place(e => e.CompanyId)
            .Place(e => e.ContactId)
            .Place(e => e.LeadId)
            .Place(e => e.StageId)
            .Place(e => e.Amount)
            .Place(e => e.CloseDate)
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(
                (ctx, q) => UseContactSearchForCompany(ctx, q, deal.Value.CompanyId),
                UseContactLookup,
                placeholder: "Select Contact"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(
                (ctx, q) => UseLeadSearchForCompany(ctx, q, deal.Value.CompanyId),
                UseLeadLookup,
                placeholder: "Select Lead"))
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.CloseDate, e => e.ToDateInput())
            .Builder(e => e.StageId, e => e.ToAsyncSelectInput(UseStageSearch, UseStageLookup, placeholder: "Select Stage"))
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
            ContactId = request.ContactId,
            LeadId = request.LeadId,
            Amount = request.Amount,
            CloseDate = request.CloseDate,
            StageId = request.StageId,
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
        var searchTerm = query?.Trim() ?? "";
        return context.UseQuery(
            key: (nameof(UseCompanySearch), searchTerm),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Companies
                        .Where(e => string.IsNullOrEmpty(searchTerm) || (e.Name != null && e.Name.Contains(searchTerm)))
                        .OrderBy(e => e.Name)
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

    private static QueryResult<Option<int?>[]> UseContactSearchForCompany(IViewContext context, string query, int companyId)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        var searchTerm = query?.Trim() ?? "";
        return context.UseQuery(
            key: (nameof(UseContactSearchForCompany), companyId, searchTerm),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var linq = db.Contacts.AsQueryable();
                if (companyId != 0)
                    linq = linq.Where(c => c.CompanyId == companyId);
                if (!string.IsNullOrEmpty(searchTerm))
                    linq = linq.Where(c => (c.FirstName != null && c.FirstName.Contains(searchTerm)) || (c.LastName != null && c.LastName.Contains(searchTerm)));
                return (await linq
                        .OrderBy(c => c.FirstName)
                        .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName })
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
                var contact = await db.Contacts.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (contact == null) return null;
                return new Option<int?>(contact.FirstName + " " + contact.LastName, contact.Id);
            });
    }

    private static QueryResult<Option<int?>[]> UseLeadSearchForCompany(IViewContext context, string query, int companyId)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        var searchTerm = query?.Trim() ?? "";
        return context.UseQuery(
            key: (nameof(UseLeadSearchForCompany), companyId, searchTerm),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var linq = db.Leads.AsQueryable();
                if (companyId != 0)
                    linq = linq.Where(l => l.CompanyId == companyId);
                if (!string.IsNullOrEmpty(searchTerm))
                    linq = linq.Where(l => l.Source == null || l.Source.Contains(searchTerm));
                return (await linq
                        .OrderBy(l => l.Source)
                        .Select(l => new { l.Id, l.Source })
                        .ToArrayAsync(ct))
                    .Select(l => new Option<int?>(l.Source ?? "", l.Id))
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
                var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (lead == null) return null;
                return new Option<int?>(lead.Source, lead.Id);
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