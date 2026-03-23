namespace SWAPI.Graph.QL.Apps.Models;

using System.Text.Json.Serialization;

public record SwapiPagedResponse<T>(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("next")] string? Next,
    [property: JsonPropertyName("previous")] string? Previous,
    [property: JsonPropertyName("results")] List<T> Results
);

public record Person(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("height")] string Height,
    [property: JsonPropertyName("mass")] string Mass,
    [property: JsonPropertyName("hair_color")] string HairColor,
    [property: JsonPropertyName("skin_color")] string SkinColor,
    [property: JsonPropertyName("eye_color")] string EyeColor,
    [property: JsonPropertyName("birth_year")] string BirthYear,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("homeworld")] string Homeworld,
    [property: JsonPropertyName("films")] List<string> Films,
    [property: JsonPropertyName("species")] List<string> Species,
    [property: JsonPropertyName("vehicles")] List<string> Vehicles,
    [property: JsonPropertyName("starships")] List<string> Starships,
    [property: JsonPropertyName("url")] string Url
);

public record Planet(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("rotation_period")] string RotationPeriod,
    [property: JsonPropertyName("orbital_period")] string OrbitalPeriod,
    [property: JsonPropertyName("diameter")] string Diameter,
    [property: JsonPropertyName("climate")] string Climate,
    [property: JsonPropertyName("gravity")] string Gravity,
    [property: JsonPropertyName("terrain")] string Terrain,
    [property: JsonPropertyName("surface_water")] string SurfaceWater,
    [property: JsonPropertyName("population")] string Population,
    [property: JsonPropertyName("residents")] List<string> Residents,
    [property: JsonPropertyName("films")] List<string> Films,
    [property: JsonPropertyName("url")] string Url
);

public record Starship(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("manufacturer")] string Manufacturer,
    [property: JsonPropertyName("cost_in_credits")] string CostInCredits,
    [property: JsonPropertyName("length")] string Length,
    [property: JsonPropertyName("max_atmosphering_speed")] string MaxAtmospheringSpeed,
    [property: JsonPropertyName("crew")] string Crew,
    [property: JsonPropertyName("passengers")] string Passengers,
    [property: JsonPropertyName("cargo_capacity")] string CargoCapacity,
    [property: JsonPropertyName("consumables")] string Consumables,
    [property: JsonPropertyName("hyperdrive_rating")] string HyperdriveRating,
    [property: JsonPropertyName("MGLT")] string MGLT,
    [property: JsonPropertyName("starship_class")] string StarshipClass,
    [property: JsonPropertyName("pilots")] List<string> Pilots,
    [property: JsonPropertyName("films")] List<string> Films,
    [property: JsonPropertyName("url")] string Url
);

public record Film(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("episode_id")] int EpisodeId,
    [property: JsonPropertyName("opening_crawl")] string OpeningCrawl,
    [property: JsonPropertyName("director")] string Director,
    [property: JsonPropertyName("producer")] string Producer,
    [property: JsonPropertyName("release_date")] string ReleaseDate,
    [property: JsonPropertyName("characters")] List<string> Characters,
    [property: JsonPropertyName("planets")] List<string> Planets,
    [property: JsonPropertyName("starships")] List<string> Starships,
    [property: JsonPropertyName("url")] string Url
);
