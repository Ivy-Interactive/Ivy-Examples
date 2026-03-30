using System.Net.Http.Json;

namespace RickAndMortyGraphQL.Services;

public class RickAndMortyClient(HttpClient httpClient)
{
    private const string Endpoint = "https://rickandmortyapi.com/graphql";

    private async Task<T> QueryAsync<T>(string query, object? variables = null, CancellationToken ct = default)
    {
        var payload = new { query, variables };
        var response = await httpClient.PostAsJsonAsync(Endpoint, payload, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<T>>(ct);
        return result!.Data;
    }

    public async Task<CharactersResult> GetCharactersAsync(int page = 1, string? name = null, string? status = null, string? species = null, string? gender = null, CancellationToken ct = default)
    {
        var query = """
            query ($page: Int, $filter: FilterCharacter) {
              characters(page: $page, filter: $filter) {
                info { count pages next prev }
                results {
                  id name status species type gender image
                  origin { id name type dimension }
                  location { id name type dimension }
                  episode { id name air_date episode }
                }
              }
            }
            """;
        var filter = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(name)) filter["name"] = name;
        if (!string.IsNullOrEmpty(status)) filter["status"] = status;
        if (!string.IsNullOrEmpty(species)) filter["species"] = species;
        if (!string.IsNullOrEmpty(gender)) filter["gender"] = gender;

        var data = await QueryAsync<CharactersData>(query, new { page, filter = filter.Count > 0 ? filter : null }, ct);
        return data.Characters;
    }

    public async Task<EpisodesResult> GetEpisodesAsync(int page = 1, string? name = null, CancellationToken ct = default)
    {
        var query = """
            query ($page: Int, $filter: FilterEpisode) {
              episodes(page: $page, filter: $filter) {
                info { count pages }
                results {
                  id name air_date episode
                  characters { id name image status }
                }
              }
            }
            """;
        var filter = !string.IsNullOrEmpty(name) ? new { name } : null;
        var data = await QueryAsync<EpisodesData>(query, new { page, filter }, ct);
        return data.Episodes;
    }

    public async Task<LocationsResult> GetLocationsAsync(int page = 1, string? name = null, string? type = null, string? dimension = null, CancellationToken ct = default)
    {
        var query = """
            query ($page: Int, $filter: FilterLocation) {
              locations(page: $page, filter: $filter) {
                info { count pages }
                results {
                  id name type dimension
                  residents { id name image status species }
                }
              }
            }
            """;
        var filter = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(name)) filter["name"] = name;
        if (!string.IsNullOrEmpty(type)) filter["type"] = type;
        if (!string.IsNullOrEmpty(dimension)) filter["dimension"] = dimension;

        var data = await QueryAsync<LocationsData>(query, new { page, filter = filter.Count > 0 ? filter : null }, ct);
        return data.Locations;
    }
}
