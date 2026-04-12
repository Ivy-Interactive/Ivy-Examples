using Ivy;

namespace CatFacts.Apps.CatFacts;

public class DailyFactView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CatFactApiService>();
        var client = UseService<IClientProvider>();
        var currentFact = UseState<string?>(null);
        var refreshToken = UseRefreshToken();

        var factQuery = UseQuery(
            key: ("daily-fact", refreshToken.Token),
            fetcher: async (ct) => await service.GetRandomFactAsync(ct)
        );

        if (factQuery.Loading && currentFact.Value == null)
            return Layout.TopCenter()
                | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                    | Skeleton.Card());

        var fact = factQuery.Value ?? currentFact.Value ?? "Loading...";
        if (factQuery.Value != null)
            currentFact.Set(factQuery.Value);

        var isFav = service.IsFavorite(fact);
        var seenCount = service.SeenFacts.Count;

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H2("🐱 Daily Cat Fact")
                | Text.Muted("Discover fun and interesting facts about cats")
                | new Separator()
                | (new Card()
                    | (Layout.Vertical().Gap(6)
                        | new Icon(Icons.Cat).Large()
                        | Text.P(fact).Large()
                        | new Separator()
                        | (Layout.Horizontal().Gap(2)
                            | new Button("New Fact", () =>
                            {
                                refreshToken.Refresh();
                            }).Icon(Icons.RefreshCw).Primary()
                            | new Button(isFav ? "Favorited" : "Favorite", () =>
                            {
                                var added = service.ToggleFavorite(fact);
                                client.Toast(added ? "Added to favorites! ❤️" : "Removed from favorites");
                            }).Icon(isFav ? Icons.Heart : Icons.HeartOff)
                              .Variant(isFav ? ButtonVariant.Primary : ButtonVariant.Outline))))
                | (Layout.Horizontal().Gap(6)
                    | (new Card()
                        | (Layout.Horizontal().Gap(2).Center()
                            | new Icon(Icons.Eye)
                            | Text.Block($"{seenCount} facts seen")))
                    | (new Card()
                        | (Layout.Horizontal().Gap(2).Center()
                            | new Icon(Icons.Heart)
                            | Text.Block($"{service.FavoriteFacts.Count} favorites")))));
    }
}
