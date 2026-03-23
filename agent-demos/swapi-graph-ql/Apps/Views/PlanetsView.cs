namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class PlanetsView : ViewBase
{
    public override object? Build()
    {
        var swapi = UseService<SwapiService>();
        var search = UseState("");
        var selectedUrl = UseState<string?>(null);

        var query = UseQuery("all-planets", async ct => await swapi.GetAllAsync<Planet>("planets", ct));

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var planets = query.Value ?? new List<Planet>();
        var filtered = string.IsNullOrWhiteSpace(search.Value)
            ? planets
            : planets.Where(p => p.Name.Contains(search.Value, StringComparison.OrdinalIgnoreCase)).ToList();

        var content = Layout.Vertical()
            | (Layout.Horizontal().Align(Align.Left)
                | Text.H2("Planets")
                | new Badge($"{planets.Count} total").Secondary())
            | search.ToTextInput().Placeholder("Search planets...")
            | (Layout.Grid().Columns(3).Gap(3)
                | filtered.Select(p =>
                    new Card()
                        .Title(p.Name)
                        .Description($"{p.Climate} · Pop: {p.Population}")
                        .OnClick(() => selectedUrl.Set(p.Url))
                    as object
                ).ToArray());

        if (selectedUrl.Value != null)
        {
            content |= new PlanetDetailView(selectedUrl.Value, swapi, () => selectedUrl.Set(null));
        }

        return content;
    }
}
