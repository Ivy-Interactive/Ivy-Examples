namespace ShowcaseCrm.Apps.Views;

public class CompanyDealsEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int dealId, Action? onSuccess = null) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<ShowcaseCrmContextFactory>();
        var queryService = UseService<IQueryService>();

        var dealQuery = UseQuery(
            key: (typeof(Deal), dealId),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return await db.Deals.FirstAsync(e => e.Id == dealId, ct);
            },
            tags: [(typeof(Deal), dealId)]
        );

        if (dealQuery.Loading || dealQuery.Value == null)
            return Skeleton.Form().ToSheet(isOpen, "Edit Deal");

        return dealQuery.Value
            .ToForm()
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.CloseDate, e => e.ToDateInput())
            .Builder(e => e.StageId, e => e.ToAsyncSelectInput(UseStageSearch, UseStageLookup, placeholder: "Select Stage"))
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(UseContactSearch, UseContactLookup, placeholder: "Select Contact"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(UseLeadSearch, UseLeadLookup, placeholder: "Select Lead"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit)
            .ToSheet(isOpen, "Edit Deal");

        async Task OnSubmit(Deal? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Deals.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(Deal), dealId));
            queryService.RevalidateByTag(typeof(Deal[]));
            queryService.RevalidateByTag((typeof(Company), request.CompanyId));
            onSuccess?.Invoke();
            refreshToken.Refresh(dealId);
        }
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
                        .Take(50)
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
                        .Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
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

    private static QueryResult<Option<int?>[]> UseLeadSearch(IViewContext context, string query)
    {
        var factory = context.UseService<ShowcaseCrmContextFactory>();
        return context.UseQuery(
            key: (nameof(UseLeadSearch), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(e => e.Source.Contains(query))
                        .Select(e => new { e.Id, e.Source })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Source, e.Id))
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
}