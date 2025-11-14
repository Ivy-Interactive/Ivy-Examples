namespace Taskly.Models;

public class StoryCard
{
    public static Card Build(
        BacklogItem story,
        int tasksCount,
        bool isInSprint,
        bool currentSprintExists,
        Action? onAddTaskBug = null,
        Action? onAddToSprint = null,
        Action? onRemoveFromSprint = null,
        Action? onDelete = null,
        object? nestedTasks = null,
        bool showActions = true)
    {
        return new Card(
            Layout.Vertical(
                // Title with badge
                Layout.Horizontal(
                    CardHelpers.GetIssueTypeBadge(story.Type),
                    Text.Strong(story.Title)
                ).Gap(4).Width(Size.Auto()),

                // Badges and buttons
                Layout.Horizontal(
                    new Badge($"{tasksCount} tasks").Secondary(),
                    new Badge($"{story.StoryPoints} pts").Primary(),

                    isInSprint ?
                        new Badge("In Sprint").Secondary() :
                        new Badge("Backlog").Outline(),

                    showActions && onAddTaskBug != null ?
                        new Button("+ Add Task/Bug", onAddTaskBug).Primary().Small() : null,

                    showActions && !isInSprint && currentSprintExists && onAddToSprint != null ?
                        new Button("Add to Sprint", onAddToSprint).Primary().Small() :
                    showActions && isInSprint && onRemoveFromSprint != null ?
                        new Button("Remove from Sprint", onRemoveFromSprint).Secondary().Small() :
                    null,

                    showActions && onDelete != null ?
                        new Button("Delete", onDelete).Destructive().Small() : null
                ),

                // Story description
                !string.IsNullOrEmpty(story.Description) ? Text.P(story.Description).Width(Size.Auto()) : null,

                // Nested Tasks/Bugs section
                nestedTasks
            )
        ).Width(Size.MinContent());
    }
}
