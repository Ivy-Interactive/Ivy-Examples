using Ivy;

namespace Trevor.Blades.Countries;

public class CompareView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CountriesService>();
        var query = UseQuery("all-countries", async ct => await service.GetAllDataAsync());

        var country1 = UseState("");
        var country2 = UseState("");

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var countries = query.Value!.Countries;

        var options = countries
            .OrderBy(c => c.Name)
            .Select(c => (IAnyOption)new Option<string>($"{c.Emoji} {c.Name}", c.Code))
            .ToArray();

        var select1 = country1.ToSelectInput(options).Placeholder("Select first country...");
        var select2 = country2.ToSelectInput(options).Placeholder("Select second country...");

        var selectors = Layout.Horizontal()
            | select1
            | select2;

        var bothSelected = !string.IsNullOrEmpty(country1.Value) && !string.IsNullOrEmpty(country2.Value);

        object content;
        if (!bothSelected)
        {
            content = Callout.Info("Please select two countries above to compare them side by side.");
        }
        else
        {
            var c1 = countries.First(c => c.Code == country1.Value);
            var c2 = countries.First(c => c.Code == country2.Value);

            content = Layout.Grid().Columns(2)
                | BuildCountryCard(c1)
                | BuildCountryCard(c2);
        }

        return Layout.Vertical()
            | Text.H1("Compare Countries")
            | selectors
            | content;
    }

    private static object BuildCountryCard(CountryDto country)
    {
        var details = new
        {
            Name = country.Name,
            NativeName = country.Native,
            Capital = country.Capital ?? "N/A",
            Continent = country.Continent.Name,
            PhoneCode = $"+{country.Phone}",
            Currency = country.Currency ?? "N/A",
            AllCurrencies = country.Currencies.Length > 0 ? string.Join(", ", country.Currencies) : "N/A",
            Languages = country.Languages.Length > 0 ? string.Join(", ", country.Languages.Select(l => l.Name)) : "N/A"
        }.ToDetails()
            .Label(x => x.NativeName, "Native Name")
            .Label(x => x.PhoneCode, "Phone Code")
            .Label(x => x.AllCurrencies, "All Currencies");

        return new Card(details).Title($"{country.Emoji} {country.Name}");
    }
}
