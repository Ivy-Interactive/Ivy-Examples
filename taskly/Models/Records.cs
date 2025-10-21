using System.Collections.Immutable;

namespace Taskly.Models;

public record BacklogItem(
    int Id,
    string Title,
    string Description,
    int StoryPoints,
    int Priority,
    ItemStatus Status,
    IssueType Type,
    int? SprintId = null,
    int? ParentId = null,
    bool IsTutorial = false
);

public record Sprint(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string Goal,
    ImmutableArray<int> ItemIds,
    bool IsTutorial = false
);

public record Developer(
    int Id,
    string Name,
    int Capacity
);

public enum ItemStatus
{
    Backlog,
    Todo,
    InProgress,
    Review,
    Done
}

public enum IssueType
{
    Task,
    Bug,
    Story,
    Epic
}
