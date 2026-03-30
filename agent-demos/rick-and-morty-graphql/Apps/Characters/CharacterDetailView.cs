using Ivy;
using RickAndMortyGraphQL.Services;

namespace RickAndMortyGraphQL.Apps.Characters;

public class CharacterDetailView : ViewBase
{
    private readonly Character _character;

    public CharacterDetailView(Character character)
    {
        _character = character;
    }

    public override object? Build()
    {
        var c = _character;

        var statusBadge = c.Status switch
        {
            "Alive" => new Badge("Alive", icon: Icons.Heart).Success(),
            "Dead" => new Badge("Dead", icon: Icons.Skull).Destructive(),
            _ => new Badge(c.Status).Secondary()
        };

        var episodeList = Layout.Vertical().Gap(2);
        foreach (var ep in c.Episode)
        {
            episodeList |= Layout.Horizontal().Gap(2)
                | new Badge(ep.Episode1 ?? "?").Outline().Small()
                | Text.P(ep.Name).Small();
        }

        return Layout.Vertical()
            | new Image(c.Image)
                .Width(Size.Full())
                .Height(Size.Units(64))
            | (Layout.Horizontal().Gap(2)
                | statusBadge
                | new Badge(c.Species).Outline()
                | new Badge(c.Gender, icon: Icons.User).Secondary())
            | new Separator()
            | Text.H4("Origin")
            | new Details(new[]
            {
                new Detail("Name", c.Origin.Name ?? "Unknown", false),
                new Detail("Type", c.Origin.Type ?? "—", false),
                new Detail("Dimension", c.Origin.Dimension ?? "—", false)
            })
            | new Separator()
            | Text.H4("Location")
            | new Details(new[]
            {
                new Detail("Name", c.Location.Name ?? "Unknown", false),
                new Detail("Type", c.Location.Type ?? "—", false),
                new Detail("Dimension", c.Location.Dimension ?? "—", false)
            })
            | new Separator()
            | Text.H4($"Episodes ({c.Episode.Count})")
            | episodeList;
    }
}
