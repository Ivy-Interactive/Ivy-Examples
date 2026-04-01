using System.Collections.Immutable;
using System.Text.Json;
using Ivy;
using CTRF.Apps.Models;

namespace CTRF.Apps;

[App(title: "CTRF Dashboard", icon: Icons.FlaskConical, group: new[] { "Testing" })]
public class CtrfDashboardApp : ViewBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public override object? Build()
    {
        var reports = UseState(ImmutableList<UploadedReport>.Empty);
        var selectedIndex = UseState(-1);
        var uploadError = UseState<string?>(null);

        var fileState = UseState<FileUpload<byte[]>?>();
        var upload = UseUpload(MemoryStreamUploadHandler.Create(fileState));

        UseEffect(() =>
        {
            var file = fileState.Value;
            if (file?.Status != FileUploadStatus.Finished || file.Content == null) return;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(file.Content);
                var report = JsonSerializer.Deserialize<CtrfReport>(json, JsonOpts);

                if (report == null || report.ReportFormat != "CTRF")
                {
                    uploadError.Set($"Invalid CTRF file: {file.FileName}");
                    fileState.Set(null);
                    return;
                }

                var uploaded = new UploadedReport
                {
                    FileName = file.FileName,
                    Report = report,
                    UploadedAt = DateTime.Now
                };

                var newList = reports.Value.Add(uploaded);
                reports.Set(newList);
                selectedIndex.Set(newList.Count - 1);
                uploadError.Set(null);
            }
            catch (JsonException ex)
            {
                uploadError.Set($"Failed to parse {file.FileName}: {ex.Message}");
            }
            finally
            {
                fileState.Set(null);
            }
        }, fileState);

        var sidebar = BuildSidebar(reports, selectedIndex, uploadError, fileState, upload);
        var main = selectedIndex.Value >= 0 && selectedIndex.Value < reports.Value.Count
            ? new ReportDashboardView(reports.Value[selectedIndex.Value])
            : (object)(Layout.Vertical().AlignContent(Align.Center).Height(Size.Full())
                | Text.H2("CTRF Dashboard").Color(Colors.Muted)
                | Text.P("Upload a CTRF JSON report to get started.").Color(Colors.Muted));

        return new SidebarLayout(
            mainContent: main,
            sidebarContent: sidebar,
            sidebarHeader: Text.H4("Reports")
        ).Resizable();
    }

    private object BuildSidebar(
        IState<ImmutableList<UploadedReport>> reports,
        IState<int> selectedIndex,
        IState<string?> uploadError,
        IState<FileUpload<byte[]>?> fileState,
        IState<UploadContext> upload)
    {
        var items = new List<object>();

        items.Add(fileState.ToFileInput(upload)
            .Placeholder("Upload CTRF JSON...")
            .Accept(".json")
            .Small());

        if (uploadError.Value != null)
            items.Add(Callout.Error(uploadError.Value));

        for (var i = 0; i < reports.Value.Count; i++)
        {
            var r = reports.Value[i];
            var idx = i;
            var summary = r.Report.Results.Summary;
            var isSelected = selectedIndex.Value == idx;

            var statusBadge = summary.Failed > 0
                ? new Badge($"{summary.Passed}✓ {summary.Failed}✗").Destructive()
                : new Badge($"{summary.Passed}✓").Success();

            var card = new Card()
                .Content(Layout.Vertical().Gap(1)
                    | (Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                        | (isSelected ? Text.Block(r.Report.Results.Tool.Name).Bold().Color(Colors.Primary) : Text.Block(r.Report.Results.Tool.Name).Bold())
                        | statusBadge)
                    | Text.Muted(r.FileName).Small()
                    | Text.Muted(r.UploadedAt.ToString("HH:mm:ss")).Small()
                );

            card = card.OnClick(() => selectedIndex.Set(idx));

            var deleteBtn = new Button("", () =>
            {
                var newList = reports.Value.RemoveAt(idx);
                reports.Set(newList);
                if (selectedIndex.Value == idx)
                    selectedIndex.Set(newList.Count > 0 ? Math.Min(idx, newList.Count - 1) : -1);
                else if (selectedIndex.Value > idx)
                    selectedIndex.Set(selectedIndex.Value - 1);
            }).Icon(Icons.Trash2).Variant(ButtonVariant.Ghost).Destructive().Small();

            items.Add(Layout.Horizontal().AlignContent(Align.Center)
                | card.Width(Size.Full())
                | deleteBtn);
        }

        var layout = Layout.Vertical().Gap(2);
        foreach (var item in items)
            layout = layout | item;
        return layout;
    }
}
