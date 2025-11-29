using Task = AutodealerCrm.Connections.AutodealerCrm.Task;

namespace AutodealerCrm.Apps.Views;

public class LeadTasksBlade(int leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();
        var tasks = this.UseState<Task[]?>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            await using var db = factory.CreateDbContext();
            tasks.Set(await db.Tasks.Include(t => t.Manager).Where(t => t.LeadId == leadId).ToArrayAsync());
        }, [ EffectTrigger.AfterInit(), refreshToken ]);

        Action OnDelete(int id)
        {
            return () =>
            {
                showAlert("Are you sure you want to delete this task?", result =>
                {
                    if (result.IsOk())
                    {
                        Delete(factory, id);
                        refreshToken.Refresh();
                    }
                }, "Delete Task", AlertButtonSet.OkCancel);
            };
        }

        if (tasks.Value == null) return null;

        var table = tasks.Value.Select(t => new
            {
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate?.ToString("d") ?? "N/A",
                Completed = t.Completed == true ? "Yes" : "No",
                Manager = t.Manager.Name,
                _ = Layout.Horizontal().Gap(2)
                    | Icons.Ellipsis
                        .ToButton()
                        .Ghost()
                        .WithDropDown(MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete(t.Id)))
                    | Icons.ChevronRight
                        .ToButton()
                        .Outline()
                        .Tooltip("Edit")
                        .ToTrigger((isOpen) => new LeadTasksEditSheet(isOpen, refreshToken, t.Id))
            })
            .ToTable()
            .RemoveEmptyColumns();

        var addBtn = new Button("Add Task").Icon(Icons.Plus).Outline()
            .ToTrigger((isOpen) => new LeadTasksCreateDialog(isOpen, refreshToken, leadId));

        return new Fragment()
               | BladeHelper.WithHeader(addBtn, table)
               | alertView;
    }

    public void Delete(AutodealerCrmContextFactory factory, int taskId)
    {
        using var db2 = factory.CreateDbContext();
        db2.Tasks.Remove(db2.Tasks.Single(t => t.Id == taskId));
        db2.SaveChanges();
    }
}