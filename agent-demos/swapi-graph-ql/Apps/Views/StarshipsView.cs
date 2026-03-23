namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class StarshipsView : ViewBase
{
    public override object? Build()
    {
        var swapi = UseService<SwapiService>();
        var search = UseState("");
        var selectedUrl = UseState<string?>(null);

        var query = UseQuery("all-starships", async ct => await swapi.GetAllAsync<Starship>("starships", ct));

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var ships = query.Value ?? new List<Starship>();
        var filtered = string.IsNullOrWhiteSpace(search.Value)
            ? ships
            : ships.Where(s => s.Name.Contains(search.Value, StringComparison.OrdinalIgnoreCase)).ToList();

        var content = Layout.Vertical()
            | (Layout.Horizontal().Align(Align.Left)
                | Text.H2("Starships")
                | new Badge($"{ships.Count} total").Secondary())
            | search.ToTextInput().Placeholder("Search starships...")
            | (Layout.Grid().Columns(3).Gap(3)
                | filtered.Select(s =>
                    new Card()
                        .Title(s.Name)
                        .Description($"{s.StarshipClass} · {s.Manufacturer}")
                        .OnClick(() => selectedUrl.Set(s.Url))
                    as object
                ).ToArray());

        if (selectedUrl.Value != null)
        {
            content |= new StarshipDetailView(selectedUrl.Value, swapi, () => selectedUrl.Set(null));
        }

        return content;
    }
}
