namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Database;
using Taskly.Connections;

[App(icon: Icons.Calendar)]
public class PlanningApp : ViewBase
{

    private static Badge GetIssueTypeBadge(IssueType type)
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
                var itemModels = InitDatabase.GetAllBacklogItems();
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Reload current sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint();
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
                // Load backlog items
                var itemModels = InitDatabase.GetAllBacklogItems();
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Load current sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint();
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }

                // Load archived sprints
                var allSprints = InitDatabase.GetAllSprints();
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
                // Reload backlog items
                var itemModels = InitDatabase.GetAllBacklogItems();
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

                // Reload current sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint();
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
                if (addTaskToStory.Value != null)
                {
                    parentId = addTaskToStory.Value.Id; // Task/Bug belongs to Story (modal within Epic view)
                }
                else if (openedStory.Value != null)
                {
                    parentId = openedStory.Value.Id; // Task/Bug belongs to Story (Story detail view)
                }
                else if (openedEpic.Value != null)
                {
                    parentId = openedEpic.Value.Id; // Story belongs to Epic
                }
                // else: Epic has no parent

                var newItemModel = new BacklogItemModel
                {
                    Title = newTitle.Value,
                    Description = newDescription.Value,
                    StoryPoints = newStoryPoints.Value,
                    Priority = backlogItems.Value.Length + 1,
                    Status = ItemStatus.Backlog.ToString(),
                    Type = newIssueType.Value.ToString(),
                    ParentId = parentId
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

        // Conditional rendering based on hierarchy navigation
        return openedStory.Value != null ?
            // STORY DETAIL VIEW: Show Tasks and Bugs within the Story
            Layout.Vertical(
                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Task/Bug to Story"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                new SelectInput<IssueType>(
                                    value: newIssueType.Value,
                                    onChange: e => { newIssueType.Set(e.Value); return ValueTask.CompletedTask; },
                                    options: new[] {
                                        new Option<IssueType>("Task", IssueType.Task),
                                        new Option<IssueType>("Bug", IssueType.Bug)
                                    }
                                ),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                // Sprint management section
                BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, RemoveItemFromSprint),

                BuildStoryDetailView(openedStory.Value, openedEpic.Value!, backlogItems, openedEpic, openedStory, currentSprint, isAddItemModalOpen)
            ) :
        openedEpic.Value != null ?
            // EPIC DETAIL VIEW: Show Stories within the Epic
            Layout.Vertical(
                // Modal for adding Story to Epic
                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Story to Epic"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                Text.P($"Type: Story"),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                // Modal for adding Task/Bug to Story (within Epic view)
                addTaskToStory.Value != null ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3($"Add Task/Bug to Story: {addTaskToStory.Value.Title}"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                new SelectInput<IssueType>(
                                    value: newIssueType.Value,
                                    onChange: e => { newIssueType.Set(e.Value); return ValueTask.CompletedTask; },
                                    options: new[] {
                                        new Option<IssueType>("Task", IssueType.Task),
                                        new Option<IssueType>("Bug", IssueType.Bug)
                                    }
                                ),
                                Layout.Horizontal(
                                    new Button("Cancel", () => addTaskToStory.Set((BacklogItem?)null)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                // Sprint management section
                BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, RemoveItemFromSprint),

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

                new Button("+ Add Epic", () => { newIssueType.Set(IssueType.Epic); isAddItemModalOpen.Set(true); }).Primary(),

                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Epic"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                Text.P($"Type: Epic"),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                    // Sprint management section
                    BuildSprintManagementSection(currentSprint, backlogItems, newSprintName, newSprintGoal, CreateSprint, ArchiveSprint, RemoveItemFromSprint),

                    BuildEpicListViewSection(backlogItems, openedEpic, currentSprint, archivedSprints)
                );
    }

    // Sprint management section - shown in all views
    private object BuildSprintManagementSection(
        IState<Sprint> currentSprint,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<string> newSprintName,
        IState<string> newSprintGoal,
        Func<Event<Button>, ValueTask> createSprint,
        Action archiveSprint,
        Action<int> removeItemFromSprint)
    {
        return currentSprint.Value == null ?
            new Card(
                Layout.Vertical(
                    Text.H3("Create New Sprint"),
                    newSprintName.ToTextInput().Placeholder("Sprint name (e.g., Sprint 1)"),
                    newSprintGoal.ToTextInput().Placeholder("Sprint goal (optional)"),
                    new Button("Create Sprint", createSprint).Primary()
                )
            ) :
            new Card(
                Layout.Vertical(
                    Layout.Horizontal(
                        Layout.Vertical(
                            Text.H3($"Current Sprint: {currentSprint.Value.Name}"),
                            !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                                Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                            Text.Small($"Items in sprint: {currentSprint.Value.ItemIds.Length}")
                        ).Width(Size.Grow()),
                        new Button("Archive Sprint", archiveSprint).Secondary()
                    ),

                    // Display sprint items
                    currentSprint.Value.ItemIds.Length > 0 ?
                        Layout.Vertical(
                            Text.H4("Sprint Items:"),
                            Layout.Vertical(
                                backlogItems.Value
                                    .Where(item => currentSprint.Value.ItemIds.Contains(item.Id))
                                    .OrderBy(x => x.Id)
                                    .Select(item => new Card(
                                        Layout.Horizontal(
                                            GetIssueTypeBadge(item.Type),
                                            Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                                                $"{item.Title} - {item.Description}" : item.Title)
                                                .Width(Size.Grow()),
                                            new Badge(item.Status.ToString()).Secondary(),
                                            new Badge($"{item.StoryPoints} pts").Primary(),
                                            new Button("Remove from Sprint", () => removeItemFromSprint(item.Id)).Secondary().Small()
                                        )
                                    ))
                                    .ToArray()
                            ).Gap(4)
                        ).Gap(4) :
                        Text.P("No items in sprint yet. Use 'Add to Sprint' buttons below to add items.")
                ).Gap(4)
            );
    }

    // EPIC LIST VIEW: Shows only Epics (top-level items)
    private object BuildEpicListViewSection(IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<Sprint> currentSprint, IState<ImmutableArray<Sprint>> archivedSprints)
    {
        // Get only Epics (items with no parent)
        var epics = backlogItems.Value.Where(item => item.Type == IssueType.Epic && item.ParentId == null).ToArray();

        void DeleteItem(int id)
        {
            InitDatabase.DeleteBacklogItem(id);

            // Reload data
            var itemModels = InitDatabase.GetAllBacklogItems();
            var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
            backlogItems.Set(items);
        }

        return Layout.Vertical(
            Text.H3($"Epics ({epics.Length})"),
            epics.Length == 0 ?
                new Card(Text.P("No epics yet. Click '+ Add Epic' to create your first epic.")) :
                Layout.Vertical(
                    epics
                        .OrderBy(x => x.Id)
                        .Select(epic =>
                        {
                            bool isInSprint = epic.SprintId != null;
                            var storiesCount = backlogItems.Value.Count(item => item.ParentId == epic.Id && item.Type == IssueType.Story);

                            return new Card(
                                Layout.Horizontal(
                                    // Issue type badge
                                    GetIssueTypeBadge(epic.Type),

                                    // Title and description
                                    Text.Strong(!string.IsNullOrEmpty(epic.Description) ?
                                        $"{epic.Title} - {epic.Description}" : epic.Title)
                                        .Width(Size.Grow()),

                                    // Story count indicator
                                    new Badge($"{storiesCount} stories").Secondary(),

                                    // Story points
                                    new Badge($"{epic.StoryPoints} pts").Primary(),

                                    // Open Epic button
                                    new Button("Open", () => openedEpic.Set(epic)).Primary().Small(),

                                    // Delete button
                                    new Button("Delete", () => DeleteItem(epic.Id)).Destructive().Small()
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
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
                var itemModels = InitDatabase.GetAllBacklogItems();
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                var currentSprintModel = InitDatabase.GetCurrentSprint();
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
                new Button("← Back to Epics", () => openedEpic.Set((BacklogItem?)null)).Secondary().Small(),
                Text.H3($"Epic: {epic.Title}")
            ),

            // Epic details
            new Card(
                Layout.Vertical(
                    !string.IsNullOrEmpty(epic.Description) ? Text.P(epic.Description) : null,
                    Layout.Horizontal(
                        new Badge($"{epic.StoryPoints} pts").Primary(),
                        new Badge($"{stories.Length} stories").Secondary()
                    ).Gap(4)
                )
            ),

            // Add Story button
            new Button("+ Add Story", () => { newIssueType.Set(IssueType.Story); isAddItemModalOpen.Set(true); }).Primary(),

            // Stories list
            Text.H3($"Stories ({stories.Length})"),
            stories.Length == 0 ?
                new Card(Text.P("No stories yet. Click '+ Add Story' to create a story.")) :
                Layout.Vertical(
                    stories
                        .OrderBy(x => x.Id)
                        .Select(story =>
                        {
                            bool isInSprint = story.SprintId != null;
                            var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

                            return new Card(
                                Layout.Vertical(
                                    // Story header
                                    Layout.Horizontal(
                                        GetIssueTypeBadge(story.Type),

                                        Text.Strong(!string.IsNullOrEmpty(story.Description) ?
                                            $"{story.Title} - {story.Description}" : story.Title)
                                            .Width(Size.Grow()),

                                        new Badge($"{tasks.Length} tasks").Secondary(),
                                        new Badge($"{story.StoryPoints} pts").Primary(),

                                        isInSprint ?
                                            new Badge("In Sprint").Secondary() :
                                            new Badge("Backlog").Outline(),

                                        new Button("+ Add Task/Bug", () => { addTaskToStory.Set(story); newIssueType.Set(IssueType.Task); }).Primary().Small(),

                                        !isInSprint && currentSprint.Value != null ?
                                            new Button("Add to Sprint", () => AddItemToSprint(story.Id)).Primary().Small() :
                                        isInSprint ?
                                            new Button("Remove from Sprint", () => RemoveItemFromSprint(story.Id)).Secondary().Small() :
                                        null,

                                        new Button("Delete", () => DeleteItem(story.Id)).Destructive().Small()
                                    ),

                                    // Nested Tasks/Bugs - Always show section
                                    Layout.Vertical(
                                        Layout.Vertical(
                                            tasks.OrderBy(t => t.Id).Select(task =>
                                            {
                                                bool taskInSprint = task.SprintId != null;
                                                return new Card(
                                                    Layout.Horizontal(
                                                        GetIssueTypeBadge(task.Type),
                                                        Text.P(!string.IsNullOrEmpty(task.Description) ?
                                                            $"{task.Title} - {task.Description}" : task.Title)
                                                            .Width(Size.Grow()),
                                                        new Badge($"{task.StoryPoints} pts").Primary(),
                                                        new Button("Delete", () => DeleteItem(task.Id)).Destructive().Small()
                                                    )
                                                );
                                            }).ToArray()
                                        ).Gap(4)
                                    ).Gap(4)
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
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
            var itemModels = InitDatabase.GetAllBacklogItems();
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
                                Layout.Horizontal(
                                    GetIssueTypeBadge(task.Type),

                                    Text.Strong(!string.IsNullOrEmpty(task.Description) ?
                                        $"{task.Title} - {task.Description}" : task.Title)
                                        .Width(Size.Grow()),

                                    new Badge($"{task.StoryPoints} pts").Primary(),
                                    new Button("Delete", () => DeleteItem(task.Id)).Destructive().Small()
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
    }

}
