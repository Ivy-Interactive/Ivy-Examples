namespace Taskly.Models;

/// <summary>
/// Reusable modal component for adding backlog items (Epic, Story, Task, Bug)
/// </summary>
public static class BacklogItemFormModal
{
    /// <summary>
    /// Build a modal for adding a backlog item with title and description
    /// </summary>
    public static object Build(
        string title,
        IState<string> itemTitle,
        IState<string> itemDescription,
        string itemType,
        Action onCancel,
        Action onSubmit,
        IState<int>? storyPoints = null,
        IState<Models.IssueType>? issueTypeSelect = null)
    {
        return new FloatingPanel(
            new Card(
                Layout.Vertical(
                    Text.H3(title),
                    itemTitle.ToTextInput().Placeholder("Enter title..."),
                    itemDescription.ToTextInput().Placeholder("Enter description..."),

                    // Optional story points input
                    storyPoints != null ?
                        storyPoints.ToNumberInput().Min(1).Max(21) : null,

                    // Optional issue type selector (for Task/Bug selection)
                    issueTypeSelect != null ?
                        new SelectInput<Models.IssueType>(
                            value: issueTypeSelect.Value,
                            onChange: e => { issueTypeSelect.Set(e.Value); return ValueTask.CompletedTask; },
                            options: new[] {
                                new Option<Models.IssueType>("Task", Models.IssueType.Task),
                                new Option<Models.IssueType>("Bug", Models.IssueType.Bug)
                            }
                        ) : null,

                    Layout.Horizontal(
                        new Button("Cancel", onCancel).Secondary(),
                        new Button("Add Item", onSubmit).Primary()
                    ).Gap(8)
                )
            )
        );
    }
}
