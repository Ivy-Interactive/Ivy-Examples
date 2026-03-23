using Ivy;

namespace Trevor.Blades.Countries;

public class ItineraryView : ViewBase
{
    public override object? Build()
    {
        var service = UseService<CountriesService>();
        var query = UseQuery("itinerary-countries", async (ct) => await service.GetAllDataAsync());

        var itinerary = UseState<string[]>(Array.Empty<string>());
        var selectedCountry = UseState("");

        if (query.Loading) return Skeleton.Card();
        if (query.Error is { } error) return Callout.Error(error.Message);

        var data = query.Value!;

        var availableCountries = data.Countries
            .Where(c => !itinerary.Value.Contains(c.Code))
            .OrderBy(c => c.Name)
            .Select(c => new Option<string>($"{c.Emoji} {c.Name}", c.Code))
            .ToArray();

        var addSection = Layout.Horizontal().Align(Align.Left)
            | selectedCountry.ToSelectInput(availableCountries, placeholder: "Select a country to add...")
                .Searchable()
            | new Button("Add to Itinerary", () =>
            {
                if (!string.IsNullOrEmpty(selectedCountry.Value))
                {
                    itinerary.Set(itinerary.Value.Append(selectedCountry.Value).ToArray());
                    selectedCountry.Set("");
                }
            }).Icon(Icons.Plus).Primary();

        if (itinerary.Value.Length == 0)
        {
            return Layout.Vertical()
                | addSection
                | Callout.Info("Your itinerary is empty. Add countries to start planning your trip!");
        }

        var countryCards = Layout.Vertical();
        for (var i = 0; i < itinerary.Value.Length; i++)
        {
            var countryCode = itinerary.Value[i];
            var country = data.Countries.FirstOrDefault(c => c.Code == countryCode);
            if (country is null) continue;

            var stopIndex = i;
            var cardContent = Layout.Vertical().Gap(2)
                | Text.Block($"🏛️ Capital: {country.Capital ?? "N/A"}")
                | Text.Block($"🌍 Continent: {country.Continent.Name}")
                | Text.Block($"🗣️ Languages: {string.Join(", ", country.Languages.Select(l => l.Name))}")
                | Text.Block($"📞 Phone: +{country.Phone}")
                | Text.Block($"💰 Currency: {country.Currency ?? "N/A"}")
                | (Layout.Horizontal().Gap(2)
                    | new Button("Move Up", () =>
                    {
                        var list = itinerary.Value.ToList();
                        var idx = list.IndexOf(countryCode);
                        if (idx > 0) { (list[idx], list[idx - 1]) = (list[idx - 1], list[idx]); }
                        itinerary.Set(list.ToArray());
                    }).Icon(Icons.ArrowUp).Outline().Small()
                    | new Button("Move Down", () =>
                    {
                        var list = itinerary.Value.ToList();
                        var idx = list.IndexOf(countryCode);
                        if (idx < list.Count - 1) { (list[idx], list[idx + 1]) = (list[idx + 1], list[idx]); }
                        itinerary.Set(list.ToArray());
                    }).Icon(Icons.ArrowDown).Outline().Small()
                    | new Button("Remove", () =>
                    {
                        itinerary.Set(itinerary.Value.Where(code => code != countryCode).ToArray());
                    }).Icon(Icons.Trash).Destructive().Small());

            var card = new Card(cardContent)
                .Title($"{country.Emoji} {country.Name}")
                .Description($"Stop {stopIndex + 1}");

            countryCards |= card;
        }

        var itineraryCountries = itinerary.Value
            .Select(code => data.Countries.FirstOrDefault(c => c.Code == code))
            .Where(c => c is not null)
            .ToArray();

        var continentsVisited = itineraryCountries
            .Select(c => c!.Continent.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        var languagesEncountered = itineraryCountries
            .SelectMany(c => c!.Languages.Select(l => l.Name))
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        var currenciesNeeded = itineraryCountries
            .SelectMany(c => c!.Currencies)
            .Where(cur => !string.IsNullOrEmpty(cur))
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        var summaryContent = Layout.Vertical().Gap(2)
            | Text.Block($"📍 Total Stops: {itinerary.Value.Length}")
            | Text.Block($"🌍 Continents: {string.Join(", ", continentsVisited)}")
            | Text.Block($"🗣️ Languages: {string.Join(", ", languagesEncountered)}")
            | Text.Block($"💰 Currencies: {string.Join(", ", currenciesNeeded)}");

        var summaryCard = new Card(summaryContent)
            .Title("📋 Trip Summary");

        return Layout.Vertical()
            | addSection
            | countryCards
            | summaryCard;
    }
}
