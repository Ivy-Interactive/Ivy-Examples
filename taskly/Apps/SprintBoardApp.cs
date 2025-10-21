namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Database;
using Taskly.Connections;

[App(icon: Icons.Kanban)]
public class SprintBoardApp : ViewBase
{
    public override object Build()
    {
        // Local state loaded from database
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState<Sprint>(() => null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);
        var isLoading = UseState(true);
        var spaceingBoardColumns = 120;

        // Create signal to notify other apps to refresh
        var refreshSignal = Context.CreateSignal<RefreshDataSignal, bool, bool>();

        // Listen for refresh signals from other apps
        var refreshReceiver = Context.UseSignal<RefreshDataSignal, bool, bool>();

        UseEffect(() => refreshReceiver.Receive(value =>
        {
            ReloadData();
            return true;
        }));

        // Load data from database on mount
        UseEffect(() =>
        {
            try
            {
                // Load backlog items (exclude tutorial items)
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Load current sprint (exclude tutorial sprints)
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: false);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }

                // Load archived sprints (exclude tutorial sprints)
                var allSprints = InitDatabase.GetAllSprints(isTutorial: false);
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);

                isLoading.Set(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                isLoading.Set(false);
            }
        });

        // Helper to reload data from database
        void ReloadData()
        {
            try
            {
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: false);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }
                else
                {
                    currentSprint.Set((Sprint)null!);
                }

                var allSprints = InitDatabase.GetAllSprints(isTutorial: false);
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading data: {ex.Message}");
            }
        }

        // Helper method to update item status in database
        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var item = backlogItems.Value.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                var updated = item with { Status = newStatus };
                InitDatabase.UpdateBacklogItem(updated.ToBacklogItemModel());
                ReloadData();
            }
        }

        async void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Archive the sprint in database
                var sprintModel = currentSprint.Value.ToSprintModel(isArchived: true);
                InitDatabase.UpdateSprint(sprintModel);
                ReloadData();

                // Notify other apps to refresh
                await refreshSignal.Send(true);
            }
        }

        if (isLoading.Value)
        {
            return new Card(Text.P("Loading..."));
        }

        // Get all items in sprint (stories only, since only stories are added to sprints)
        var sprintStories = backlogItems.Value.Where(item =>
            currentSprint.Value != null &&
            currentSprint.Value.ItemIds.Contains(item.Id) &&
            item.Type == IssueType.Story).ToImmutableArray();

        // Get all tasks/bugs that belong to stories in the sprint
        var allTasks = backlogItems.Value.Where(item =>
            (item.Type == IssueType.Task || item.Type == IssueType.Bug) &&
            sprintStories.Any(story => story.Id == item.ParentId)).ToImmutableArray();

        // Group tasks by status
        var todoTasks = allTasks.Where(item => item.Status == ItemStatus.Todo).ToArray();
        var inProgressTasks = allTasks.Where(item => item.Status == ItemStatus.InProgress).ToArray();
        var doneTasks = allTasks.Where(item => item.Status == ItemStatus.Done).ToArray();

        // Helper to build task/bug card
        object BuildTaskCard(BacklogItem task)
        {
            return new Card(
                Layout.Vertical(
                    // Header with issue type and title
                    Layout.Horizontal(
                        GetIssueTypeBadge(task.Type),
                        Text.Strong(task.Title).Width(Size.Grow())
                    ),

                    // Description if available
                    !string.IsNullOrEmpty(task.Description) ?
                        Text.P(task.Description) : null,

                    // Footer with story points and action buttons
                    Layout.Horizontal(
                        new Badge($"{task.StoryPoints} pts").Primary(),

                        // Status progression buttons
                        task.Status == ItemStatus.Todo ?
                            new Button("Start", () => UpdateItemStatus(task.Id, ItemStatus.InProgress)).Primary().Small() :
                        task.Status == ItemStatus.InProgress ?
                            Layout.Horizontal(
                                new Button("← Reverse", () => UpdateItemStatus(task.Id, ItemStatus.Todo)).Destructive().Small(),
                                new Button("Complete", () => UpdateItemStatus(task.Id, ItemStatus.Done)).Primary().Small()
                            ).Gap(4) :
                        task.Status == ItemStatus.Done ?
                            new Button("← Reverse", () => UpdateItemStatus(task.Id, ItemStatus.InProgress)).Destructive().Small() :
                        null
                    ).Gap(8)
                ).Gap(8)
            ).Width(Size.Grow());
        }

        // Helper to build Epic/Story hierarchy with kanban columns inside each story
        object BuildHierarchyPanel()
        {
            // Group stories by their parent epic
            var epicGroups = sprintStories
                .GroupBy(story => story.ParentId)
                .OrderBy(g => g.Key);

            return epicGroups.Any() ?
                Layout.Vertical(
                    epicGroups.Select(epicGroup =>
                    {
                        var epic = backlogItems.Value.FirstOrDefault(e => e.Id == epicGroup.Key);
                        var stories = epicGroup.ToArray();

                        return new Card(
                            Layout.Vertical(
                                // Epic header
                                Layout.Horizontal(
                                    GetIssueTypeBadge(IssueType.Epic),
                                    Text.Strong(epic?.Title ?? "Unknown Epic").Width(Size.Grow())
                                ),

                                // Stories within epic
                                Layout.Vertical(
                                    stories.Select(story =>
                                    {
                                        // Get tasks for this story grouped by status
                                        var storyTasks = allTasks.Where(t => t.ParentId == story.Id).ToArray();
                                        var todoTasks = storyTasks.Where(t => t.Status == ItemStatus.Todo).ToArray();
                                        var inProgressTasks = storyTasks.Where(t => t.Status == ItemStatus.InProgress).ToArray();
                                        var doneTasks = storyTasks.Where(t => t.Status == ItemStatus.Done).ToArray();

                                        return new Card(
                                            Layout.Vertical(
                                                // Story header
                                                Layout.Horizontal(
                                                    GetIssueTypeBadge(IssueType.Story),
                                                    Text.Strong(story.Title).Width(Size.Grow())
                                                ),

                                                // Kanban columns for this story's tasks
                                                Layout.Horizontal(
                                                    // To Do Column
                                                    Layout.Vertical(
                                                        Text.H4($"To Do ({todoTasks.Length})"),
                                                        todoTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                todoTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Units(spaceingBoardColumns)),

                                                    // In Progress Column
                                                    Layout.Vertical(
                                                        Text.H4($"In Progress ({inProgressTasks.Length})"),
                                                        inProgressTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                inProgressTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Units(spaceingBoardColumns)),

                                                    // Done Column
                                                    Layout.Vertical(
                                                        Text.H4($"Done ({doneTasks.Length})"),
                                                        doneTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                doneTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Units(spaceingBoardColumns))
                                                ).Gap(8)
                                            ).Gap(8)
                                        );
                                    }).ToArray()
                                ).Gap(4)
                            ).Gap(8)
                        );
                    }).ToArray()
                ).Gap(8) :
                new Card(Text.P("No stories in sprint"));
        }

        Badge GetIssueTypeBadge(IssueType type)
        {
            return type switch
            {
                IssueType.Task => new Badge("Task").Secondary(),
                IssueType.Bug => new Badge("Bug").Destructive(),
                IssueType.Story => new Badge("Story").Primary(),
                IssueType.Epic => new Badge("Epic").Outline(),
                _ => new Badge(type.ToString()).Secondary()
            };
        }

        return Layout.Vertical(
            Text.H2("Sprint Board"),

            // Show current sprint info or message if no sprint
            currentSprint.Value != null ?
                SprintSummaryCard.Build(
                    sprint: currentSprint.Value,
                    allTasksCount: allTasks.Length,
                    todoTasksCount: todoTasks.Length,
                    inProgressTasksCount: inProgressTasks.Length,
                    doneTasksCount: doneTasks.Length,
                    archiveButton: ArchiveSprint  // Active archive button
                ) :
                new Card(
                    Text.P("No active sprint. Create a sprint in the Planning app to get started.")
                ).Width(Size.Fit()),

            // Hierarchical Kanban board with columns inside each story
            currentSprint.Value != null && sprintStories.Length > 0 ?
                BuildHierarchyPanel() :
            currentSprint.Value != null ?
                new Card(
                    Text.P("No stories in sprint yet. Add stories from the Planning app.")
                ).Width(Size.Fit()) : null
        ).Gap(16);
    }
}
