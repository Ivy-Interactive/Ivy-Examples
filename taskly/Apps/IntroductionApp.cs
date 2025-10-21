namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Database;
using Taskly.Connections;

[App(icon: Icons.BookOpen)]
public class IntroductionApp : ViewBase
{
    public override object? Build()
    {
        var currentStep = UseState(1);
        const int totalSteps = 8;

        // Load data from database
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState(() => (Sprint)null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);
        var isLoading = UseState(true);

        // Create signal to notify other apps to refresh
        var refreshSignal = Context.CreateSignal<RefreshDataSignal, bool, bool>();

        // Listen for refresh signals from other apps (e.g., when PlanningApp cleans database)
        var refreshReceiver = Context.UseSignal<RefreshDataSignal, bool, bool>();

        // Helper to reload data from database (used by signal receiver)
        void ReloadFromSignal()
        {
            try
            {
                // Load only tutorial items
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: true);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Load only tutorial sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: true);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }
                else
                {
                    currentSprint.Set((Sprint)null!);
                }

                // Load archived tutorial sprints
                var allSprints = InitDatabase.GetAllSprints(isTutorial: true);
                var archived = allSprints
                    .Where(s => s.IsArchived == 1)
                    .Select(s => s.ToSprint())
                    .ToImmutableArray();
                archivedSprints.Set(archived);

                // Only reset to step 1 if database was cleaned (no tutorial items AND introduction not completed)
                var introCompleted = AppSettings.GetBoolSetting("introduction_completed", false);
                if (!introCompleted && items.Length == 0 && currentSprintModel == null)
                {
                    currentStep.Set(1);
                }
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

        // Modal states
        var isAddEpicModalOpen = UseState(false);
        var isAddStoryModalOpen = UseState(false);
        var isAddTaskModalOpen = UseState(false);
        var showStoryAddedPopup = UseState(false);
        var showSprintArchivedPopup = UseState(false);

        // Form states for creating items
        var newEpicTitle = UseState("");
        var newEpicDescription = UseState("");

        var newStoryTitle = UseState("");
        var newStoryDescription = UseState("");
        var newStoryPoints = UseState(1);
        var selectedEpic = UseState<BacklogItem?>(() => null);

        var newTaskTitle = UseState("");
        var newTaskDescription = UseState("");
        var newTaskPoints = UseState(1);
        var newTaskType = UseState(IssueType.Task);
        var selectedStory = UseState<BacklogItem?>(() => null);

        var newSprintName = UseState("");
        var newSprintGoal = UseState("");

        // Load data from database on mount
        UseEffect(() =>
        {
            try
            {
                // Check if introduction is already completed
                var introCompleted = AppSettings.GetBoolSetting("introduction_completed", false);
                if (introCompleted)
                {
                    currentStep.Set(8);
                }

                // Load only tutorial items
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: true);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Load only tutorial sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: true);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }

                // Load archived tutorial sprints
                var allSprints = InitDatabase.GetAllSprints(isTutorial: true);
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


        // Helper to reload data from database and notify other apps
        async Task ReloadData()
        {
            try
            {
                // Load only tutorial items
                var itemModels = InitDatabase.GetAllBacklogItems(isTutorial: true);
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);

                // Load only tutorial sprint
                var currentSprintModel = InitDatabase.GetCurrentSprint(isTutorial: true);
                if (currentSprintModel != null)
                {
                    currentSprint.Set(currentSprintModel.ToSprint());
                }
                else
                {
                    currentSprint.Set((Sprint)null!);
                }

                // Load archived tutorial sprints
                var allSprints = InitDatabase.GetAllSprints(isTutorial: true);
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

        // Create Epic
        async void CreateEpic()
        {
            if (!string.IsNullOrWhiteSpace(newEpicTitle.Value))
            {
                var epicModel = new BacklogItemModel
                {
                    Title = newEpicTitle.Value,
                    Description = newEpicDescription.Value,
                    StoryPoints = 0,
                    Priority = 1,
                    Status = ItemStatus.Backlog.ToString(),
                    Type = IssueType.Epic.ToString(),
                    ParentId = null,
                    IsTutorial = 1  // Tutorial data
                };

                var created = InitDatabase.CreateBacklogItem(epicModel);
                if (created != null)
                {
                    await ReloadData();
                    newEpicTitle.Set("");
                    newEpicDescription.Set("");
                    isAddEpicModalOpen.Set(false);
                }
            }
        }

        // Create Story
        async void CreateStory()
        {
            if (!string.IsNullOrWhiteSpace(newStoryTitle.Value) && selectedEpic.Value != null)
            {
                var storyModel = new BacklogItemModel
                {
                    Title = newStoryTitle.Value,
                    Description = newStoryDescription.Value,
                    StoryPoints = newStoryPoints.Value,
                    Priority = 1,
                    Status = ItemStatus.Backlog.ToString(),
                    Type = IssueType.Story.ToString(),
                    ParentId = selectedEpic.Value.Id,
                    IsTutorial = 1  // Tutorial data
                };

                var created = InitDatabase.CreateBacklogItem(storyModel);
                if (created != null)
                {
                    await ReloadData();

                    // Set the newly created story as selected and advance to Step 3
                    var newStory = created.ToBacklogItem();
                    selectedStory.Set(newStory);
                    currentStep.Set(4);

                    newStoryTitle.Set("");
                    newStoryDescription.Set("");
                    newStoryPoints.Set(3);
                    isAddStoryModalOpen.Set(false);
                }
            }
        }

        // Create Task/Bug
        async void CreateTask()
        {
            if (!string.IsNullOrWhiteSpace(newTaskTitle.Value) && selectedStory.Value != null)
            {
                // Check if the parent story is already in a sprint
                var parentStory = selectedStory.Value;
                var storyInSprint = parentStory.SprintId != null;

                var taskModel = new BacklogItemModel
                {
                    Title = newTaskTitle.Value,
                    Description = newTaskDescription.Value,
                    StoryPoints = newTaskPoints.Value,
                    Priority = 1,
                    Status = storyInSprint ? ItemStatus.Todo.ToString() : ItemStatus.Backlog.ToString(),
                    Type = newTaskType.Value.ToString(),
                    ParentId = selectedStory.Value.Id,
                    SprintId = parentStory.SprintId,
                    IsTutorial = 1  // Tutorial data
                };

                var created = InitDatabase.CreateBacklogItem(taskModel);
                if (created != null)
                {
                    await ReloadData();

                    // Advance to Step 5 (Add to Sprint)
                    currentStep.Set(5);

                    newTaskTitle.Set("");
                    newTaskDescription.Set("");
                    newTaskPoints.Set(2);
                    newTaskType.Set(IssueType.Task);
                    isAddTaskModalOpen.Set(false);
                }
            }
        }

        // Create Sprint
        async ValueTask CreateSprint(Event<Button> _)
        {
            if (!string.IsNullOrWhiteSpace(newSprintName.Value))
            {
                var sprintModel = new SprintModel
                {
                    Name = newSprintName.Value,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(14),
                    Goal = newSprintGoal.Value,
                    ItemIds = new List<int>(),
                    IsArchived = 0,
                    IsTutorial = 1  // Tutorial data
                };

                var created = InitDatabase.CreateSprint(sprintModel);
                if (created != null)
                {
                    await ReloadData();
                    newSprintName.Set("");
                    newSprintGoal.Set("");
                    currentStep.Set(2); // Advance to Step 2 (Create Epic)
                }
            }
        }

        // Add Story to Sprint
        async void AddStoryToSprint(BacklogItem story)
        {
            if (currentSprint.Value != null)
            {
                var updatedStory = story with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id };
                InitDatabase.UpdateBacklogItem(updatedStory.ToBacklogItemModel());

                // Also update child tasks
                var childTasks = backlogItems.Value.Where(item => item.ParentId == story.Id).ToArray();
                foreach (var task in childTasks)
                {
                    var updatedTask = task with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id };
                    InitDatabase.UpdateBacklogItem(updatedTask.ToBacklogItemModel());
                }

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Add(story.Id)
                };
                InitDatabase.UpdateSprint(updatedSprint.ToSprintModel());

                await ReloadData();

                // Show success popup
                showStoryAddedPopup.Set(true);
            }
        }

        // Delete item
        async void DeleteItem(int id)
        {
            InitDatabase.DeleteBacklogItem(id);
            await ReloadData();
        }

        // Archive Sprint
        async void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Archive the sprint in database
                var sprintModel = currentSprint.Value.ToSprintModel(isArchived: true);
                InitDatabase.UpdateSprint(sprintModel);

                await ReloadData();

                // If we're in Step 7, show the success popup
                if (currentStep.Value == 7)
                {
                    showSprintArchivedPopup.Set(true);
                }
                else
                {
                    // Move to Step 7 (Archive view) from other steps
                    currentStep.Set(7);
                }
            }
        }

        void NextStep()
        {
            var nextStepValue = Math.Min(currentStep.Value + 1, totalSteps);
            currentStep.Set(nextStepValue);

            // Mark introduction as completed when reaching step 8
            if (nextStepValue == 8)
            {
                AppSettings.SetBoolSetting("introduction_completed", true);
            }
        }

        if (isLoading.Value)
        {
            return new Card(Text.P("Loading..."));
        }

        var epics = backlogItems.Value.Where(x => x.Type == IssueType.Epic && x.ParentId == null).ToArray();

        // Check if user can proceed to next step
        bool CanProceedToNextStep()
        {
            return currentStep.Value switch
            {
                1 => currentSprint.Value != null, // Must create a sprint
                2 => epics.Length > 0, // Must create at least one epic
                3 => backlogItems.Value.Any(x => x.Type == IssueType.Story), // Must create at least one story
                4 => backlogItems.Value.Any(x => x.Type == IssueType.Task || x.Type == IssueType.Bug), // Must create at least one task/bug
                5 => currentSprint.Value != null && currentSprint.Value.ItemIds.Length > 0, // Must add at least one story to sprint
                6 => true, // Can always proceed from sprint board
                7 => true, // Can always proceed from archive
                8 => false, // Last step, can't proceed
                _ => false
            };
        }

        return Layout.Vertical(
            // Header
            new Card(
                Layout.Vertical(
                    Text.H2("Getting Started with Taskly"),
                    Text.P("Follow these steps to create your first project structure."),
                    Layout.Vertical(
                        new Badge($"Step {currentStep.Value} of {totalSteps}").Primary(),
                        new Progress(currentStep.Value * 100 / totalSteps),
                        Text.Small($"Progress: {(currentStep.Value * 100 / totalSteps)}%")
                    ).Gap(4)
                ).Gap(4)
            ).Width(Size.Units(300)),

            // Step Content
            currentStep.Value switch
            {
                1 => BuildStep1_CreateSprint(currentSprint, newSprintName, newSprintGoal, CreateSprint),
                2 => BuildStep2_CreateEpic(epics, backlogItems, newEpicTitle, newEpicDescription, CreateEpic, DeleteItem, isAddEpicModalOpen, selectedEpic, currentStep),
                3 => BuildStep3_CreateStories(epics, selectedEpic, backlogItems, newStoryTitle, newStoryDescription, newStoryPoints, CreateStory, DeleteItem, isAddStoryModalOpen, selectedStory, currentStep),
                4 => BuildStep4_CreateTasks(epics, selectedEpic, selectedStory, backlogItems, newTaskTitle, newTaskDescription, newTaskPoints, newTaskType, CreateTask, DeleteItem, isAddTaskModalOpen, currentStep),
                5 => BuildStep5_AddToSprint(epics, backlogItems, currentSprint, AddStoryToSprint, showStoryAddedPopup, currentStep),
                6 => BuildStep6_SprintBoard(backlogItems, currentSprint, currentStep, CanProceedToNextStep, NextStep),
                7 => BuildStep7_Archive(backlogItems, archivedSprints, currentSprint, ArchiveSprint, showSprintArchivedPopup, currentStep),
                8 => BuildStep8_Congratulations(currentStep),
                _ => Text.P("Invalid step")
            }
        ).Gap(8);
    }

    private object BuildStep1_CreateSprint(
        IState<Sprint> currentSprint,
        IState<string> newSprintName,
        IState<string> newSprintGoal,
        Func<Event<Button>, ValueTask> createSprint)
    {
        return new Card(
            Layout.Vertical(
                Text.H3("Step 1: Create a Sprint"),
                Text.P("A Sprint is a time-boxed iteration where you'll work on selected Stories. Create your sprint first, then we'll add work items to it. Input a name and goal for the sprint then press the 'Create Sprint' button to progress to next step."),

                currentSprint.Value == null ?
                    SprintFormCard.Build(newSprintName, newSprintGoal, createSprint) : null,

                new Card(
                    Layout.Vertical(
                        Text.Strong("💡 Tips:"),
                        Text.P("• Sprints are typically 1-2 weeks long"),
                        Text.P("• Set a clear, achievable goal for each sprint"),
                        Text.P("• You can only have one active sprint at a time"),
                        Text.P("• After creating the sprint, we'll create Epics and Stories to add to it")
                    ).Gap(2)
                ).Width(Size.Fit())
            ).Gap(8)
        ).Width(Size.Half());
    }

    private object BuildStep2_CreateEpic(
        BacklogItem[] epics,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<string> newEpicTitle,
        IState<string> newEpicDescription,
        Action createEpic,
        Action<int> deleteItem,
        IState<bool> isAddEpicModalOpen,
        IState<BacklogItem?> selectedEpic,
        IState<int> currentStep)
    {
        return Layout.Vertical(
            // Modal for adding Epic
            isAddEpicModalOpen.Value ?
                BacklogItemFormModal.Build(
                    title: "Add Epic",
                    itemTitle: newEpicTitle,
                    itemDescription: newEpicDescription,
                    itemType: "Epic",
                    onCancel: () => isAddEpicModalOpen.Set(false),
                    onSubmit: createEpic
                ) : null,

            new Card(
                Layout.Vertical(
                    Text.H3("Step 2: Create an Epic"),
                    Text.P("An Epic represents a large feature or initiative. Create a new epic by pressing the Add Epic button below. Then open the epic to add stories in the next step."),

                    // Display existing epics
                    Layout.Vertical(
                        Text.H4($"Epics ({epics.Length})"),
                        new Button("+ Add Epic", () => isAddEpicModalOpen.Set(true)).Primary(),
                        epics.Length == 0 ?
                            new Card(
                                Text.P("No epics created yet. Click '+ Add Epic' to create your first epic.")
                            ).Width(Size.Fit()) :
                            Layout.Vertical(
                                epics.Select(epic =>
                                {
                                    var storiesCount = backlogItems.Value.Count(item => item.ParentId == epic.Id && item.Type == IssueType.Story);
                                    return EpicCard.Build(
                                        epic: epic,
                                        storiesCount: storiesCount,
                                        onOpen: () => { selectedEpic.Set(epic); currentStep.Set(3); },
                                        onDelete: () => deleteItem(epic.Id),
                                        showActions: true
                                    );
                                }).ToArray()
                            ).Gap(4)
                    ).Gap(4),

                    new Card(
                        Layout.Vertical(
                            Text.Strong("💡 Tips:"),
                            Text.P("• Epics are the highest level - think big features or business goals"),
                            Text.P("• Examples: 'User Management', 'Payment System', 'Mobile App'"),
                            Text.P("• You can create multiple epics for different areas of your project")
                        ).Gap(2)
                    ).Width(Size.Fit())
                ).Gap(8)
            ).Width(Size.Half())
        );
    }

    private object BuildStep3_CreateStories(
        BacklogItem[] epics,
        IState<BacklogItem?> selectedEpic,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<string> newStoryTitle,
        IState<string> newStoryDescription,
        IState<int> newStoryPoints,
        Action createStory,
        Action<int> deleteItem,
        IState<bool> isAddStoryModalOpen,
        IState<BacklogItem?> selectedStory,
        IState<int> currentStep)
    {
        var epic = selectedEpic.Value!;
        var stories = backlogItems.Value.Where(x => x.Type == IssueType.Story && x.ParentId == epic.Id).ToArray();

        return Layout.Vertical(
            // Modal for adding Story
            isAddStoryModalOpen.Value ?
                BacklogItemFormModal.Build(
                    title: "Add Story",
                    itemTitle: newStoryTitle,
                    itemDescription: newStoryDescription,
                    itemType: "Story",
                    onCancel: () => isAddStoryModalOpen.Set(false),
                    onSubmit: createStory,
                    storyPoints: newStoryPoints
                ) : null,

            Layout.Vertical(
                // Step instructions
                Text.H2("Step 3: Create Stories"),
                Text.P("Stories are user-focused features that deliver value. Break down your Epic into smaller, manageable Stories that can be completed in a sprint. Click the '+ Add Story' button to create a new story under the selected epic."),

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
                    new Button("+ Add Story", () => isAddStoryModalOpen.Set(true)).Primary(),
                    stories.Length == 0 ?
                        new Card(Text.P("No stories yet. Click '+ Add Story' to create a story.")).Width(Size.Units(100)) :
                        Layout.Vertical(
                            stories.OrderBy(x => x.Id).Select(story =>
                            {
                                var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

                                // Build nested tasks
                                var nestedTasks = Layout.Vertical(
                                    Layout.Vertical(
                                        tasks.OrderBy(t => t.Id).Select(task =>
                                            TaskCard.Build(
                                                task: task,
                                                onDelete: () => deleteItem(task.Id),
                                                showActions: true
                                            )
                                        ).ToArray()
                                    ).Gap(4)
                                ).Gap(4);

                                return StoryCard.Build(
                                    story: story,
                                    tasksCount: tasks.Length,
                                    isInSprint: false,
                                    currentSprintExists: false,
                                    onAddTaskBug: () => { },
                                    onDelete: () => deleteItem(story.Id),
                                    nestedTasks: nestedTasks,
                                    showActions: true
                                );
                            }).ToArray()
                        ).Gap(16)
                ),

                new Card(
                    Layout.Vertical(
                        Text.Strong("💡 Tips:"),
                        Text.P("• Stories should deliver value to users"),
                        Text.P("• Keep stories small enough to complete in one sprint"),
                        Text.P("• Use story points to estimate effort (3-5 points is typical)"),
                        Text.P("• Click '+ Add Task/Bug' on a story to break it down into tasks")
                    ).Gap(2)
                ).Width(Size.Fit())
            ).Gap(4)
        );
    }

    private object BuildStep4_CreateTasks(
        BacklogItem[] epics,
        IState<BacklogItem?> selectedEpic,
        IState<BacklogItem?> selectedStory,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<string> newTaskTitle,
        IState<string> newTaskDescription,
        IState<int> newTaskPoints,
        IState<IssueType> newTaskType,
        Action createTask,
        Action<int> deleteItem,
        IState<bool> isAddTaskModalOpen,
        IState<int> currentStep)
    {
        var epic = selectedEpic.Value!;
        var story = selectedStory.Value!;
        var allStoriesInEpic = backlogItems.Value.Where(x => x.Type == IssueType.Story && x.ParentId == epic.Id).ToArray();

        return Layout.Vertical(
            // Modal for adding Task/Bug
            isAddTaskModalOpen.Value ?
                BacklogItemFormModal.Build(
                    title: "Add Task/Bug",
                    itemTitle: newTaskTitle,
                    itemDescription: newTaskDescription,
                    itemType: "Task/Bug",
                    onCancel: () => isAddTaskModalOpen.Set(false),
                    onSubmit: createTask,
                    storyPoints: newTaskPoints,
                    issueTypeSelect: newTaskType
                ) : null,

            Layout.Vertical(
                // Step instructions
                Text.H2("Step 4: Break Down Stories into Tasks"),
                Text.P("Tasks are the actual work items that need to be done to complete a Story. Break down each Story into specific Tasks (implementation work) or Bugs (issues to fix). Click the '+ Add Task/Bug' button to create a new task under the selected story."),

                // Epic details
                Text.H3($"Epic: {epic.Title}"),
                Layout.Vertical(
                    !string.IsNullOrEmpty(epic.Description) ? Text.P("Goal: " + epic.Description) : null,
                    Layout.Horizontal(
                        new Badge($"{allStoriesInEpic.Length} stories").Secondary()
                    )
                ).Width(Size.Units(100)),

                // Stories list
                Layout.Vertical(
                    Text.H3($"Stories ({allStoriesInEpic.Length})"),
                    allStoriesInEpic.Length == 0 ?
                        new Card(Text.P("No stories yet.")).Width(Size.Units(100)) :
                        Layout.Vertical(
                            allStoriesInEpic.OrderBy(x => x.Id).Select(s =>
                            {
                                var tasks = backlogItems.Value.Where(item => item.ParentId == s.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

                                // Build nested tasks
                                var nestedTasks = Layout.Vertical(
                                    Layout.Vertical(
                                        tasks.OrderBy(t => t.Id).Select(task =>
                                            TaskCard.Build(
                                                task: task,
                                                onDelete: () => deleteItem(task.Id),
                                                showActions: true
                                            )
                                        ).ToArray()
                                    ).Gap(4)
                                ).Gap(4);

                                return StoryCard.Build(
                                    story: s,
                                    tasksCount: tasks.Length,
                                    isInSprint: false,
                                    currentSprintExists: false,
                                    onAddTaskBug: () => { selectedStory.Set(s); isAddTaskModalOpen.Set(true); },
                                    onDelete: () => deleteItem(s.Id),
                                    nestedTasks: nestedTasks,
                                    showActions: true
                                );
                            }).ToArray()
                        ).Gap(16)
                ),

                new Card(
                    Layout.Vertical(
                        Text.Strong("💡 Tips:"),
                        Text.P("• Click '+ Add Task/Bug' on any story to add tasks"),
                        Text.P("• Tasks: Implementation work (frontend, backend, testing)"),
                        Text.P("• Bugs: Defects that need fixing"),
                        Text.P("• When done, click 'Next' to proceed to sprint planning")
                    ).Gap(2)
                ).Width(Size.Fit())
            ).Gap(4)
        );
    }

    private object BuildStep5_AddToSprint(
        BacklogItem[] epics,
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<Sprint> currentSprint,
        Action<BacklogItem> addStoryToSprint,
        IState<bool> showStoryAddedPopup,
        IState<int> currentStep)
    {
        if (currentSprint.Value == null)
        {
            return new Card(
                Layout.Vertical(
                    Text.H3("Step 5: Add Stories to Sprint"),
                    Text.P("⚠️ You need to create a Sprint first."),
                    new Button("← Go Back", () => { }).Secondary()
                ).Gap(4)
            ).Width(Size.Full());
        }

        return Layout.Vertical(
            // Success popup when story is added
            showStoryAddedPopup.Value ?
                new FloatingPanel(
                    new Card(
                        Layout.Vertical(
                            Text.H3("✅ Story Added to Sprint!"),
                            Text.P($"The story has been successfully added to '{currentSprint.Value.Name}'."),
                            Text.P("All tasks under this story are automatically included in the sprint."),
                            Layout.Horizontal(
                                new Button("Next: Sprint Board →", () =>
                                {
                                    showStoryAddedPopup.Set(false);
                                    currentStep.Set(6);
                                }).Primary()
                            ).Gap(8)
                        ).Gap(8)
                    ).Width(Size.Units(120))
                ) : null,

            // Step instructions
            Text.H2("Step 5: Add Stories to Sprint"),
            Text.P("Select which stories you want to work on in this sprint. Click 'Add to Sprint' on your story to include it and all its tasks in your sprint planning."),

            // Sprint Info Card at top - matching PlanningApp style
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
                                                    new Badge($"{item.StoryPoints} pts").Primary()
                                                )
                                            ).Gap(2),
                                            content: !string.IsNullOrEmpty(item.Description) ?
                                                Text.P(item.Description) : Text.P("No description")
                                        )
                                    )
                                    .ToArray()
                            )
                        ).Gap(2) :
                        Text.P("No items in sprint yet. Use 'Add to Sprint' buttons below to add items.")
                ).Gap(2)
            ).Width(Size.Third()),

            // Epics with Stories
            epics.Length == 0 ?
                new Card(Text.P("No epics yet. Go back to create an epic first.")).Width(Size.Units(100)) :
                Layout.Vertical(
                    epics.OrderBy(x => x.Id).Select(epic =>
                    {
                        var allStoriesInEpic = backlogItems.Value
                            .Where(x => x.Type == IssueType.Story && x.ParentId == epic.Id)
                            .ToArray();

                        return Layout.Vertical(
                            // Epic details
                            Text.H3($"Epic: {epic.Title}"),
                            Layout.Vertical(
                                !string.IsNullOrEmpty(epic.Description) ? Text.P("Goal: " + epic.Description) : null,
                                Layout.Horizontal(
                                    new Badge($"{allStoriesInEpic.Length} stories").Secondary()
                                )
                            ).Width(Size.Units(100)),

                            // Stories list with proper heading
                            Layout.Vertical(
                                Text.H3($"Stories ({allStoriesInEpic.Length})"),
                                allStoriesInEpic.Length == 0 ?
                                    new Card(Text.P("No stories yet.")).Width(Size.Units(100)) :
                                    Layout.Vertical(
                                        allStoriesInEpic.OrderBy(s => s.Id).Select(s =>
                                        {
                                            var tasks = backlogItems.Value
                                                .Where(item => item.ParentId == s.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug))
                                                .ToArray();
                                            var isInSprint = s.SprintId == currentSprint.Value.Id;

                                            // Build nested tasks
                                            var nestedTasks = Layout.Vertical(
                                                Layout.Vertical(
                                                    tasks.OrderBy(t => t.Id).Select(task =>
                                                        TaskCard.Build(
                                                            task: task,
                                                            onDelete: () => { },
                                                            showActions: true
                                                        )
                                                    ).ToArray()
                                                ).Gap(4)
                                            ).Gap(4);

                                            return StoryCard.Build(
                                                story: s,
                                                tasksCount: tasks.Length,
                                                isInSprint: isInSprint,
                                                currentSprintExists: true,
                                                onAddToSprint: !isInSprint ? () => addStoryToSprint(s) : null,
                                                onRemoveFromSprint: isInSprint ? () => { } : null,
                                                onAddTaskBug: () => { },
                                                onDelete: () => { },
                                                nestedTasks: nestedTasks,
                                                showActions: true
                                            );
                                        }).ToArray()
                                    ).Gap(16)
                            )
                        ).Gap(0);
                    }).ToArray()
                ).Gap(0),

            new Card(
                Layout.Vertical(
                    Text.Strong("💡 Tips:"),
                    Text.P("• Click 'Add to Sprint' on stories to include them in the sprint"),
                    Text.P("• All tasks under a story are automatically included"),
                    Text.P("• Don't overcommit - start with a few stories"),
                    Text.P("• Stories already in sprint show 'In Sprint' badge")
                ).Gap(2)
            ).Width(Size.Fit())
        ).Gap(8);
    }

    private object BuildStep6_SprintBoard(
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<Sprint> currentSprint,
        IState<int> currentStep,
        Func<bool> canProceedToNextStep,
        Action nextStep)
    {
        var spaceingBoardColumns = 120;

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

        // Helper method to update item status in database
        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var item = backlogItems.Value.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                var updated = item with { Status = newStatus };
                InitDatabase.UpdateBacklogItem(updated.ToBacklogItemModel());

                // Reload data
                var itemModels = InitDatabase.GetAllBacklogItems();
                var items = itemModels.Select(m => m.ToBacklogItem()).ToImmutableArray();
                backlogItems.Set(items);
            }
        }

        // Helper to build task/bug card
        object BuildTaskCard(BacklogItem task)
        {
            return new Card(
                Layout.Vertical(
                    // Header with issue type and title
                    Layout.Horizontal(
                        CardHelpers.GetIssueTypeBadge(task.Type),
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
                                    CardHelpers.GetIssueTypeBadge(IssueType.Epic),
                                    Text.Strong(epic?.Title ?? "Unknown Epic").Width(Size.Grow())
                                ),

                                // Stories within epic
                                Layout.Vertical(
                                    stories.Select(story =>
                                    {
                                        // Get tasks for this story grouped by status
                                        var storyTasks = allTasks.Where(t => t.ParentId == story.Id).ToArray();
                                        var storyTodoTasks = storyTasks.Where(t => t.Status == ItemStatus.Todo).ToArray();
                                        var storyInProgressTasks = storyTasks.Where(t => t.Status == ItemStatus.InProgress).ToArray();
                                        var storyDoneTasks = storyTasks.Where(t => t.Status == ItemStatus.Done).ToArray();

                                        return new Card(
                                            Layout.Vertical(
                                                // Story header
                                                Layout.Horizontal(
                                                    CardHelpers.GetIssueTypeBadge(IssueType.Story),
                                                    Text.Strong(story.Title).Width(Size.Grow())
                                                ),

                                                // Kanban columns for this story's tasks
                                                Layout.Horizontal(
                                                    // To Do Column
                                                    Layout.Vertical(
                                                        Text.H4($"To Do ({storyTodoTasks.Length})"),
                                                        storyTodoTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                storyTodoTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Units(spaceingBoardColumns)),

                                                    // In Progress Column
                                                    Layout.Vertical(
                                                        Text.H4($"In Progress ({storyInProgressTasks.Length})"),
                                                        storyInProgressTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                storyInProgressTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Units(spaceingBoardColumns)),

                                                    // Done Column
                                                    Layout.Vertical(
                                                        Text.H4($"Done ({storyDoneTasks.Length})"),
                                                        storyDoneTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                storyDoneTasks.Select(BuildTaskCard).ToArray()
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

        return Layout.Vertical(
            Text.H2("Step 6: Sprint Board - Track Your Work"),
            Text.P("Manage and track your work during the sprint using this Sprint Board. Move tasks through the workflow from To Do → In Progress → Done using the action buttons on each task card. When you feel ready press the next button in the bottom of the page to move to the next step."),

            // Show current sprint info or message if no sprint
            currentSprint.Value != null ?
                SprintSummaryCard.Build(
                    sprint: currentSprint.Value,
                    allTasksCount: allTasks.Length,
                    todoTasksCount: todoTasks.Length,
                    inProgressTasksCount: inProgressTasks.Length,
                    doneTasksCount: doneTasks.Length,
                    archiveButton: null  // Disabled in tutorial step 6
                ) : null,
                
            // Hierarchical Kanban board with columns inside each story
            currentSprint.Value != null && sprintStories.Length > 0 ?
                BuildHierarchyPanel() :
            currentSprint.Value != null ?
                new Card(
                    Text.P("No stories in sprint yet. Go back to Step 5 to add stories.")
                ).Width(Size.Fit()) : null,

            // Tips section
            new Card(
                Layout.Vertical(
                    Text.Strong("💡 Try It Out:"),
                    Text.P("• Click 'Start' to move a task from To Do → In Progress"),
                    Text.P("• Click 'Complete' to move a task from In Progress → Done"),
                    Text.P("• Click '← Reverse' to move a task backwards"),
                    Text.P("• The Sprint Board app works exactly like this!")
                ).Gap(2)
            ).Width(Size.Full()),

            // Next step prompt
            new Card(
                Layout.Vertical(
                    Text.Strong("Ready for the next step?"),
                    Text.P("Click 'Next' to archive your sprint and see the Sprint Archive view!")
                ).Gap(2)
            ).Width(Size.Full()),

            // Navigation button
            new Card(
                Layout.Horizontal(
                    new Spacer().Width(Size.Grow()),
                    canProceedToNextStep() ?
                        new Button("Next →", nextStep).Primary() :
                        new Button("Next →", nextStep).Primary().Disabled()
                ).Gap(8)
            )
        ).Gap(16);
    }

    private object BuildStep7_Archive(
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<ImmutableArray<Sprint>> archivedSprints,
        IState<Sprint> currentSprint,
        Action archiveSprint,
        IState<bool> showSprintArchivedPopup,
        IState<int> currentStep)
    {
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
            // Success popup when sprint is archived
            showSprintArchivedPopup.Value ?
                new FloatingPanel(
                    new Card(
                        Layout.Vertical(
                            Text.H3("✅ Sprint Archived!"),
                            Text.P("The sprint has been successfully archived and moved to the archive list below."),
                            Text.P("You can now review its metrics and see all completed work."),
                            Layout.Horizontal(
                                new Button("Next: Complete Tutorial →", () =>
                                {
                                    showSprintArchivedPopup.Set(false);
                                    currentStep.Set(8);
                                }).Primary()
                            ).Gap(8)
                        ).Gap(6)
                    ).Width(Size.Grow())
                ) : null,

            Text.H2("Step 7: Sprint Archive - Review Completed Work"),
            Text.P("The Sprint Archive allows you to review completed sprints along with key metrics like item completion rates and story points. You can also restore archived sprints back to active status if needed. Press the 'Archive Sprint' button below to archive your current sprint and see it appear in the archive list."),

            // Archive current sprint button
            currentSprint.Value != null ?
                new Card(
                    Layout.Vertical(
                        // Archive button at top-right
                        Layout.Horizontal(
                            new Spacer().Width(Size.Grow()),
                            new Button("Archive Sprint", archiveSprint).Secondary()
                        ),

                        // Sprint info below
                        Text.H3("Current Sprint"),
                        Text.P($"You still have an active sprint: {currentSprint.Value.Name}"),
                        Text.P("Click the button above to archive it and move it to the archive view.")
                    ).Gap(4)
                ).Width(Size.Fit()) : null,

            // Archived sprints
            archivedSprints.Value.Length == 0 ?
                new Card(
                    Text.P("No archived sprints yet. Click 'Archive Sprint' above to archive your current sprint.")
                ).Width(Size.Fit()) :
                Layout.Vertical(
                    Text.H3("Archived Sprints"),
                    Layout.Vertical(
                        archivedSprints.Value
                            .OrderByDescending(s => s.Id)
                            .Select(sprint =>
                            {
                                var sprintItems = backlogItems.Value
                                    .Where(item => sprint.ItemIds.Contains(item.Id))
                                    .ToArray();

                                return ArchivedSprintCard.Build(
                                    sprint: sprint,
                                    sprintItems: sprintItems,
                                    makeCurrentAction: null,  // Disabled in tutorial
                                    deleteAction: null,        // Disabled in tutorial
                                    getIssueTypeBadge: GetIssueTypeBadge
                                );
                            })
                            .ToArray()
                    ).Gap(6).Width(Size.Half())
                ),

            // Tips section
            new Card(
                Layout.Vertical(
                    Text.Strong("💡 About Sprint Archive:"),
                    Text.P("• The Archive shows completed sprints with their metrics"),
                    Text.P("• You can see completion rates for items and story points"),
                    Text.P("• 'Make Current Sprint' lets you restore an archived sprint"),
                    Text.P("• This helps track team velocity and progress over time")
                ).Gap(2)
            ).Width(Size.Full())
        ).Gap(10);
    }

    private object BuildStep8_Congratulations(IState<int> currentStep)
    {
        async ValueTask RestartTutorial(Event<Button> _)
        {
            // Clean only tutorial data
            await CleanDatabase.CleanTutorialData();

            // Reset introduction completion flag
            AppSettings.SetBoolSetting("introduction_completed", false);

            // Go back to step 1
            currentStep.Set(1);
        }

        return new Card(
            Layout.Vertical(
                Text.H2("🎉 Congratulations!"),
                Text.P("You've completed the complete Taskly introduction!"),

                new Card(
                    Layout.Vertical(
                        Text.H3("What You've Learned:").WithConfetti(AnimationTrigger.Auto),
                        Text.P("✅ Created Epics for large initiatives"),
                        Text.P("✅ Broke down Epics into Stories"),
                        Text.P("✅ Created Tasks and Bugs for Stories"),
                        Text.P("✅ Planned Sprints with selected Stories"),
                        Text.P("✅ Managed work on the Sprint Board"),
                        Text.P("✅ Moved tasks through the workflow (To Do → In Progress → Done)"),
                        Text.P("✅ Archived sprints and reviewed metrics"),
                        Text.P("✅ All your items are saved and visible in other tabs!")
                    ).Gap(2)
                ).Width(Size.Full()),

                new Card(
                    Layout.Vertical(
                        Text.Strong("📱 Your Apps:"),
                        Text.P("• Planning App: Manage your backlog, create items, and plan sprints"),
                        Text.P("• Sprint Board: Track active work with the kanban board"),
                        Text.P("• Sprint Archive: View completed sprints and team metrics")
                    ).Gap(2)
                ).Width(Size.Full()),

                new Card(
                    Layout.Vertical(
                        Text.Strong("🚀 You're Ready!"),
                        Text.P("All the Epics, Stories, and Tasks you created are in your backlog."),
                        Text.P("Switch to the other tabs to continue using Taskly for your projects."),
                        Text.P("Happy project planning! 🎯")
                    ).Gap(2)
                ).Width(Size.Full()),

                // Action button
                new Button("🔄 Restart Tutorial", RestartTutorial).Secondary()
            ).Gap(8)
        ).Width(Size.Fit());
    }
}
