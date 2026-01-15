namespace AutodealerCrm.Apps.Views;

public class CustomerLeadsCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int customerId) : ViewBase
{
    private record LeadCreateRequest
    {
        [Required]
        public string Notes { get; init; } = "";

        public int SourceChannelId { get; init; }
        public int LeadIntentId { get; init; }
        public int LeadStageId { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var lead = UseState(() => new LeadCreateRequest());

        UseEffect(() =>
        {
            var leadId = CreateLead(factory, lead.Value);
            refreshToken.Refresh(leadId);
        }, [lead]);

        return lead
            .ToForm()
            .Builder(e => e.Notes, e => e.ToTextAreaInput())
            .Builder(e => e.SourceChannelId, e => e.ToAsyncSelectInput<int>(QuerySourceChannels, LookupSourceChannel, placeholder: "Select Source Channel"))
            .Builder(e => e.LeadIntentId, e => e.ToAsyncSelectInput<int>(QueryLeadIntents, LookupLeadIntent, placeholder: "Select Lead Intent"))
            .Builder(e => e.LeadStageId, e => e.ToAsyncSelectInput<int>(QueryLeadStages, LookupLeadStage, placeholder: "Select Lead Stage"))
            .ToDialog(isOpen, title: "Create Lead", submitTitle: "Create");
    }

    private int CreateLead(AutodealerCrmContextFactory factory, LeadCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var lead = new Lead()
        {
            CustomerId = customerId,
            Notes = request.Notes,
            SourceChannelId = request.SourceChannelId,
            LeadIntentId = request.LeadIntentId,
            LeadStageId = request.LeadStageId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Leads.Add(lead);
        db.SaveChanges();

        return lead.Id;
    }

    private static QueryResult<Option<int>[]> QuerySourceChannels(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QuerySourceChannels), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.SourceChannels
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupSourceChannel(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupSourceChannel), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var channel = await db.SourceChannels.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (channel == null) return null;
                return new Option<int>(channel.DescriptionText, channel.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryLeadIntents(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryLeadIntents), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadIntents
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupLeadIntent(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupLeadIntent), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var intent = await db.LeadIntents.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (intent == null) return null;
                return new Option<int>(intent.DescriptionText, intent.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryLeadStages(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryLeadStages), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.LeadStages
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupLeadStage(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupLeadStage), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var stage = await db.LeadStages.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (stage == null) return null;
                return new Option<int>(stage.DescriptionText, stage.Id);
            });
    }
}