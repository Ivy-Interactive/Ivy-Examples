namespace Taskly.Models;

/// <summary>
/// Reusable component for displaying an archived sprint with metrics and items
/// </summary>
public static class ArchivedSprintCard
{
    /// <summary>
    /// Build an archived sprint card showing sprint details, completion metrics, and items
    /// </summary>
    /// <param name="sprint">The archived sprint to display</param>
    /// <param name="sprintItems">All backlog items in this sprint</param>
    /// <param name="makeCurrentAction">Action for "Make Current Sprint" button (null for disabled)</param>
    /// <param name="deleteAction">Action for "Delete Sprint" button (null for disabled)</param>
    /// <param name="getIssueTypeBadge">Function to get badge for issue type</param>
    /// <returns>Card component with archived sprint information</returns>
    public static object Build(
        Sprint sprint,
        BacklogItem[] sprintItems,
        Action? makeCurrentAction,
        Action? deleteAction,
        Func<IssueType, Badge> getIssueTypeBadge)
    {
        var completedItems = sprintItems.Where(item => item.Status == ItemStatus.Done).Count();
        var totalPoints = sprintItems.Sum(item => item.StoryPoints);
        var completedPoints = sprintItems.Where(item => item.Status == ItemStatus.Done).Sum(item => item.StoryPoints);

        return new Card(
            Layout.Vertical(
                // Buttons at top-right
                Layout.Horizontal(
                    new Spacer().Width(Size.Grow()),
                    makeCurrentAction != null ?
                        new Button("Make Current Sprint", makeCurrentAction).Primary() :
                        new Button("Make Current Sprint", () => { }).Primary(),
                    deleteAction != null ?
                        new Button("Delete Sprint", deleteAction).Destructive() :
                        new Button("Delete Sprint", () => { }).Destructive()
                ).Gap(2),

                // Sprint info (full width)
                Layout.Vertical(
                    Text.H3($"{sprint.Name}"),
                    !string.IsNullOrEmpty(sprint.Goal) ?
                        Text.P($"Goal: {sprint.Goal}") : null,
                    Text.Small($"Duration: {sprint.StartDate:MMM dd, yyyy} - {sprint.EndDate:MMM dd, yyyy}"),
                    Layout.Horizontal(
                        new Badge($"{completedItems}/{sprintItems.Length} items completed").Primary(),
                        new Badge($"{completedPoints}/{totalPoints} points completed").Secondary()
                    ).Gap(4),
                    sprintItems.Length > 0 ?
                        Layout.Vertical(
                            Text.H4("Sprint Items:"),
                            Layout.Vertical(
                                sprintItems
                                    .OrderBy(item => item.Id)
                                    .Select(item => new Card(
                                        Layout.Vertical(
                                            // Title with badge
                                            Layout.Horizontal(
                                                getIssueTypeBadge(item.Type),
                                                Text.Strong(item.Title)
                                            ).Gap(4),

                                            // Badges
                                            Layout.Horizontal(
                                                new Badge(item.Status.ToString()).Secondary(),
                                                new Badge($"{item.StoryPoints} pts").Primary()
                                            )
                                        ).Gap(4)
                                    ))
                                    .ToArray()
                            ).Gap(2)
                        ).Gap(4) :
                        Text.P("No items in this sprint.")
                )
            ).Gap(0)
        );
    }
}