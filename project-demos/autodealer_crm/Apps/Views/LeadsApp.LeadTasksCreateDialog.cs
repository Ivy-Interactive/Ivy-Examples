using Task = AutodealerCrm.Connections.AutodealerCrm.Task;

namespace AutodealerCrm.Apps.Views;

public class LeadTasksCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int leadId) : ViewBase
{
    private record TaskCreateRequest
    {
        [Required]
        public string Title { get; init; } = "";

        public string? Description { get; init; }

        public DateTime? DueDate { get; init; }

        [Required]
        public int ManagerId { get; init; }
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
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
            .Builder(e => e.DueDate, e => e.ToDateInput())
            .ToDialog(isOpen, title: "Create Task", submitTitle: "Create");
    }

    private int CreateTask(AutodealerCrmContextFactory factory, TaskCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var task = new Task
        {
            LeadId = leadId,
            ManagerId = request.ManagerId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        db.SaveChanges();

        return task.Id;
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