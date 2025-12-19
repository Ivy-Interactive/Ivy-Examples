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
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(QueryLeads(factory), LookupLead(factory), placeholder: "Select Lead"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
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

    private static AsyncSelectQueryDelegate<int> QueryLeads(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Leads
                    .Where(e => e.Notes.Contains(query))
                    .Select(e => new { e.Id, e.Notes })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.Notes, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupLead(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var lead = await db.Leads.FirstOrDefaultAsync(e => e.Id == id);
            if (lead == null) return null;
            return new Option<int>(lead.Notes, lead.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int> QueryManagers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Users
                    .Where(e => e.Name.Contains(query))
                    .Select(e => new { e.Id, e.Name })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int> LookupManager(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            await using var db = factory.CreateDbContext();
            var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id);
            if (manager == null) return null;
            return new Option<int>(manager.Name, manager.Id);
        };
    }
}