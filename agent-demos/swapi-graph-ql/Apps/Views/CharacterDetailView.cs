namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class CharacterDetailView : ViewBase
{
    private readonly string _url;
    private readonly SwapiService _swapi;
    private readonly Action _onClose;

    public CharacterDetailView(string url, SwapiService swapi, Action onClose)
    {
        _url = url;
        _swapi = swapi;
        _onClose = onClose;
    }

    public override object? Build()
    {
        var person = UseQuery(("person", _url), async ct => await _swapi.GetAsync<Person>(_url, ct));
        var homeworld = UseQuery(
            () => person.Value?.Homeworld,
            async (hw, ct) => await _swapi.GetAsync<Planet>(hw, ct));
        var films = UseQuery(
            () => person.Value?.Films,
            async (filmUrls, ct) => await _swapi.ResolveNamesAsync<Film>(filmUrls, f => f.Title, ct));
        var starships = UseQuery(
            () => person.Value?.Starships,
            async (shipUrls, ct) => await _swapi.ResolveNamesAsync<Starship>(shipUrls, s => s.Name, ct));

        object content;
        if (person.Loading)
        {
            content = Skeleton.Card();
        }
        else if (person.Value is not { } p)
        {
            content = Callout.Error("Character not found");
        }
        else
        {
            var details = new
            {
                BirthYear = p.BirthYear,
                Gender = p.Gender,
                Height = p.Height != "unknown" ? $"{p.Height} cm" : "Unknown",
                Mass = p.Mass != "unknown" ? $"{p.Mass} kg" : "Unknown",
                HairColor = p.HairColor,
                SkinColor = p.SkinColor,
                EyeColor = p.EyeColor,
                Homeworld = homeworld.Value?.Name ?? "Loading..."
            }.ToDetails();

            var filmsSection = Layout.Vertical().Gap(2)
                | Text.H4("Films")
                | (films.Loading
                    ? (object)Text.Muted("Loading films...")
                    : films.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | films.Value.Select(f => new Badge(f.Name).Info() as object).ToArray())
                        : (object)Text.Muted("None"));

            var starshipsSection = Layout.Vertical().Gap(2)
                | Text.H4("Starships Piloted")
                | (starships.Loading
                    ? (object)Text.Muted("Loading starships...")
                    : starships.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | starships.Value.Select(s => new Badge(s.Name, icon: Icons.Rocket).Outline() as object).ToArray())
                        : (object)Text.Muted("None"));

            content = Layout.Vertical()
                | Text.H2(p.Name)
                | details
                | filmsSection
                | starshipsSection;
        }

        return new Sheet(_onClose, content);
    }
}
