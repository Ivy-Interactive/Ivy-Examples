using Ivy;
using CTRF.Apps.Models;

namespace CTRF.Apps;

public class TestDetailView : ViewBase
{
    private readonly CtrfTest _test;

    public TestDetailView(CtrfTest test) => _test = test;

    public override object? Build()
    {
        var sections = Layout.Vertical().Gap(3);


        sections = sections | (Layout.Horizontal().Gap(3).AlignContent(Align.Center)
            | Text.H4(_test.Name)
            | StatusBadge(_test.Status)
            | (_test.Flaky == true ? new Badge("Flaky", icon: Icons.Shuffle).Warning() : (object)Layout.Vertical()));


        if (!string.IsNullOrEmpty(_test.Message))
        {
            sections = sections | Callout.Error(_test.Message, "Error");
        }

        if (!string.IsNullOrEmpty(_test.Trace))
        {
            sections = sections | new Expandable("Stack Trace",
                Text.Monospaced(_test.Trace));
        }


        if (_test.Stdout?.Count > 0)
        {
            sections = sections | new Expandable("Stdout",
                Text.Monospaced(string.Join("\n", _test.Stdout)));
        }

        if (_test.Stderr?.Count > 0)
        {
            sections = sections | new Expandable("Stderr",
                Text.Monospaced(string.Join("\n", _test.Stderr)));
        }


        if (_test.RetryAttempts?.Count > 0)
        {
            var retryLayout = Layout.Vertical().Gap(2);
            foreach (var attempt in _test.RetryAttempts)
            {
                var badge = StatusBadge(attempt.Status);
                var row = Layout.Horizontal().Gap(3).AlignContent(Align.Center)
                    | Text.Block($"Attempt {attempt.Attempt}").Bold()
                    | badge
                    | Text.Muted(ReportDashboardView.FormatDuration(attempt.Duration));

                if (!string.IsNullOrEmpty(attempt.Message))
                    row = row | Text.Muted(attempt.Message);

                retryLayout = retryLayout | row;
            }
            sections = sections | new Expandable("Retry Attempts", retryLayout).Open();
        }


        if (_test.Attachments?.Count > 0)
        {
            var attachLayout = Layout.Vertical().Gap(1);
            foreach (var att in _test.Attachments)
            {
                attachLayout = attachLayout | (Layout.Horizontal().Gap(2)
                    | new Badge(att.ContentType).Outline().Small()
                    | Text.Block(att.Name));
            }
            sections = sections | new Expandable("Attachments", attachLayout);
        }


        if (_test.Steps?.Count > 0)
        {
            var stepsLayout = Layout.Vertical().Gap(1);
            foreach (var step in _test.Steps)
            {
                stepsLayout = stepsLayout | (Layout.Horizontal().Gap(2)
                    | StatusBadge(step.Status)
                    | Text.Block(step.Name));
            }
            sections = sections | new Expandable("Steps", stepsLayout).Open();
        }


        if (!string.IsNullOrEmpty(_test.FilePath))
        {
            var fileInfo = _test.FilePath;
            if (_test.Line.HasValue) fileInfo += $":{_test.Line}";
            sections = sections | (Layout.Horizontal().Gap(2)
                | Text.Muted("File:").Bold()
                | Text.Monospaced(fileInfo));
        }

        return new Card().Content(sections);
    }

    private static object StatusBadge(string status) => status switch
    {
        "passed" => new Badge("Passed").Success(),
        "failed" => new Badge("Failed").Destructive(),
        "skipped" => new Badge("Skipped").Warning(),
        "pending" => new Badge("Pending").Secondary(),
        _ => new Badge(status).Outline()
    };
}
