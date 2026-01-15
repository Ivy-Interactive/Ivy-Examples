namespace AutodealerCrm.Apps.Views;

public class TaskListBlade : ViewBase
{
    private record TaskListRecord(int Id, string Title, string? ManagerName);

    public override object? Build()
    {
        var blades = UseContext<IBladeService>();
        var factory = UseService<AutodealerCrmContextFactory>();
        var refreshToken = this.UseRefreshToken();

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue is int taskId)
            {
                blades.Pop(this, true);
                blades.Push(this, new TaskDetailsBlade(taskId));
            }
        }, [refreshToken]);

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var task = (TaskListRecord)e.Sender.Tag!;
            blades.Push(this, new TaskDetailsBlade(task.Id), task.Title);
        });

        ListItem CreateItem(TaskListRecord record) =>
            new(title: record.Title, subtitle: record.ManagerName, onClick: onItemClicked, tag: record);

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Create Task").ToTrigger((isOpen) => new TaskCreateDialog(isOpen, refreshToken));

        return new FilteredListView<TaskListRecord>(
            fetchRecords: (filter) => FetchTasks(factory, filter),
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private async Task<TaskListRecord[]> FetchTasks(AutodealerCrmContextFactory factory, string filter)
    {
        await using var db = factory.CreateDbContext();

        var linq = db.Tasks
            .Include(t => t.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            linq = linq.Where(t => t.Title.Contains(filter) || t.Manager.Name.Contains(filter));
        }

        return await linq
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new TaskListRecord(t.Id, t.Title, t.Manager.Name))
            .ToArrayAsync();
    }
}