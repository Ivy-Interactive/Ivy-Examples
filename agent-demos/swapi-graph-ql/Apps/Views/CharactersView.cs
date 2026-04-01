namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class CharactersView : ViewBase
{
    public override object? Build()
    {
        var swapi = UseService<SwapiService>();
        var search = UseState("");
        var selectedUrl = UseState<string?>(null);

        var query = UseQuery("all-people", async ct => await swapi.GetAllAsync<Person>("people", ct));

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var people = query.Value ?? new List<Person>();
        var filtered = string.IsNullOrWhiteSpace(search.Value)
            ? people
            : people.Where(p => p.Name.Contains(search.Value, StringComparison.OrdinalIgnoreCase)).ToList();

        var content = Layout.Vertical()
            | (Layout.Horizontal().Left()
                | Text.H2("Characters")
                | new Badge($"{people.Count} total").Secondary())
            | search.ToTextInput().Placeholder("Search characters...")
            | (Layout.Grid().Columns(3).Gap(3)
                | filtered.Select(p =>
                    new Card()
                        .Title(p.Name)
                        .Description($"{p.Gender} · {p.BirthYear}")
                        .OnClick(() => selectedUrl.Set(p.Url))
                    as object
                ).ToArray());

        if (selectedUrl.Value != null)
        {
            content |= new CharacterDetailView(selectedUrl.Value, swapi, () => selectedUrl.Set(null));
        }

        return content;
    }
}
