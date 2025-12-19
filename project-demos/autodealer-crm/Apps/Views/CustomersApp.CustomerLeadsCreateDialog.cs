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
            .Builder(e => e.SourceChannelId, e => e.ToAsyncSelectInput(QuerySourceChannels(factory), LookupSourceChannel(factory), placeholder: "Select Source Channel"))
            .Builder(e => e.LeadIntentId, e => e.ToAsyncSelectInput(QueryLeadIntents(factory), LookupLeadIntent(factory), placeholder: "Select Lead Intent"))
            .Builder(e => e.LeadStageId, e => e.ToAsyncSelectInput(QueryLeadStages(factory), LookupLeadStage(factory), placeholder: "Select Lead Stage"))
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

    private static AsyncSelectQueryDelegate<int?> QuerySourceChannels(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.SourceChannels
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupSourceChannel(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var channel = await db.SourceChannels.FirstOrDefaultAsync(e => e.Id == id);
            if (channel == null) return null;
            return new Option<int?>(channel.DescriptionText, channel.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeadIntents(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.LeadIntents
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLeadIntent(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var intent = await db.LeadIntents.FirstOrDefaultAsync(e => e.Id == id);
            if (intent == null) return null;
            return new Option<int?>(intent.DescriptionText, intent.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeadStages(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.LeadStages
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLeadStage(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var stage = await db.LeadStages.FirstOrDefaultAsync(e => e.Id == id);
            if (stage == null) return null;
            return new Option<int?>(stage.DescriptionText, stage.Id);
        };
    }
}