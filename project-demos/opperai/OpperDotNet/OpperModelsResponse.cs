using System.Text.Json.Serialization;

namespace OpperDotNet;

/// <summary>
/// Response from the List Models API
/// </summary>
public class OpperModelsResponse
{
    /// <summary>
    /// Metadata about the response
    /// </summary>
    [JsonPropertyName("meta")]
    public OpperModelsMeta Meta { get; set; } = new();

    /// <summary>
    /// List of models
    /// </summary>
    [JsonPropertyName("data")]
    public List<OpperModel> Data { get; set; } = new();
}

/// <summary>
/// Metadata about the models response
/// </summary>
public class OpperModelsMeta
{
    /// <summary>
    /// Total number of models available
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

