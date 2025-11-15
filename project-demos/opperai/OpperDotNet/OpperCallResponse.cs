using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpperDotNet;

/// <summary>
/// Response model from Opper.ai Call API
/// </summary>
public class OpperCallResponse
{
    /// <summary>
    /// The text message response from the AI
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Structured JSON output (when output_schema is provided)
    /// </summary>
    [JsonPropertyName("json_payload")]
    public JsonElement? JsonPayload { get; set; }

    /// <summary>
    /// Unique identifier for this call
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// Model used for the call
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Cache status (hit/miss)
    /// </summary>
    [JsonPropertyName("cache_status")]
    public string? CacheStatus { get; set; }

    /// <summary>
    /// Duration of the call in milliseconds
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; set; }

    /// <summary>
    /// Input tokens used
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; set; }

    /// <summary>
    /// Output tokens used
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; set; }
}

