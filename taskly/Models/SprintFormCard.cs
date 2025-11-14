namespace Taskly.Models;

public class SprintFormCard
{
    public static Card Build(
        IState<string> newSprintName,
        IState<string> newSprintGoal,
        Func<Event<Button>, ValueTask> createSprint)
    {
        return new Card(
            Layout.Vertical(
                Text.H3("Create New Sprint"),
                newSprintName.ToTextInput().Placeholder("Sprint name (e.g., Sprint 1)"),
                newSprintGoal.ToTextInput().Placeholder("Sprint goal (optional)"),
                new Button("Create Sprint", createSprint).Primary()
            )
        ).Width(Size.Units(100));
    }
}
