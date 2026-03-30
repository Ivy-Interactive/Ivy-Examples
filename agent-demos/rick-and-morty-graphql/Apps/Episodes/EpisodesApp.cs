using Ivy;
using RickAndMortyGraphQL.Services;

namespace RickAndMortyGraphQL.Apps.Episodes;

[App(icon: Icons.Tv)]
public class EpisodesApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<RickAndMortyClient>();
        var page = UseState(1);
        var search = UseState("");

        var query = UseQuery(
            key: ("episodes", page.Value, search.Value),
            fetcher: async ct => await client.GetEpisodesAsync(page.Value, string.IsNullOrWhiteSpace(search.Value) ? null : search.Value, ct),
            options: new QueryOptions { KeepPrevious = true }
        );

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | Text.H1("Episodes")
                | Text.Muted("Browse all Rick and Morty episodes by season")
                | search.ToTextInput().Placeholder("Search episodes by name...")
                | BuildContent(query, page)
            );
    }

    private object BuildContent(QueryResult<EpisodesResult> query, IState<int> page)
    {
        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var result = query.Value;
        if (result?.Results == null || result.Results.Count == 0)
            return Callout.Warning("No episodes found.");

        var seasons = result.Results
            .GroupBy(e => ExtractSeason(e.Episode1))
            .OrderBy(g => g.Key);

        var layout = Layout.Vertical().Gap(6);

        foreach (var season in seasons)
        {
            var seasonLayout = Layout.Vertical().Gap(2);
            seasonLayout |= Text.H2($"Season {season.Key}");

            foreach (var ep in season)
            {
                seasonLayout |= BuildEpisodeCard(ep);
            }

            layout |= seasonLayout;
        }

        layout |= BuildPagination(page, result.Info);

        return layout;
    }

    private static object BuildEpisodeCard(Episode ep)
    {
        var characterCount = ep.Characters?.Count ?? 0;

        var header = Layout.Horizontal().Align(Align.Left)
            | new Badge(ep.Episode1 ?? "?").Color(Colors.Primary)
            | Text.P(ep.Name ?? "Unknown").Bold()
            | Text.Muted(ep.AirDate ?? "")
            | new Badge($"{characterCount} characters").Color(Colors.Zinc);

        if (ep.Characters is { Count: > 0 })
        {
            var avatars = Layout.Wrap().Gap(2);
            foreach (var ch in ep.Characters)
            {
                avatars |= new Avatar(ch.Name, ch.Image)
                    .WithTooltip($"{ch.Name} ({ch.Status})");
            }

            return new Expandable(header, avatars);
        }

        return new Card(header);
    }

    private static object BuildPagination(IState<int> page, EpisodeInfo info)
    {
        return Layout.Horizontal().Align(Align.Center)
            | new Button("Previous", () => page.Set(page.Value - 1))
                .Disabled(page.Value <= 1)
            | Text.P($"Page {page.Value} of {info.Pages}")
            | new Button("Next", () => page.Set(page.Value + 1))
                .Disabled(page.Value >= info.Pages);
    }

    private static int ExtractSeason(string? episodeCode)
    {
        if (string.IsNullOrEmpty(episodeCode)) return 0;
        var sIndex = episodeCode.IndexOf('S');
        var eIndex = episodeCode.IndexOf('E', sIndex + 1);
        if (sIndex >= 0 && eIndex > sIndex && int.TryParse(episodeCode[(sIndex + 1)..eIndex], out var season))
            return season;
        return 0;
    }
}
