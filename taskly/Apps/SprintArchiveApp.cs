namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Database;
using Taskly.Connections;

[App(icon: Icons.Archive)]
public class SprintArchiveApp : ViewBase
{
    public override object Build()
    {
        // Local state loaded from database
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState<Sprint>(() => null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);
        var isLoading = UseState(true);

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

        async void RestoreSprint(Sprint sprint)
        {
            // If there's a current sprint, archive it first
            if (currentSprint.Value != null)
            {
                var currentModel = currentSprint.Value.ToSprintModel(isArchived: true);
                InitDatabase.UpdateSprint(currentModel);
            }

            // Unarchive the selected sprint and make it current
            var restoredModel = sprint.ToSprintModel(isArchived: false);
            InitDatabase.UpdateSprint(restoredModel);

            // Reload data
            ReloadData();

            // Notify other apps to refresh
            await refreshSignal.Send(true);
        }

        async void DeleteSprint(Sprint sprint)
        {
            // Delete the sprint from database
            InitDatabase.DeleteSprint(sprint.Id);

            // Also update all items in the sprint to remove sprint reference
            var itemsInSprint = backlogItems.Value.Where(item => item.SprintId == sprint.Id).ToArray();
            foreach (var item in itemsInSprint)
            {
                var updated = item with { Status = ItemStatus.Backlog, SprintId = null };
                InitDatabase.UpdateBacklogItem(updated.ToBacklogItemModel());
            }

            // Reload data
            ReloadData();

            // Notify other apps to refresh
            await refreshSignal.Send(true);
        }

        if (isLoading.Value)
        {
            return new Card(Text.P("Loading..."));
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
            Text.H2("Sprint Archive"),

            archivedSprints.Value.Length == 0 ?
                new Card(
                    Text.P("No archived sprints yet. Archive a sprint from the Backlog or Sprint Board tab to see it here.")
                ).Width(Size.Fit()) :
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
                                makeCurrentAction: () => RestoreSprint(sprint),  // Active
                                deleteAction: () => DeleteSprint(sprint),        // Active
                                getIssueTypeBadge: GetIssueTypeBadge
                            );
                        })
                        .ToArray()
                ).Gap(6).Width(Size.Half())
        ).Gap(10);
    }
}
