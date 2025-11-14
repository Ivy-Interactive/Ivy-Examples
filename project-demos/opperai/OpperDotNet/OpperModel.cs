using System.Text.Json.Serialization;

namespace OpperDotNet;

/// <summary>
/// Represents a language model available in the Opper platform
/// </summary>
public class OpperModel
{
    /// <summary>
    /// The hosting provider of the model (e.g., "openai", "azure", "anthropic")
    /// </summary>
    [JsonPropertyName("hosting_provider")]
    public string HostingProvider { get; set; } = string.Empty;

    /// <summary>
    /// The name of the model (e.g., "gpt-4o", "azure/gpt-4o-eu")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The location of the model (e.g., "us", "eu")
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The cost in USD per token for input
    /// </summary>
    [JsonPropertyName("input_cost_per_token")]
    public decimal? InputCostPerToken { get; set; }

    /// <summary>
    /// The cost in USD per token for output
    /// </summary>
    [JsonPropertyName("output_cost_per_token")]
    public decimal? OutputCostPerToken { get; set; }

    /// <summary>
    /// Display name for the model (provider/name)
    /// </summary>
    public string DisplayName => $"{HostingProvider}/{Name}";
}

