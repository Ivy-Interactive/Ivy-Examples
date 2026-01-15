namespace AutodealerCrm.Apps.Views;

public class LeadTasksEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int taskId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var task = UseState(() => factory.CreateDbContext().Tasks.FirstOrDefault(e => e.Id == taskId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            task.Value.UpdatedAt = DateTime.UtcNow;
            db.Tasks.Update(task.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [task]);

        return task
            .ToForm()
            .Builder(e => e.Title, e => e.ToTextInput())
            .Builder(e => e.Description, e => e.ToTextAreaInput())
            .Builder(e => e.DueDate, e => e.ToDateInput())
            .Builder(e => e.Completed, e => e.ToNumberInput())
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput<int?>(QueryManagers, LookupManager, placeholder: "Select Manager"))
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int?>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Task");
    }

    private static QueryResult<Option<int?>[]> QueryManagers(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryManagers), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Users
                        .Where(e => e.Name.Contains(query))
                        .Select(e => new { e.Id, e.Name })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupManager(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupManager), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (manager == null) return null;
                return new Option<int?>(manager.Name, manager.Id);
            });
    }

    private static QueryResult<Option<int?>[]> QueryLeads(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryLeads), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(e => e.Notes.Contains(query))
                        .Select(e => new { e.Id, e.Notes })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.Notes, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupLead(IViewContext context, int? id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupLead), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (lead == null) return null;
                return new Option<int?>(lead.Notes, lead.Id);
            });
    }
}