using System.Net.Http.Json;
using System.Text.Json;

namespace Trevor.Blades.Countries;

public record CountryDto(string Code, string Name, string Native, string Emoji, string Phone, string? Capital, string? Currency, string[] Currencies, ContinentRef Continent, LanguageRef[] Languages);
public record ContinentRef(string Code, string Name);
public record LanguageRef(string Code, string Name);
public record ContinentDto(string Code, string Name);
public record LanguageDto(string Code, string Name, string Native, bool Rtl);
public record CountriesData(CountryDto[] Countries, ContinentDto[] Continents, LanguageDto[] Languages);

public class CountriesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string Endpoint = "https://countries.trevorblades.com/graphql";

    private const string Query = """
        {
          continents { code name }
          languages { code name native rtl }
          countries { code name native emoji phone capital currency currencies continent { code name } languages { code name } }
        }
        """;

    private readonly HttpClient _http;
    private CountriesData? _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private record GraphQlResponse<T>(T Data);

    public CountriesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<CountriesData> GetAllDataAsync()
    {
        if (_cache is not null) return _cache;

        await _semaphore.WaitAsync();
        try
        {
            if (_cache is not null) return _cache;

            var request = new { query = Query };
            var response = await _http.PostAsJsonAsync(Endpoint, request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GraphQlResponse<CountriesData>>(JsonOptions);
            _cache = result?.Data ?? throw new InvalidOperationException("Failed to deserialize GraphQL response.");
            return _cache;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
