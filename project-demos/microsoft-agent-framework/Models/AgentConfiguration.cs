namespace MicrosoftAgentFramework.Models;

/// <summary>
/// Represents the configuration for an AI agent persona
/// </summary>
public class AgentConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Agent";
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = "You are a helpful AI assistant.";
    public string OllamaModel { get; set; } = "llama2";
    public bool IsPreset { get; set; } = false;
    
    public AgentConfiguration Clone()
    {
        return new AgentConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name + " (Copy)",
            Description = Description,
            Instructions = Instructions,
            OllamaModel = OllamaModel,
            IsPreset = false
        };
    }
}

/// <summary>
/// Form model for editing agent configuration in UI
/// </summary>
public record AgentFormModel
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = "New Agent";
    
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string OllamaModel { get; set; } = "llama2";
    
    [Required]
    public string Instructions { get; set; } = "You are a helpful AI assistant.";
    
    public static AgentFormModel FromConfiguration(AgentConfiguration config)
    {
        return new AgentFormModel
        {
            Name = config.Name,
            Description = config.Description,
            OllamaModel = config.OllamaModel,
            Instructions = config.Instructions
        };
    }
    
    public AgentConfiguration ToConfiguration(string? existingId = null)
    {
        return new AgentConfiguration
        {
            Id = existingId ?? Guid.NewGuid().ToString(),
            Name = Name,
            Description = Description,
            OllamaModel = OllamaModel,
            Instructions = Instructions,
            IsPreset = false
        };
    }
    
    public void ApplyTo(AgentConfiguration config)
    {
        config.Name = Name;
        config.Description = Description;
        config.OllamaModel = OllamaModel;
        config.Instructions = Instructions;
    }
}

/// <summary>
/// Settings form model for API keys and Ollama configuration
/// </summary>
public record ApiSettingsModel
{
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    
    public string OllamaModel { get; set; } = "llama2";
    
    public string BingApiKey { get; set; } = string.Empty;
}

