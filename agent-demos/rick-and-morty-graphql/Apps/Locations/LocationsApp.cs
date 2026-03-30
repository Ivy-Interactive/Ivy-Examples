using Ivy;
using RickAndMortyGraphQL.Services;

namespace RickAndMortyGraphQL.Apps.Locations;

[App(icon: Icons.Globe)]
public class LocationsApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<RickAndMortyClient>();

        var page = UseState(1);
        var search = UseState("");
        var typeFilter = UseState("");
        var dimensionFilter = UseState("");
        var selectedLocation = UseState<Location?>(null);

        var query = UseQuery(
            key: $"locations?page={page.Value}&name={search.Value}&type={typeFilter.Value}&dim={dimensionFilter.Value}",
            fetcher: async ct => await client.GetLocationsAsync(
                page.Value,
                string.IsNullOrWhiteSpace(search.Value) ? null : search.Value,
                string.IsNullOrWhiteSpace(typeFilter.Value) ? null : typeFilter.Value,
                string.IsNullOrWhiteSpace(dimensionFilter.Value) ? null : dimensionFilter.Value,
                ct),
            options: new QueryOptions { KeepPrevious = true }
        );

        object BuildContent()
        {
            if (query.Loading) return Skeleton.Card();
            if (query.Error is { } error) return Callout.Error(error.Message);

            var result = query.Value;
            if (result?.Results is not { Count: > 0 })
                return Callout.Info("No locations found.");

            var cards = result.Results.Select(loc =>
                new Card(
                    Layout.Vertical().Gap(2)
                        | (Layout.Horizontal()
                            | Text.Muted("Type:")
                            | Text.P(string.IsNullOrEmpty(loc.Type) ? "Unknown" : loc.Type))
                        | (Layout.Horizontal()
                            | Text.Muted("Dimension:")
                            | Text.P(string.IsNullOrEmpty(loc.Dimension) ? "Unknown" : loc.Dimension))
                        | new Badge($"{loc.Residents?.Count ?? 0} Residents").Color(Colors.Info)
                ).Title(loc.Name)
                 .OnClick(() => selectedLocation.Set(loc))
            );

            return Layout.Grid().Columns(2) | cards.ToArray();
        }

        var totalPages = query.Value?.Info.Pages ?? 1;

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | Text.H1("Locations")
                | Text.Lead("Explore locations across the Rick and Morty multiverse")
                | (Layout.Horizontal()
                    | search.ToTextInput().Placeholder("Search by name...")
                    | typeFilter.ToTextInput().Placeholder("Filter by type...")
                    | dimensionFilter.ToTextInput().Placeholder("Filter by dimension..."))
                | BuildContent()
                | (Layout.Horizontal().Align(Align.Center)
                    | new Button("Previous").OnClick(() => page.Set(Math.Max(1, page.Value - 1))).Disabled(page.Value <= 1)
                    | Text.P($"Page {page.Value} of {totalPages}")
                    | new Button("Next").OnClick(() => page.Set(page.Value + 1)).Disabled(page.Value >= totalPages))
                | (selectedLocation.Value is { } loc
                    ? new Sheet(
                        () => selectedLocation.Set(null),
                        BuildResidentsList(loc),
                        title: loc.Name,
                        description: $"{loc.Type} — {loc.Dimension}"
                    ).Width(Size.Fraction(1 / 3f))
                    : null));
    }

    private static object BuildResidentsList(Location location)
    {
        var residents = location.Residents;
        if (residents is not { Count: > 0 })
            return Text.Muted("No known residents.");

        return Layout.Vertical().Gap(2)
            | residents.Select(r =>
                (object)(Layout.Horizontal().Gap(3)
                    | new Avatar(r.Image)
                    | (Layout.Vertical().Gap(1)
                        | Text.P(r.Name).Bold()
                        | (Layout.Horizontal().Gap(2)
                            | new Badge(r.Status).Color(r.Status switch
                            {
                                "Alive" => Colors.Green,
                                "Dead" => Colors.Red,
                                _ => Colors.Gray
                            })
                            | Text.Muted(r.Species)))
                    | new Separator())
            ).ToArray();
    }
}
