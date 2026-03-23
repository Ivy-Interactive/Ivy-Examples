namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class StarshipDetailView : ViewBase
{
    private readonly string _url;
    private readonly SwapiService _swapi;
    private readonly Action _onClose;

    public StarshipDetailView(string url, SwapiService swapi, Action onClose)
    {
        _url = url;
        _swapi = swapi;
        _onClose = onClose;
    }

    public override object? Build()
    {
        var ship = UseQuery(("starship", _url), async ct => await _swapi.GetAsync<Starship>(_url, ct));
        var pilots = UseQuery(
            () => ship.Value?.Pilots,
            async (pilotUrls, ct) => await _swapi.ResolveNamesAsync<Person>(pilotUrls, p => p.Name, ct));
        var films = UseQuery(
            () => ship.Value?.Films,
            async (filmUrls, ct) => await _swapi.ResolveNamesAsync<Film>(filmUrls, f => f.Title, ct));

        object content;
        if (ship.Loading)
        {
            content = Skeleton.Card();
        }
        else if (ship.Value is not { } s)
        {
            content = Callout.Error("Starship not found");
        }
        else
        {
            var details = new Details(new[]
            {
                new Detail("Model", s.Model, false),
                new Detail("Class", s.StarshipClass, false),
                new Detail("Manufacturer", s.Manufacturer, false),
                new Detail("Cost", s.CostInCredits != "unknown" ? $"{s.CostInCredits} credits" : "Unknown", false),
                new Detail("Length", s.Length != "unknown" ? $"{s.Length} m" : "Unknown", false),
                new Detail("Max Speed", s.MaxAtmospheringSpeed != "n/a" ? s.MaxAtmospheringSpeed : "N/A", false),
                new Detail("Hyperdrive", s.HyperdriveRating, false),
                new Detail("MGLT", s.MGLT, false),
                new Detail("Crew", s.Crew, false),
                new Detail("Passengers", s.Passengers, false),
                new Detail("Cargo Capacity", s.CargoCapacity != "unknown" ? s.CargoCapacity : "Unknown", false),
                new Detail("Consumables", s.Consumables, false),
            });

            var pilotsSection = Layout.Vertical().Gap(2)
                | Text.H4("Pilots")
                | (pilots.Loading
                    ? (object)Text.Muted("Loading pilots...")
                    : pilots.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | pilots.Value.Select(p => new Badge(p.Name, icon: Icons.User).Info() as object).ToArray())
                        : (object)Text.Muted("No known pilots"));

            var filmsSection = Layout.Vertical().Gap(2)
                | Text.H4("Films")
                | (films.Loading
                    ? (object)Text.Muted("Loading films...")
                    : films.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | films.Value.Select(f => new Badge(f.Name).Outline() as object).ToArray())
                        : (object)Text.Muted("None"));

            content = Layout.Vertical()
                | Text.H2(s.Name)
                | details
                | pilotsSection
                | filmsSection;
        }

        return new Sheet(_onClose, content);
    }
}
