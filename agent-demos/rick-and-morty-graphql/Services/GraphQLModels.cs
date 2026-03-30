using System.Text.Json.Serialization;

namespace RickAndMortyGraphQL.Services;

public record GraphQLResponse<T>(T Data);


public record CharactersData(CharactersResult Characters);
public record CharactersResult(CharacterInfo Info, List<Character> Results);
public record CharacterInfo(int Count, int Pages, int? Next, int? Prev);
public record Character(
    string Id, string Name, string Status, string Species, string Type, string Gender,
    CharacterLocation Origin, CharacterLocation Location, string Image, List<Episode> Episode);
public record CharacterLocation(string? Id, string? Name, string? Type, string? Dimension);


public record EpisodesData(EpisodesResult Episodes);
public record EpisodesResult(EpisodeInfo Info, List<Episode> Results);
public record EpisodeInfo(int Count, int Pages);
public record Episode(
    string Id, string Name,
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("episode")] string? Episode1,
    List<EpisodeCharacter>? Characters);
public record EpisodeCharacter(string Id, string Name, string Image, string Status);


public record LocationsData(LocationsResult Locations);
public record LocationsResult(LocationInfo Info, List<Location> Results);
public record LocationInfo(int Count, int Pages);
public record Location(string Id, string Name, string Type, string Dimension, List<LocationResident>? Residents);
public record LocationResident(string Id, string Name, string Image, string Status, string Species);
