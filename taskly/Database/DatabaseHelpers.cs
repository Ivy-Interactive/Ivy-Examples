using Taskly.Models;
using System.Collections.Immutable;

namespace Taskly.Database;

/// <summary>
/// Helper methods to convert between database models and UI models
/// </summary>
public static class DatabaseHelpers
{
    // Convert database model to UI model
    public static BacklogItem ToBacklogItem(this BacklogItemModel model)
    {
        return new BacklogItem(
            Id: model.Id,
            Title: model.Title,
            Description: model.Description ?? string.Empty,
            StoryPoints: model.StoryPoints,
            Priority: model.Priority,
            Status: Enum.Parse<ItemStatus>(model.Status),
            Type: Enum.Parse<IssueType>(model.Type),
            SprintId: model.SprintId,
            ParentId: model.ParentId
        );
    }

    // Convert UI model to database model
    public static BacklogItemModel ToBacklogItemModel(this BacklogItem item)
    {
        return new BacklogItemModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            StoryPoints = item.StoryPoints,
            Priority = item.Priority,
            Status = item.Status.ToString(),
            Type = item.Type.ToString(),
            SprintId = item.SprintId,
            ParentId = item.ParentId
        };
    }

    // Convert database model to UI model
    public static Sprint ToSprint(this SprintModel model)
    {
        return new Sprint(
            Id: model.Id,
            Name: model.Name,
            StartDate: model.StartDate,
            EndDate: model.EndDate,
            Goal: model.Goal ?? string.Empty,
            ItemIds: model.ItemIds?.ToImmutableArray() ?? ImmutableArray<int>.Empty
        );
    }

    // Convert UI model to database model
    public static SprintModel ToSprintModel(this Sprint sprint)
    {
        return new SprintModel
        {
            Id = sprint.Id,
            Name = sprint.Name,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Goal = sprint.Goal,
            ItemIds = sprint.ItemIds.ToList(),
            IsArchived = 0  // Not archived by default (SQLite uses 0/1 for boolean)
        };
    }

    // Convert UI model to database model with archive flag
    public static SprintModel ToSprintModel(this Sprint sprint, bool isArchived)
    {
        return new SprintModel
        {
            Id = sprint.Id,
            Name = sprint.Name,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Goal = sprint.Goal,
            ItemIds = sprint.ItemIds.ToList(),
            IsArchived = isArchived ? 1 : 0  // Convert boolean to int for SQLite
        };
    }
}
