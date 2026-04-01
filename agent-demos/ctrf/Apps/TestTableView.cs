using Ivy;
using CTRF.Apps.Models;

namespace CTRF.Apps;

public class TestTableView : ViewBase
{
    private readonly List<CtrfTest> _tests;

    public TestTableView(List<CtrfTest> tests) => _tests = tests;

    public override object? Build()
    {
        var statusFilter = UseState("");
        var searchText = UseState("");
        var expandedIdx = UseState(-1);

        var filtered = _tests.Where(t =>
        {
            if (!string.IsNullOrEmpty(statusFilter.Value) && t.Status != statusFilter.Value)
                return false;
            if (!string.IsNullOrEmpty(searchText.Value) &&
                !t.Name.Contains(searchText.Value, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }).ToList();

        var statuses = _tests.Select(t => t.Status).Distinct().OrderBy(s => s).ToList();

        var rows = filtered.Select((t, i) => new TestRowModel
        {
            Status = t.Status,
            Name = t.Name,
            Suite = t.SuitePath,
            Duration = ReportDashboardView.FormatDuration(t.Duration),
            Flaky = t.Flaky == true ? "Yes" : "",
            Retries = t.Retries > 0 ? t.Retries.Value.ToString() : "",
            Index = i
        }).ToList();

        var filters = Layout.Horizontal().Gap(3).AlignContent(Align.Center)
            | searchText.ToTextInput().Placeholder("Search test name...").Width(Size.Units(60)).Small()
            | statusFilter.ToSelectInput(statuses.Select(s => new Option<string>(s, FormatStatusLabel(s))).ToArray())
                .Placeholder("All statuses").Width(Size.Units(48)).Small();

        var table = rows.AsQueryable().ToDataTable(r => r.Index)
            .Header(r => r.Status, "Status")
            .Header(r => r.Duration, "Duration")
            .Header(r => r.Flaky, "Flaky")
            .Header(r => r.Retries, "Retries")
            .Hidden(r => r.Index)
            .Config(c =>
            {
                c.AllowSorting = true;
                c.EnableCellClickEvents = true;
                c.ShowSearch = false;
            })
            .OnCellClick(e =>
            {
                var rowIdx = e.Value.RowIndex;
                expandedIdx.Set(expandedIdx.Value == rowIdx ? -1 : rowIdx);
                return ValueTask.CompletedTask;
            })
            .LoadAllRows();

        var content = Layout.Vertical().Gap(3)
            | filters
            | table;

        if (expandedIdx.Value >= 0 && expandedIdx.Value < filtered.Count)
        {
            content = content | new TestDetailView(filtered[expandedIdx.Value]);
        }

        return new Card()
            .Header(Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                | Text.H4("Test Results")
                | new Badge($"{filtered.Count} tests").Secondary())
            .Content(content);
    }

    public static object StatusBadge(string status) => status switch
    {
        "passed" => new Badge("Passed").Success(),
        "failed" => new Badge("Failed").Destructive(),
        "skipped" => new Badge("Skipped").Warning(),
        "pending" => new Badge("Pending").Secondary(),
        _ => new Badge(status).Outline()
    };

    private static string FormatStatusLabel(string status) => status switch
    {
        "passed" => "✓ Passed",
        "failed" => "✗ Failed",
        "skipped" => "⊘ Skipped",
        "pending" => "◷ Pending",
        _ => status
    };
}
