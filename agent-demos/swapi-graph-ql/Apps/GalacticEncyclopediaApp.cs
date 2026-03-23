namespace SWAPI.Graph.QL.Apps;

using Ivy;
using SWAPI.Graph.QL.Apps.Views;

[App(title: "Galactic Encyclopedia", icon: Icons.Globe, group: new[] { "Star Wars" })]
public class GalacticEncyclopediaApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Tabs(
            new Tab("Characters", new CharactersView()).Icon(Icons.Users),
            new Tab("Starships", new StarshipsView()).Icon(Icons.Rocket),
            new Tab("Planets", new PlanetsView()).Icon(Icons.Earth)
        );
    }
}
