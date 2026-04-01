using Ivy;

namespace CatFacts.Apps.CatFacts;

public class BreedsView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CatFactApiService>();
        var page = UseState(1);
        var search = UseState("");

        var query = UseQuery(
            key: ("breeds", page.Value),
            fetcher: async (ct) => await service.GetBreedsAsync(page.Value, 15, ct),
            options: new QueryOptions { KeepPrevious = true }
        );

        if (query.Loading && query.Value == null)
            return Layout.TopCenter()
                | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                    | Skeleton.Card());

        if (query.Error is { } error)
            return Layout.TopCenter()
                | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                    | Callout.Error($"Failed to load breeds: {error.Message}"));

        var data = query.Value!;
        var breeds = data.Data.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search.Value))
            breeds = breeds.Where(b => b.Breed.Contains(search.Value, StringComparison.OrdinalIgnoreCase));

        var filteredBreeds = breeds.ToArray();

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H2("🐾 Cat Breeds")
                | Text.Muted($"{data.Total} breeds from around the world")
                | new Separator()
                | search.ToTextInput().Placeholder("Search breeds...").Prefix(Icons.Search)
                | filteredBreeds.ToTable()
                    .Width(Size.Full())
                    .Header(b => b.Breed, "Breed")
                    .Header(b => b.Country, "Country")
                    .Header(b => b.Origin, "Origin")
                    .Header(b => b.Coat, "Coat")
                    .Header(b => b.Pattern, "Pattern")
                    .Builder(b => b.Breed, f => BuilderFactoryExtensions.Func<BreedInfo, string>(f, val => Text.Block(val).Bold()))
                    .Builder(b => b.Origin, f => BuilderFactoryExtensions.Func<BreedInfo, string>(f, val => new Badge(val)))
                | (Layout.Horizontal().Gap(2).Center()
                    | new Button("Previous", () => page.Set(page.Value - 1))
                        .Icon(Icons.ChevronLeft).Outline()
                        .Disabled(data.CurrentPage <= 1)
                    | Text.Block($"Page {data.CurrentPage} of {data.LastPage}")
                    | new Button("Next", () => page.Set(page.Value + 1))
                        .Icon(Icons.ChevronRight).Outline()
                        .Disabled(data.CurrentPage >= data.LastPage)));
    }
}
