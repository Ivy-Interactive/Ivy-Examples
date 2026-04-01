namespace GitHubWrapped.Apps.Views;

public class MonthlyChartRowView : ViewBase
{
    private readonly string _month;
    private readonly int _count;
    private readonly int _progressValue;
    private readonly bool _isPeak;
    private readonly int _index;

    public MonthlyChartRowView(string month, int count, int maxCommits, bool isPeak, int index)
    {
        _month = month;
        _count = count;
        _progressValue = maxCommits > 0 ? (int)Math.Round((count / (double)maxCommits) * 100) : 0;
        _isPeak = isPeak;
        _index = index;
    }

    public override object? Build()
    {
        var progressState = this.UseState(0);

        this.UseEffect(() =>
        {
            if (_count == 0) return;

            var finalValue = _progressValue;
            var steps = 30;
            var delay = _index * 30;

            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);

                for (int i = 0; i <= steps; i++)
                {
                    var currentValue = (int)Math.Round((i / (double)steps) * finalValue);
                    progressState.Set(currentValue);
                    await Task.Delay(30);
                }

                progressState.Set(finalValue);
            });
        });

        if (_count == 0)
        {
            return Layout.Horizontal().Gap(2).AlignContent(Align.Center)
                   | Text.Block(_month).Width(10).Muted()
                   | new Progress(progressState)
                       .Height(Size.Units(4))
                   | Text.Block("0").Width(10).Muted();
        }

        return Layout.Horizontal().Gap(2).AlignContent(Align.Center)
               | Text.Block(_month).Width(10).Bold(_isPeak)
               | new Progress(progressState)
                   .Goal(_count > 0 ? _count.ToString() : null)
               | Text.Block(_count.ToString()).Width(10).Bold(_isPeak);
    }
}
