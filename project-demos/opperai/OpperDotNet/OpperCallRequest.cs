using System.Text.Json.Serialization;

namespace OpperDotNet;

/// <summary>
/// Request model for Opper.ai Call API
/// </summary>
public class OpperCallRequest
{
    /// <summary>
    /// Name of the task/call (for tracking and organization)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Instructions for the AI model on how to process the input
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// The input text/data to process
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Model to use (if not specified, Opper uses default)
    /// </summary>
    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    /// <summary>
    /// Optional: JSON schema for structured output
    /// </summary>
    [JsonPropertyName("output_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? OutputSchema { get; set; }

    /// <summary>
    /// Creates a new OpperCallRequest
    /// </summary>
    public OpperCallRequest()
    {
    }

    /// <summary>
    /// Creates a new OpperCallRequest with basic parameters
    /// </summary>
    public OpperCallRequest(string name, string instructions, string input, string? model = null)
    {
        Name = name;
        Instructions = instructions;
        Input = input;
        Model = model;
    }
}

