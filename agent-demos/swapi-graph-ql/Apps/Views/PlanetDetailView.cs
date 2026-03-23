namespace SWAPI.Graph.QL.Apps.Views;

using Ivy;
using SWAPI.Graph.QL.Apps.Models;
using SWAPI.Graph.QL.Apps.Services;

public class PlanetDetailView : ViewBase
{
    private readonly string _url;
    private readonly SwapiService _swapi;
    private readonly Action _onClose;

    public PlanetDetailView(string url, SwapiService swapi, Action onClose)
    {
        _url = url;
        _swapi = swapi;
        _onClose = onClose;
    }

    public override object? Build()
    {
        var planet = UseQuery(("planet", _url), async ct => await _swapi.GetAsync<Planet>(_url, ct));
        var residents = UseQuery(
            () => planet.Value?.Residents,
            async (residentUrls, ct) => await _swapi.ResolveNamesAsync<Person>(residentUrls, p => p.Name, ct));
        var films = UseQuery(
            () => planet.Value?.Films,
            async (filmUrls, ct) => await _swapi.ResolveNamesAsync<Film>(filmUrls, f => f.Title, ct));

        object content;
        if (planet.Loading)
        {
            content = Skeleton.Card();
        }
        else if (planet.Value is not { } p)
        {
            content = Callout.Error("Planet not found");
        }
        else
        {
            var details = new
            {
                Climate = p.Climate,
                Terrain = p.Terrain,
                Gravity = p.Gravity,
                Diameter = p.Diameter != "unknown" ? $"{p.Diameter} km" : "Unknown",
                RotationPeriod = p.RotationPeriod != "unknown" ? $"{p.RotationPeriod} hours" : "Unknown",
                OrbitalPeriod = p.OrbitalPeriod != "unknown" ? $"{p.OrbitalPeriod} days" : "Unknown",
                SurfaceWater = p.SurfaceWater != "unknown" ? $"{p.SurfaceWater}%" : "Unknown",
                Population = p.Population
            }.ToDetails();

            var residentsSection = Layout.Vertical().Gap(2)
                | Text.H4("Notable Residents")
                | (residents.Loading
                    ? (object)Text.Muted("Loading residents...")
                    : residents.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | residents.Value.Select(r => new Badge(r.Name, icon: Icons.User).Info() as object).ToArray())
                        : (object)Text.Muted("No known residents"));

            var filmsSection = Layout.Vertical().Gap(2)
                | Text.H4("Films")
                | (films.Loading
                    ? (object)Text.Muted("Loading films...")
                    : films.Value?.Count > 0
                        ? (Layout.Wrap().Gap(2)
                            | films.Value.Select(f => new Badge(f.Name).Outline() as object).ToArray())
                        : (object)Text.Muted("None"));

            content = Layout.Vertical()
                | Text.H2(p.Name)
                | details
                | residentsSection
                | filmsSection;
        }

        return new Sheet(_onClose, content);
    }
}
