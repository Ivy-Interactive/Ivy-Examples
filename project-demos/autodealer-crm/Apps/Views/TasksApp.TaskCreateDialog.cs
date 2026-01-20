using Task = AutodealerCrm.Connections.AutodealerCrm.Task;

namespace AutodealerCrm.Apps.Views;

public class TaskCreateDialog(IState<bool> isOpen, RefreshToken refreshToken) : ViewBase
{
    private record TaskCreateRequest
    {
        [Required]
        public int LeadId { get; init; }

        [Required]
        public int ManagerId { get; init; }

        [Required]
        public string Title { get; init; } = "";

        public string? Description { get; init; }

        public DateTime? DueDate { get; init; }

        [Required]
        public bool Completed { get; init; }
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var taskState = UseState(() => new TaskCreateRequest());

        UseEffect(() =>
        {
            var taskId = CreateTask(factory, taskState.Value);
            refreshToken.Refresh(taskId);
        }, [taskState]);

        return taskState
            .ToForm()
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput<int>(QueryLeads, LookupLead, placeholder: "Select Lead"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput<int>(QueryManagers, LookupManager, placeholder: "Select Manager"))
            .Builder(e => e.Title, e => e.ToTextInput())
            .Builder(e => e.Description, e => e.ToTextAreaInput())
            .Builder(e => e.DueDate, e => e.ToDateTimeInput())
            .Builder(e => e.Completed, e => e.ToFeedbackInput())
            .ToDialog(isOpen, title: "Create Task", submitTitle: "Create");
    }

    private int CreateTask(AutodealerCrmContextFactory factory, TaskCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var task = new Task
        {
            LeadId = request.LeadId,
            ManagerId = request.ManagerId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Completed = request.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        db.SaveChanges();

        return task.Id;
    }

    private static QueryResult<Option<int>[]> QueryLeads(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryLeads), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Leads
                        .Where(e => e.Notes.Contains(query))
                        .Select(e => new { e.Id, e.Notes })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.Notes, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupLead(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupLead), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (lead == null) return null;
                return new Option<int>(lead.Notes, lead.Id);
            });
    }

    private static QueryResult<Option<int>[]> QueryManagers(IViewContext context, string query)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>[], (string, string)>(
            key: (nameof(QueryManagers), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Users
                        .Where(e => e.Name.Contains(query))
                        .Select(e => new { e.Id, e.Name })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int>(e.Name, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int>?> LookupManager(IViewContext context, int id)
    {
        var factory = context.UseService<AutodealerCrmContextFactory>();
        return context.UseQuery<Option<int>?, (string, int)>(
            key: (nameof(LookupManager), id),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (manager == null) return null;
                return new Option<int>(manager.Name, manager.Id);
            });
    }
}