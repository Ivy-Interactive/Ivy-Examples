namespace ShowcaseCrm.Apps.Views;

public class DealEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int dealId) : ViewBase
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

        var formBuilder = dealQuery.Value
            .ToForm()
            .Builder(e => e.Amount, e => e.ToMoneyInput().Currency("USD"))
            .Builder(e => e.CloseDate, e => e.ToDateInput())
            .Builder(e => e.StageId, e => e.ToAsyncSelectInput(UseStageSearch, UseStageLookup, placeholder: "Select Stage"))
            .Builder(e => e.CompanyId, e => e.ToAsyncSelectInput(UseCompanySearch, UseCompanyLookup, placeholder: "Select Company"))
            .Builder(e => e.ContactId, e => e.ToAsyncSelectInput(UseContactSearch, UseContactLookup, placeholder: "Select Contact"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(UseLeadSearch, UseLeadLookup, placeholder: "Select Lead"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .HandleSubmit(OnSubmit);

        var (onSubmit, formView, validationView, loading) = formBuilder.UseForm(Context);

        var deleteBtn = new Button("Delete", onClick: async _ =>
            {
                await DeleteDeal(dealId, factory, queryService);
                queryService.RevalidateByTag((typeof(Deal), dealId));
                queryService.RevalidateByTag(typeof(Deal[]));
                refreshToken.Refresh(dealId);
                isOpen.Set(false);
            })
            .Variant(ButtonVariant.Destructive)
            .Icon(Icons.Trash2)
            .WithConfirm("Are you sure you want to delete this deal?", "Delete Deal");

        var footer = Layout.Horizontal()
                | new Button("Save").Variant(ButtonVariant.Primary).Loading(loading).Disabled(loading)
                    .HandleClick(async _ =>
                    {
                        if (await onSubmit())
                            isOpen.Set(false);
                    })
                | deleteBtn
                | new Button("Cancel").Variant(ButtonVariant.Outline).HandleClick(_ => isOpen.Set(false))
                | validationView;

        var layout = new FooterLayout(footer, formView);
        return !isOpen.Value ? null : new Sheet(_ => isOpen.Set(false), layout, title: "Edit Deal");

        async Task OnSubmit(Deal? request)
        {
            if (request == null) return;
            await using var db = factory.CreateDbContext();
            request.UpdatedAt = DateTime.UtcNow;
            db.Deals.Update(request);
            await db.SaveChangesAsync();
            queryService.RevalidateByTag((typeof(Deal), dealId));
            queryService.RevalidateByTag(typeof(Deal[]));
            refreshToken.Refresh(dealId);
        }
    }

    private static async Task DeleteDeal(int id, ShowcaseCrmContextFactory factory, IQueryService queryService)
    {
        await using var db = factory.CreateDbContext();
        var deal = await db.Deals.SingleOrDefaultAsync(d => d.Id == id);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            await db.SaveChangesAsync();
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
                        .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
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