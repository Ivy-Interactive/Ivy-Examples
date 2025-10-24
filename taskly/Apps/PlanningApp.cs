namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Database;
using Taskly.Connections;

[App(icon: Icons.Calendar)]
public class PlanningApp : ViewBase
{

    public override object? Build()
    {
        // Shared state management - loaded from database
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState(() => (Sprint)null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);
        var isLoading = UseState(true);

        // Create signal to notify other apps to refresh
        var refreshSignal = Context.CreateSignal<RefreshDataSignal, bool, bool>();

        // Listen for refresh signals from other apps
        var refreshReceiver = Context.UseSignal<RefreshDataSignal, bool, bool>();

        // Helper to reload data from database (used by signal receiver)
        void ReloadFromSignal()
        {
            try
            {
                // Reload backlog items
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Reload current sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: false);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }
                else
                {
                    currentSprint.Set((Sprint)null!);
                }

                // Reload archived sprints
                var allSprints = InitDatabase.GetAllSprints();
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading data from signal: {ex.Message}");
            }
        }

        UseEffect(() => refreshReceiver.Receive(value =>
        {
            ReloadFromSignal();
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

        return isLoading.Value ?
            new Card(Text.P("Loading...")) :
            BuildBacklogView(backlogItems, currentSprint, archivedSprints, refreshSignal);
    }

    private object BuildBacklogView(
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<Sprint> currentSprint,
        IState<ImmutableArray<Sprint>> archivedSprints,
        ISignalSender<bool, bool> refreshSignal)
    {
        // Hierarchy navigation state
        var openedEpic = UseState<BacklogItem?>(() => null);
        var openedStory = UseState<BacklogItem?>(() => null);

        // State for adding new items
        var newTitle = UseState("");
        var newDescription = UseState("");
        var newStoryPoints = UseState(1);
        var newIssueType = UseState(IssueType.Epic); // Default to Epic for top level

        // Modal state
        var isAddItemModalOpen = UseState(false);
        var addTaskToStory = UseState<BacklogItem?>(() => null); // Track which story's Add Task/Bug modal is open

        // State for creating new sprint
        var newSprintName = UseState("");
        var newSprintGoal = UseState("");

        // Helper to reload data from database and notify other apps
        async Task ReloadData()
        {
            try
            {
                // Reload backlog items (exclude tutorial items)
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Refresh opened epic/story references if they exist
                if (openedEpic.Value != null)
                {
                    var refreshedEpic = items.FirstOrDefault(i => i.Id == openedEpic.Value.Id);
                    if (refreshedEpic != null)
                    {
                        openedEpic.Set(refreshedEpic);
                    }
                }

                if (openedStory.Value != null)
                {
                    var refreshedStory = items.FirstOrDefault(i => i.Id == openedStory.Value.Id);
                    if (refreshedStory != null)
                    {
                        openedStory.Set(refreshedStory);
                    }
                }

                // Reload current sprint (exclude tutorial sprints)
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: false);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }
                else
                {
                    currentSprint.Set((Sprint)null!);
                }

                // Reload archived sprints (exclude tutorial sprints)
                var allSprints = InitDatabase.GetAllSprints(isTutorial: false);
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);

                // Notify other apps to refresh
                await refreshSignal.Send(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading data: {ex.Message}");
            }
        }

        // Helper methods for CRUD operations
        async void AddItem()
        {
            if (!string.IsNullOrWhiteSpace(newTitle.Value))
            {
                // Determine ParentId based on hierarchy context
                int? parentId = null;
                BacklogItem? parentItem = null;

                if (addTaskToStory.Value != null)
                {
                    parentId = addTaskToStory.Value.Id; // Task/Bug belongs to Story (modal within Epic view)
                    parentItem = addTaskToStory.Value;
                }
                else if (openedStory.Value != null)
                {
                    parentId = openedStory.Value.Id; // Task/Bug belongs to Story (Story detail view)
                    parentItem = openedStory.Value;
                }
                else if (openedEpic.Value != null)
                {
                    parentId = openedEpic.Value.Id; // Story belongs to Epic
                    parentItem = openedEpic.Value;
                }
                // else: Epic has no parent

                // If parent is in a sprint, inherit sprint status
                var parentInSprint = parentItem?.SprintId != null;

                var newItemModel = new BacklogItemModel
                {
                    Title = newTitle.Value,
                    Description = newDescription.Value,
                    StoryPoints = newStoryPoints.Value,
                    Priority = backlogItems.Value.Length + 1,
                    Status = parentInSprint ? ItemStatus.Todo.ToString() : ItemStatus.Backlog.ToString(),
                    Type = newIssueType.Value.ToString(),
                    ParentId = parentId,
                    SprintId = parentItem?.SprintId
                };

                // Save to database
                var created = InitDatabase.CreateBacklogItem(newItemModel);

                if (created != null)
                {
                    // Reload data from database
                    await ReloadData();

                    // Clear form
                    newTitle.Set("");
                    newDescription.Set("");
                    newStoryPoints.Set(1);

                    // Reset to default type based on context
                    if (addTaskToStory.Value != null)
                    {
                        newIssueType.Set(IssueType.Task);
                        addTaskToStory.Set((BacklogItem?)null); // Close the add task modal
                    }
                    else if (openedStory.Value != null)
                    {
                        newIssueType.Set(IssueType.Task);
                    }
                    else if (openedEpic.Value != null)
                    {
                        newIssueType.Set(IssueType.Story);
                    }
                    else
                    {
                        newIssueType.Set(IssueType.Epic);
                    }

                    // Close modal
                    isAddItemModalOpen.Set(false);
                }
            }
        }

        async ValueTask CreateSprint(Event<Button> _)
        {
            if (!string.IsNullOrWhiteSpace(newSprintName.Value))
            {
                var sprintModel = new SprintModel
                {
                    Name = newSprintName.Value,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(14), // 2-week sprint
                    Goal = newSprintGoal.Value,
                    ItemIds = new List<int>(),
                    IsArchived = 0
                };

                // Save to database
                var created = InitDatabase.CreateSprint(sprintModel);

                if (created != null)
                {
                    // Reload data
                    await ReloadData();

                    // Clear form
                    newSprintName.Set("");
                    newSprintGoal.Set("");
                }
            }
        }

        async void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                // Update item status to Backlog
                var item = backlogItems.Value.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    var updated = item with { Status = ItemStatus.Backlog, SprintId = null };
                    InitDatabase.UpdateBacklogItem(updated.ToBacklogItemModel());
                }

                // Update sprint by removing item ID
                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };
                InitDatabase.UpdateSprint(updatedSprint.ToSprintModel());

                // Reload data
                await ReloadData();
            }
        }

        async void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Archive the sprint in database
                var sprintModel = currentSprint.Value.ToSprintModel(isArchived: true);
                InitDatabase.UpdateSprint(sprintModel);

                // Reload data
                await ReloadData();
            }
        }

        async void DeleteSprint(IState<Sprint> sprint)
        {
            if (sprint.Value != null)
            {
                // Delete the sprint from database
                InitDatabase.DeleteSprint(sprint.Value.Id);

                // Also update all items in the sprint to remove sprint reference
                var itemsInSprint = backlogItems.Value.Where(item => item.SprintId == sprint.Value.Id).ToArray();
                foreach (var item in itemsInSprint)
                {
                    var updated = item with { Status = ItemStatus.Backlog, SprintId = null };
                    InitDatabase.UpdateBacklogItem(updated.ToBacklogItemModel());
                }

                // Reload data
                await ReloadData();
            }
        }

        // Conditional rendering based on hierarchy navigation
        return openedStory.Value != null ?
            // STORY DETAIL VIEW: Show Tasks and Bugs within the Story
            Layout.Vertical(
                isAddItemModalOpen.Value ?
                    BacklogItemFormModal.Build(
                        title: "Add Task/Bug to Story",
                        itemTitle: newTitle,
                        itemDescription: newDescription,
                        itemType: "Task/Bug",
                        onCancel: () => isAddItemModalOpen.Set(false),
                        onSubmit: AddItem,
                        storyPoints: newStoryPoints,
                        issueTypeSelect: newIssueType
                    ) : null,

                // Sprint management section
                BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, DeleteSprint, RemoveItemFromSprint),

                BuildStoryDetailView(openedStory.Value, openedEpic.Value!, backlogItems, openedEpic, openedStory, currentSprint, isAddItemModalOpen)
            ) :
        openedEpic.Value != null ?
            // EPIC DETAIL VIEW: Show Stories within the Epic
            Layout.Vertical(
                // Modal for adding Story to Epic
                isAddItemModalOpen.Value ?
                    BacklogItemFormModal.Build(
                        title: "Add Story to Epic",
                        itemTitle: newTitle,
                        itemDescription: newDescription,
                        itemType: "Story",
                        onCancel: () => isAddItemModalOpen.Set(false),
                        onSubmit: AddItem,
                        storyPoints: newStoryPoints
                    ) : null,

                // Modal for adding Task/Bug to Story (within Epic view)
                addTaskToStory.Value != null ?
                    BacklogItemFormModal.Build(
                        title: $"Add Task/Bug to Story: {addTaskToStory.Value.Title}",
                        itemTitle: newTitle,
                        itemDescription: newDescription,
                        itemType: "Task/Bug",
                        onCancel: () => addTaskToStory.Set((BacklogItem?)null),
                        onSubmit: AddItem,
                        storyPoints: newStoryPoints,
                        issueTypeSelect: newIssueType
                    ) : null,

                // Sprint management section
                BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, DeleteSprint, RemoveItemFromSprint),

                BuildEpicDetailView(openedEpic.Value, backlogItems, openedEpic, openedStory, currentSprint, archivedSprints, isAddItemModalOpen, addTaskToStory, newIssueType, refreshSignal)
            ) :
            // EPIC LIST VIEW: Show all Epics with Sprint Management
            Layout.Vertical(
                Layout.Horizontal(
                    Text.H2("Product Backlog").Width(Size.Grow()),
                    new Button("Clean Database", async () =>
                    {
                        await CleanDatabase.CleanAllTables();
                        await ReloadData();
                    }).Destructive().Small()
                ).Gap(8),

                isAddItemModalOpen.Value ?
                    BacklogItemFormModal.Build(
                        title: "Add Epic",
                        itemTitle: newTitle,
                        itemDescription: newDescription,
                        itemType: "Epic",
                        onCancel: () => isAddItemModalOpen.Set(false),
                        onSubmit: AddItem
                    ) : null,

                    // Sprint management section
                    BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, DeleteSprint, RemoveItemFromSprint),

                    BuildEpicListViewSection(backlogItems, openedEpic, currentSprint, archivedSprints, isAddItemModalOpen, newIssueType)
            ).Gap(4);
                
    }

    // Sprint management section - shown in all views
    private object BuildSprintManagementSection(
        IState<Sprint> currentSprint,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<string> newSprintName,
        IState<string> newSprintGoal,
        Func<Event<Button>, ValueTask> createSprint,
        Action archiveSprint,
        Action<IState<Sprint>> deleteSprint,
        Action<int> removeItemFromSprint)
    {
        return currentSprint.Value == null ?
            SprintFormCard.Build(newSprintName, newSprintGoal, createSprint) :
            new Card(
                Layout.Vertical(
                    // Sprint info (full width)
                    Layout.Vertical(
                        Text.H3($"Current Sprint: {currentSprint.Value.Name}"),
                        !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                            Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                        Text.Small($"Items in sprint: {currentSprint.Value.ItemIds.Length}")
                    ),

                    // Display sprint items
                    currentSprint.Value.ItemIds.Length > 0 ?
                        Layout.Vertical(
                            Text.H4("Sprint Items:"),
                            Layout.Vertical(
                                backlogItems.Value
                                    .Where(item => currentSprint.Value.ItemIds.Contains(item.Id))
                                    .OrderBy(x => x.Id)
                                    .Select(item => new Expandable(
                                            header: Layout.Vertical(
                                                // Title
                                                Text.Strong(item.Title),
                                                // Badges and buttons
                                                Layout.Horizontal(
                                                    CardHelpers.GetIssueTypeBadge(item.Type),
                                                    new Badge(item.Status.ToString()).Secondary(),
                                                    new Badge($"{item.StoryPoints} pts").Primary(),
                                                    new Button("Remove from Sprint", () => removeItemFromSprint(item.Id)).Destructive().Small()
                                                )
                                            ).Gap(2),
                                            content: !string.IsNullOrEmpty(item.Description) ?
                                                Text.P(item.Description) : Text.P("No description")
                                        )
                                    )
                                    .ToArray()
                            )
                        ).Gap(2) :
                        Text.P("No items in sprint yet. Use 'Add to Sprint' buttons below to add items."),

                    // Vertical spacer before buttons
                    Layout.Vertical().Height(Size.Units(8)),

                    // Buttons at bottom-right
                    Layout.Horizontal(
                        new Spacer().Width(Size.Grow()), // Spacer to push buttons right
                        new Button("Archive Sprint", archiveSprint).Secondary(),
                        new Button("Delete Sprint", () => deleteSprint(currentSprint)).Destructive()
                    ).Gap(2)
                ).Gap(2)
            ).Width(Size.Third());
    }

    // EPIC LIST VIEW: Shows only Epics (top-level items)
    private object BuildEpicListViewSection(IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<Sprint> currentSprint, IState<ImmutableArray<Sprint>> archivedSprints, IState<bool> isAddItemModalOpen, IState<IssueType> newIssueType)
    {
        // Get only Epics (items with no parent)
        var epics = backlogItems.Value.Where(item => item.Type == IssueType.Epic && item.ParentId == null).ToArray();

        void DeleteItem(int id)
        {
            InitDatabase.DeleteBacklogItem(id);

            // Reload data
            var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
            var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
            backlogItems.Set(items);
        }

        return Layout.Vertical(
            Text.H3($"Epics ({epics.Length})"),
            new Button("+ Add Epic", () => { newIssueType.Set(IssueType.Epic); isAddItemModalOpen.Set(true); }).Primary(),
            epics.Length == 0 ?
                new Card(Text.P("No epics yet. Click '+ Add Epic' to create your first epic.")).Width(Size.Fit()) :
                Layout.Vertical(
                    epics
                        .OrderBy(x => x.Id)
                        .Select(epic =>
                        {
                            var storiesCount = backlogItems.Value.Count(item => item.ParentId == epic.Id && item.Type == IssueType.Story);
                            return EpicCard.Build(
                                epic: epic,
                                storiesCount: storiesCount,
                                onOpen: () => openedEpic.Set(epic),
                                onDelete: () => DeleteItem(epic.Id),
                                showActions: true
                            );
                        }).ToArray()
                ).Gap(4)
        ).Gap(4);
    }

    // EPIC DETAIL VIEW: Shows Stories within an Epic
    private object BuildEpicDetailView(BacklogItem epic, IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<BacklogItem?> openedStory, IState<Sprint> currentSprint, IState<ImmutableArray<Sprint>> archivedSprints, IState<bool> isAddItemModalOpen, IState<BacklogItem?> addTaskToStory, IState<IssueType> newIssueType, ISignalSender<bool, bool> refreshSignal)
    {
        // Get Stories that belong to this Epic - use openedEpic.Value to get latest state
        var stories = backlogItems.Value.Where(item => item.ParentId == openedEpic.Value?.Id && item.Type == IssueType.Story).ToArray();

        // Helper to reload data from database and notify other apps
        async Task ReloadData()
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

                var allSprints = InitDatabase.GetAllSprints();
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);

                // Notify other apps to refresh
                await refreshSignal.Send(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading data: {ex.Message}");
            }
        }

        async void DeleteItem(int id)
        {
            InitDatabase.DeleteBacklogItem(id);
            await ReloadData();
        }

        async void AddItemToSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                // When adding a Story to sprint, also update status of all its child Tasks/Bugs to Todo
                var itemToAdd = backlogItems.Value.FirstOrDefault(i => i.Id == itemId);

                // Update the story status
                if (itemToAdd != null)
                {
                    var updatedStory = itemToAdd with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id };
                    InitDatabase.UpdateBacklogItem(updatedStory.ToBacklogItemModel());
                }

                // If it's a Story, also update all its child Tasks/Bugs to Todo status
                if (itemToAdd?.Type == IssueType.Story)
                {
                    var childTasks = backlogItems.Value.Where(item => item.ParentId == itemId).ToArray();

                    foreach (var task in childTasks)
                    {
                        var updatedTask = task with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id };
                        InitDatabase.UpdateBacklogItem(updatedTask.ToBacklogItemModel());
                    }
                }

                // Update sprint by adding item ID
                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Add(itemId)
                };
                InitDatabase.UpdateSprint(updatedSprint.ToSprintModel());

                // Reload data
                await ReloadData();
            }
        }

        async void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                // When removing a Story from sprint, also update status of all its child Tasks/Bugs back to Backlog
                var itemToRemove = backlogItems.Value.FirstOrDefault(i => i.Id == itemId);

                // Update the story status
                if (itemToRemove != null)
                {
                    var updatedStory = itemToRemove with { Status = ItemStatus.Backlog, SprintId = null };
                    InitDatabase.UpdateBacklogItem(updatedStory.ToBacklogItemModel());
                }

                // If it's a Story, also update all its child Tasks/Bugs back to Backlog status
                if (itemToRemove?.Type == IssueType.Story)
                {
                    var childTasks = backlogItems.Value.Where(item => item.ParentId == itemId).ToArray();
                    foreach (var task in childTasks)
                    {
                        var updatedTask = task with { Status = ItemStatus.Backlog, SprintId = null };
                        InitDatabase.UpdateBacklogItem(updatedTask.ToBacklogItemModel());
                    }
                }

                // Update sprint by removing item ID
                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };
                InitDatabase.UpdateSprint(updatedSprint.ToSprintModel());

                // Reload data
                await ReloadData();
            }
        }

        return Layout.Vertical(
            // Breadcrumb navigation
            Layout.Horizontal(
                new Button("← Back to Epics", () => openedEpic.Set((BacklogItem?)null)).Secondary().Small()
            ),
            // Epic details
            Text.H3($"Epic: {epic.Title}"),
            Layout.Vertical(
                !string.IsNullOrEmpty(epic.Description) ? Text.P("Goal: " + epic.Description) : null,
                Layout.Horizontal(
                    new Badge($"{stories.Length} stories").Secondary()
                )
            ).Width(Size.Units(100)),

            // Stories list
            Layout.Vertical(
                Text.H3($"Stories ({stories.Length})"),
                new Button("+ Add Story", () => { newIssueType.Set(IssueType.Story); isAddItemModalOpen.Set(true); }).Primary(),
                stories.Length == 0 ?
                new Card(Text.P("No stories yet. Click '+ Add Story' to create a story.")).Width(Size.Units(100)) :
                Layout.Vertical(
                    stories
                        .OrderBy(x => x.Id)
                        .Select(story =>
                        {
                            bool isInSprint = story.SprintId != null;
                            var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

                            // Build nested tasks
                            var nestedTasks = Layout.Vertical(
                                Layout.Vertical(
                                    tasks.OrderBy(t => t.Id).Select(task =>
                                        TaskCard.Build(
                                            task: task,
                                            onDelete: () => DeleteItem(task.Id),
                                            showActions: true
                                        )
                                    ).ToArray()
                                ).Gap(4)
                            ).Gap(4);

                            return StoryCard.Build(
                                story: story,
                                tasksCount: tasks.Length,
                                isInSprint: isInSprint,
                                currentSprintExists: currentSprint.Value != null,
                                onAddTaskBug: () => { addTaskToStory.Set(story); newIssueType.Set(IssueType.Task); },
                                onAddToSprint: () => AddItemToSprint(story.Id),
                                onRemoveFromSprint: () => RemoveItemFromSprint(story.Id),
                                onDelete: () => DeleteItem(story.Id),
                                nestedTasks: nestedTasks,
                                showActions: true
                            );
                        }).ToArray()
                )
            )
        ).Gap(0);
    }

    // STORY DETAIL VIEW: Shows Tasks and Bugs within a Story
    private object BuildStoryDetailView(BacklogItem story, BacklogItem epic, IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<BacklogItem?> openedStory, IState<Sprint> currentSprint, IState<bool> isAddItemModalOpen)
    {
        // Get Tasks and Bugs that belong to this Story
        var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

        void DeleteItem(int id)
        {
            InitDatabase.DeleteBacklogItem(id);

            // Reload data
            var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: false);
            var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
            backlogItems.Set(items);
        }

        return Layout.Vertical(
            // Breadcrumb navigation
            Layout.Horizontal(
                new Button("← Back to Epic", () => openedStory.Set((BacklogItem?)null)).Secondary().Small(),
                Text.H3($"Story: {story.Title}")
            ),

            // Story details
            new Card(
                Layout.Vertical(
                    Text.Small($"Epic: {epic.Title}"),
                    !string.IsNullOrEmpty(story.Description) ? Text.P(story.Description) : null,
                    Layout.Horizontal(
                        new Badge($"{story.StoryPoints} pts").Primary(),
                        new Badge($"{tasks.Length} tasks/bugs").Secondary()
                    ).Gap(4)
                )
            ),

            // Add Task/Bug button
            new Button("+ Add Task/Bug", () => isAddItemModalOpen.Set(true)).Primary(),

            // Tasks and Bugs list
            Text.H3($"Tasks & Bugs ({tasks.Length})"),
            tasks.Length == 0 ?
                new Card(Text.P("No tasks or bugs yet. Click '+ Add Task/Bug' to create a task or bug.")) :
                Layout.Vertical(
                    tasks
                        .OrderBy(x => x.Id)
                        .Select(task =>
                        {
                            bool isInSprint = task.SprintId != null;

                            return new Card(
                                Layout.Vertical(
                                    // Title with badge
                                    Layout.Horizontal(
                                        CardHelpers.GetIssueTypeBadge(task.Type),
                                        Text.Strong(task.Title)
                                    ).Gap(4),

                                    // Badges and buttons
                                    Layout.Horizontal(
                                        new Badge($"{task.StoryPoints} pts").Primary(),
                                        new Button("Delete", () => DeleteItem(task.Id)).Destructive().Small()
                                    )
                                ).Gap(4)
                            );
                        }).ToArray()
                ).Gap(4)
        );
    }

}
