using Task = AutodealerCrm.Connections.AutodealerCrm.Task;

namespace AutodealerCrm.Apps.Views;

public class UserTasksCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int managerId) : ViewBase
{
    private record TaskCreateRequest
    {
        [Required]
        public string Title { get; init; } = "";

        public string? Description { get; init; }

        public DateTime? DueDate { get; init; }

        [Required]
        public int Completed { get; init; } = 0;
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
            .Builder(e => e.Title, e => e.ToTextInput())
            .Builder(e => e.Description, e => e.ToTextAreaInput())
            .Builder(e => e.DueDate, e => e.ToDateInput())
            .Builder(e => e.Completed, e => e.ToFeedbackInput())
            .ToDialog(isOpen, title: "Create Task", submitTitle: "Create");
    }

    private int CreateTask(AutodealerCrmContextFactory factory, TaskCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var task = new Task
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Completed = request.Completed,
            ManagerId = managerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        db.SaveChanges();

        return task.Id;
    }
}