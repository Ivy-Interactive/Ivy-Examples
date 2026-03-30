using CatFacts.Apps.CatFacts;
using Ivy;

namespace CatFacts.Apps;

[App(icon: Icons.Cat)]
public class CatFactsApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Tabs(
            new Tab("Daily Fact", new DailyFactView()).Icon(Icons.Sparkles),
            new Tab("Breeds", new BreedsView()).Icon(Icons.PawPrint),
            new Tab("Favorites", new FavoritesView()).Icon(Icons.Heart)
        );
    }
}
