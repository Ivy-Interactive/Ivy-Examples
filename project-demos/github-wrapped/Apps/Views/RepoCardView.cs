namespace GitHubWrapped.Apps.Views;

using GitHubWrapped.Models;

public class RepoCardView : ViewBase
{
    private readonly RepoStats _repo;
    private readonly int _index;
    private readonly double _percentage;

    public RepoCardView(RepoStats repo, int index, double percentage)
    {
        _repo = repo;
        _index = index;
        _percentage = percentage;
    }

    public override object? Build()
    {
        var client = this.UseService<IClientProvider>();
        var animatedPercentage = this.UseState(0.0);
        var refresh = this.UseRefreshToken();

        this.UseEffect(() =>
        {
            if (_repo.CommitCount == 0) return;

            var finalValue = _percentage;
            var steps = 30;
            var delay = _index * 50;

            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);

                for (int i = 0; i <= steps; i++)
                {
                    var currentValue = Math.Round((i / (double)steps) * finalValue, 1);
                    animatedPercentage.Set(currentValue);
                    refresh.Refresh();
                    await Task.Delay(30);
                }

                animatedPercentage.Set(finalValue);
                refresh.Refresh();
            });
        });

        return new Card(Layout.Vertical().AlignContent(Align.Center)
                | Text.H2($"{animatedPercentage.Value}%").Bold().Italic()
                | Text.Block($"{_repo.CommitCount} commits").Muted())
            .Title($"{_repo.Name}")
            .Icon(Icons.Folder)
            .OnClick(_ => client.OpenUrl(_repo.HtmlUrl));
    }
}
