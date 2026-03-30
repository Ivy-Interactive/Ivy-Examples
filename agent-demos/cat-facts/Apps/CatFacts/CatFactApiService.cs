using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CatFacts.Apps.CatFacts;

public record CatFact(string Fact, int Length);

public record BreedInfo(
    string Breed,
    string Country,
    string Origin,
    string Coat,
    string Pattern
);

public record BreedsResponse(
    [property: JsonPropertyName("current_page")] int CurrentPage,
    [property: JsonPropertyName("data")] BreedInfo[] Data,
    [property: JsonPropertyName("last_page")] int LastPage,
    [property: JsonPropertyName("total")] int Total
);

public class CatFactApiService(HttpClient httpClient)
{
    private readonly List<string> _seenFacts = [];
    private readonly List<string> _favoriteFacts = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<string> SeenFacts
    {
        get { lock (_lock) return _seenFacts.ToList(); }
    }

    public IReadOnlyList<string> FavoriteFacts
    {
        get { lock (_lock) return _favoriteFacts.ToList(); }
    }

    public async Task<string> GetRandomFactAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<CatFact>("https://catfact.ninja/fact", ct);
        var fact = result?.Fact ?? "Cats are amazing!";
        lock (_lock)
        {
            if (!_seenFacts.Contains(fact))
                _seenFacts.Add(fact);
        }
        return fact;
    }

    public async Task<BreedsResponse> GetBreedsAsync(int page = 1, int limit = 10, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<BreedsResponse>(
            $"https://catfact.ninja/breeds?page={page}&limit={limit}", ct);
        return result ?? new BreedsResponse(1, [], 1, 0);
    }

    public bool IsFavorite(string fact)
    {
        lock (_lock) return _favoriteFacts.Contains(fact);
    }

    public bool ToggleFavorite(string fact)
    {
        lock (_lock)
        {
            if (_favoriteFacts.Contains(fact))
            {
                _favoriteFacts.Remove(fact);
                return false;
            }
            _favoriteFacts.Add(fact);
            return true;
        }
    }

    public void RemoveFavorite(string fact)
    {
        lock (_lock) _favoriteFacts.Remove(fact);
    }
}
