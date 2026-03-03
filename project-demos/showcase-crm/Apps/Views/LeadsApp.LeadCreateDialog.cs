namespace ShowcaseCrm.Apps.Views;

public class LeadCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record LeadCreateRequest
    {
        [Required]
        public int? StatusId { get; init; } = null;

        public int? CompanyId { get; init; } = null;

        public int? ContactId { get; init; } = null;

        public string? Source { get; init; } = null;
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var lead = UseState(() => new LeadCreateRequest());

        return lead
            .ToForm()
            .Builder(e => e.StatusId, e => e.ToAsyncSelectInput(UseStatusSearch, UseStatusLookup, placeholder: "Select Status"))
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(UseContactSearch, UseContactLookup, placeholder: "Select Contact"))
            .Builder(e => e.Source, e => e.ToTextInput())
            .HandleSubmit(OnSubmit)
            .ToDialog(isOpen, title: "Create Lead", submitTitle: "Create");

        async Task OnSubmit(LeadCreateRequest request)
        {
            var leadId = await CreateLeadAsync(factory, request);
            refreshToken.Refresh(leadId);
        }
    }

    private async Task<int> CreateLeadAsync(ShowcaseCrmContextFactory factory, LeadCreateRequest request)
    {
        await using var db = factory.CreateDbContext();

        var lead = new Lead
        {
            StatusId = request.StatusId!.Value,
            CompanyId = request.CompanyId,
            ContactId = request.ContactId,
            Source = request.Source,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return lead.Id;
    }

    private static QueryResult<Option<int?>[]> UseStatusSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStatusSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadStatuses
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseStatusLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseStatusLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var status = await db.LeadStatuses.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (status == null) return null;
                return new Option<int?>(status.DescriptionText, status.Id);
            });
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

    private static QueryResult<Option<int?>[]> UseContactSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseContactSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Contacts
                        .Where(e => e.FirstName.Contains(query) || e.LastName.Contains(query))
                        .Select(e => new { e.Id, FullName = e.FirstName + " " + e.LastName })
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.FullName, e.Id))
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
}