namespace Taskly.Models;

public class TaskCard
{
    public static Card Build(
        BacklogItem task,
        Action? onDelete = null,
        bool showActions = true)
    {
        return new Card(
            Layout.Vertical(
                // Title with badge
                Layout.Horizontal(
                    CardHelpers.GetIssueTypeBadge(task.Type),
                    Text.P(task.Title)
                ).Gap(4),

                // Badges and buttons
                Layout.Horizontal(
                    new Badge($"{task.StoryPoints} pts").Primary(),
                    showActions && onDelete != null ?
                        new Button("Delete", onDelete).Destructive().Small() : null
                ),

                // Task description
                !string.IsNullOrEmpty(task.Description) ? Text.P(task.Description) : null
            ).Gap(4)
        );
    }
}
