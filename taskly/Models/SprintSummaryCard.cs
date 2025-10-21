namespace Taskly.Models;

/// <summary>
/// Reusable component for displaying sprint summary information
/// </summary>
public static class SprintSummaryCard
{
    /// <summary>
    /// Build a sprint summary card showing sprint name, goal, and task counts
    /// </summary>
    /// <param name="sprint">The sprint to display</param>
    /// <param name="allTasks">All tasks in the sprint</param>
    /// <param name="todoTasks">Tasks in To Do status</param>
    /// <param name="inProgressTasks">Tasks in In Progress status</param>
    /// <param name="doneTasks">Tasks in Done status</param>
    /// <param name="archiveButton">Optional archive button action (null for disabled)</param>
    /// <returns>Card component with sprint summary</returns>
    public static object Build(
        Sprint sprint,
        int allTasksCount,
        int todoTasksCount,
        int inProgressTasksCount,
        int doneTasksCount,
        Action? archiveButton = null)
    {
        return new Card(
            Layout.Vertical(
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.H3($"Active Sprint: {sprint.Name}"),
                        !string.IsNullOrEmpty(sprint.Goal) ?
                            Text.P($"Goal: {sprint.Goal}") : null,
                        Text.Small($"Tasks/Bugs: {allTasksCount} | " +
                                  $"To Do: {todoTasksCount} | " +
                                  $"In Progress: {inProgressTasksCount} | " +
                                  $"Done: {doneTasksCount}")
                    ),
                    archiveButton != null ?
                        new Button("Archive Sprint", archiveButton).Secondary() :
                        new Button("Archive Sprint", () => { }).Secondary()
                )
            )
        ).Width(Size.Units(200));
    }
}
