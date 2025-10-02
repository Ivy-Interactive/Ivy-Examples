public sealed class Store
{
    [JsonProperty("store")]
    public StoreInner? Inner { get; set; }

    // Convenience projection for the UI
    [JsonIgnore]
    public List<Book>? Book => Inner?.Book;

    [JsonIgnore]
    public Bicycle? Bicycle => Inner?.Bicycle;
}

public sealed class StoreInner
{
    [JsonProperty("book")] public List<Book>? Book { get; set; }
    [JsonProperty("bicycle")] public Bicycle? Bicycle { get; set; }
}

public sealed class Book
{
    [JsonProperty("title")] public string? Title { get; set; }
    [JsonProperty("author")] public string? Author { get; set; }
    [JsonProperty("price")] public decimal Price { get; set; }

    [JsonProperty("genre")] public Genre Genre { get; set; }

    // Stored as Unix epoch seconds in input JSON. Demonstrates custom converter.
    [JsonProperty("published")]
    [JsonConverter(typeof(UnixEpochDateConverter))]
    public DateTime Published { get; set; }
}

public sealed class Bicycle
{
    [JsonProperty("color")] public string? Color { get; set; }
    [JsonProperty("price")] public decimal Price { get; set; }
}

public enum Genre
{
    Technical,
    Fiction,
    Fantasy,
    Biography
}
