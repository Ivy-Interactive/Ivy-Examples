using Ivy;

namespace Trevor.Blades.Countries;

public class ExploreView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CountriesService>();

        var query = UseQuery(
            key: "countries-data",
            fetcher: async (ct) => await service.GetAllDataAsync()
        );

        var selectedContinent = UseState("");
        var selectedLanguage = UseState("");
        var searchText = UseState("");

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var data = query.Value!;


        var continentOptions = new IAnyOption[] { new Option<string>("All Continents", "") }
            .Concat(data.Continents.OrderBy(c => c.Name).Select(c => new Option<string>(c.Name, c.Code)))
            .ToArray();


        var languageOptions = new IAnyOption[] { new Option<string>("All Languages", "") }
            .Concat(data.Languages.OrderBy(l => l.Name).Select(l => new Option<string>(l.Name, l.Code)))
            .ToArray();


        var filtered = data.Countries.AsEnumerable();

        if (!string.IsNullOrEmpty(selectedContinent.Value))
            filtered = filtered.Where(c => c.Continent.Code == selectedContinent.Value);

        if (!string.IsNullOrEmpty(selectedLanguage.Value))
            filtered = filtered.Where(c => c.Languages.Any(l => l.Code == selectedLanguage.Value));

        if (!string.IsNullOrEmpty(searchText.Value))
            filtered = filtered.Where(c =>
                c.Name.Contains(searchText.Value, StringComparison.OrdinalIgnoreCase));

        var countries = filtered.ToArray();


        var continentSelect = selectedContinent.ToSelectInput(continentOptions, placeholder: "All Continents");
        var languageSelect = selectedLanguage.ToSelectInput(languageOptions, placeholder: "All Languages");
        var searchInput = searchText.ToTextInput().Placeholder("Search countries...");


        var tableData = countries.Select(c => new
        {
            Flag = c.Emoji,
            c.Name,
            Capital = c.Capital ?? "—",
            Continent = c.Continent.Name,
            Languages = string.Join(", ", c.Languages.Select(l => l.Name)),
            Phone = $"+{c.Phone}",
            Currency = c.Currency ?? "—"
        }).AsQueryable();

        var table = tableData.ToDataTable()
            .Header(x => x.Flag, "Flag")
            .Header(x => x.Name, "Name")
            .Header(x => x.Capital, "Capital")
            .Header(x => x.Continent, "Continent")
            .Header(x => x.Languages, "Languages")
            .Header(x => x.Phone, "Phone")
            .Header(x => x.Currency, "Currency");

        return Layout.Vertical()
            | Text.H2("Explore Countries")
            | (Layout.Horizontal().Gap(3)
                | searchInput
                | continentSelect
                | languageSelect)
            | Text.Muted($"{countries.Length} countries found")
            | table;
    }
}
