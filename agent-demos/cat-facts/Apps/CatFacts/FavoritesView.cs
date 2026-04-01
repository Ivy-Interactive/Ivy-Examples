using Ivy;

namespace CatFacts.Apps.CatFacts;

public class FavoritesView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CatFactApiService>();
        var client = UseService<IClientProvider>();
        var favorites = service.FavoriteFacts;

        if (favorites.Count == 0)
        {
            return Layout.Center()
                | (Layout.Vertical().Gap(2).Center()
                    | Icons.Heart.ToIcon().Large().Color(Colors.Gray)
                    | Text.P("No favorite facts yet").Color(Colors.Gray)
                    | Text.Muted("Go to the Daily Fact tab and heart some facts!"));
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H2("❤️ Favorite Facts")
                | Text.Muted($"You have {favorites.Count} favorite fact{(favorites.Count != 1 ? "s" : "")}")
                | new Separator()
                | new Fragment(favorites.Select(fact =>
                    (object)(new Card()
                        | (Layout.Horizontal()
                            | (Layout.Vertical().Width(Size.Full())
                                | Text.P(fact))
                            | new Button("Remove", () =>
                            {
                                service.RemoveFavorite(fact);
                                client.Toast("Removed from favorites");
                            }).Icon(Icons.Trash2)
                              .Variant(ButtonVariant.Destructive).Ghost()
                              .WithConfirm("Remove this fact from favorites?", title: "Remove Favorite", confirmLabel: "Remove", destructive: true)))).ToArray()));
    }
}
