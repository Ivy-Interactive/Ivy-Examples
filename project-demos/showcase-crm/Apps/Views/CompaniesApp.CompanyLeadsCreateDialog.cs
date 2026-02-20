namespace ShowcaseCrm.Apps.Views;

public class CompanyLeadsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? companyId) : ViewBase
{
    private record LeadCreateRequest
    {
        [Required]
        public int StatusId { get; init; }

        public string? Source { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var lead = UseState(() => new LeadCreateRequest());

        return lead
            .ToForm()
            .Builder(e => e.StatusId, e => e.ToAsyncSelectInput(UseLeadStatusSearch, UseLeadStatusLookup, placeholder: "Select Status"))
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
            CompanyId = companyId,
            StatusId = request.StatusId,
            Source = request.Source,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return lead.Id;
    }

    private static QueryResult<Option<int?>[]> UseLeadStatusSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadStatusSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadStatuses
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> UseLeadStatusLookup(IViewContext context, int? id)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadStatusLookup), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var status = await db.LeadStatuses.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (status == null) return null;
                return new Option<int?>(status.DescriptionText, status.Id);
            });
    }
}