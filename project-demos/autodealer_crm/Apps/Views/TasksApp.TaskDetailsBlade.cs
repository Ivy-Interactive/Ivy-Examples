using Task = AutodealerCrm.Connections.AutodealerCrm.Task;

namespace AutodealerCrm.Apps.Views;

public class TaskDetailsBlade(int taskId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var task = UseState<Task?>(() => null!);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            task.Set(await db.Tasks
                .Include(e => e.Lead)
                .Include(e => e.Manager)
                .SingleOrDefaultAsync(e => e.Id == taskId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (task.Value == null) return null;

        var taskValue = task.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this task?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Task", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(onDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger(isOpen => new TaskEditSheet(isOpen, refreshToken, taskId));

        var detailsCard = new Card(
            content: new {
                Id = taskValue.Id,
                Title = taskValue.Title,
                Description = taskValue.Description,
                Lead = taskValue.Lead?.Customer?.FirstName + " " + taskValue.Lead?.Customer?.LastName,
                Manager = taskValue.Manager.Name,
                DueDate = taskValue.DueDate?.ToString("d") ?? "N/A",
                Completed = taskValue.Completed == 1 ? "Yes" : "No"
            }
            .ToDetails()
            .MultiLine(e => e.Description)
            .RemoveEmpty(),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                | dropDown
                | editBtn
        ).Title("Task Details").Width(Size.Units(100));

        return new Fragment() 
            | (Layout.Vertical() | detailsCard)
            | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var task = db.Tasks.FirstOrDefault(e => e.Id == taskId)!;
        db.Tasks.Remove(task);
        db.SaveChanges();
    }
}