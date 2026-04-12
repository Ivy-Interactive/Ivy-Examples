using Ivy;
using RickAndMortyGraphQL.Services;

namespace RickAndMortyGraphQL.Apps.Characters;

[App(icon: Icons.Users)]
public class CharactersApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<RickAndMortyClient>();

        var name = UseState("");
        var status = UseState("");
        var species = UseState("");
        var gender = UseState("");
        var page = UseState(1);
        var selectedCharacter = UseState<Character?>(null);
        var isSheetOpen = UseState(false);

        var query = UseQuery(
            key: ("characters", name.Value, status.Value, species.Value, gender.Value, page.Value),
            fetcher: async (ct) => await client.GetCharactersAsync(
                page.Value,
                string.IsNullOrWhiteSpace(name.Value) ? null : name.Value,
                string.IsNullOrWhiteSpace(status.Value) ? null : status.Value,
                string.IsNullOrWhiteSpace(species.Value) ? null : species.Value,
                string.IsNullOrWhiteSpace(gender.Value) ? null : gender.Value,
                ct),
            options: new QueryOptions { KeepPrevious = true }
        );

        var statusOptions = new[] { "", "Alive", "Dead", "unknown" }.ToOptions();
        var genderOptions = new[] { "", "Female", "Male", "Genderless", "unknown" }.ToOptions();


        var filters = Layout.Horizontal().Gap(2)
            | name.ToTextInput().Placeholder("Search by name...").Width(Size.Units(60))
            | status.ToSelectInput(statusOptions).Placeholder("Status").Width(Size.Units(40))
            | species.ToTextInput().Placeholder("Species...").Width(Size.Units(40))
            | gender.ToSelectInput(genderOptions).Placeholder("Gender").Width(Size.Units(40));


        object content;
        if (query.Loading)
        {
            content = Skeleton.Card();
        }
        else if (query.Error is { } error)
        {
            content = Callout.Error(error.Message);
        }
        else
        {
            var data = query.Value;
            var totalPages = data?.Info.Pages ?? 1;
            var characters = data?.Results ?? [];


            var grid = Layout.Grid().Columns(4);
            foreach (var c in characters)
            {
                var statusBadge = c.Status switch
                {
                    "Alive" => new Badge("Alive", icon: Icons.Heart).Success(),
                    "Dead" => new Badge("Dead", icon: Icons.Skull).Destructive(),
                    _ => new Badge(c.Status).Secondary()
                };

                var card = new Card(
                    Layout.Vertical().Gap(2)
                        | new Image(c.Image).Width(Size.Full()).Height(Size.Units(48))
                        | Text.P(c.Name).Bold()
                        | statusBadge
                        | Text.Muted(c.Species)
                        | (Layout.Vertical().Gap(1)
                            | Text.Muted($"Origin: {c.Origin.Name ?? "Unknown"}").Small()
                            | Text.Muted($"Location: {c.Location.Name ?? "Unknown"}").Small())
                ).OnClick(() =>
                {
                    selectedCharacter.Set(c);
                    isSheetOpen.Set(true);
                });

                grid |= card;
            }


            var pagination = Layout.Horizontal().AlignContent(Align.Center).Gap(2)
                | new Button("Previous", () => page.Set(Math.Max(1, page.Value - 1)))
                    .Outline()
                    .Disabled(page.Value <= 1)
                | Text.P($"Page {page.Value} of {totalPages}")
                | new Button("Next", () => page.Set(Math.Min(totalPages, page.Value + 1)))
                    .Outline()
                    .Disabled(page.Value >= totalPages);

            content = Layout.Vertical()
                | grid
                | pagination;
        }

        object? sheet = null;
        if (selectedCharacter.Value is { } sel)
        {
            sheet = new CharacterDetailView(sel)
                .ToSheet(isSheetOpen, sel.Name, $"{sel.Status} • {sel.Species}", width: Size.Units(120));
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(300)).Margin(10)
                | Text.H1("Characters")
                | Text.Lead("Browse all Rick and Morty characters")
                | filters
                | content
                | sheet);
    }
}
