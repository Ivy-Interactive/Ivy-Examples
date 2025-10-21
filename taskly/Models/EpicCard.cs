namespace Taskly.Models;

public class EpicCard
{
    public static Card Build(
        BacklogItem epic,
        int storiesCount,
        Action? onOpen = null,
        Action? onDelete = null,
        bool showActions = true)
    {
        return new Card(
            Layout.Vertical(
                // Title with badge
                Layout.Horizontal(
                    CardHelpers.GetIssueTypeBadge(epic.Type),
                    Text.Strong(epic.Title)
                ),

                // Badges and buttons
                Layout.Horizontal(
                    // Story count indicator
                    new Badge($"{storiesCount} stories").Secondary(),

                    // Open Epic button
                    showActions && onOpen != null ?
                        new Button("Open", onOpen).Primary().Small() : null,

                    // Delete button
                    showActions && onDelete != null ?
                        new Button("Delete", onDelete).Destructive().Small() : null
                )
            ).Gap(4)
        ).Width(Size.MinContent());
    }
}
