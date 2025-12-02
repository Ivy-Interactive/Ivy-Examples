using Microsoft.Agents.AI;
using OllamaSharp;
using MicrosoftAgentFramework.Models;

namespace MicrosoftAgentFramework.Services;

/// <summary>
/// Service for managing AI agents and chat interactions using Microsoft Agent Framework with Ollama
/// </summary>
public class AgentManager : IDisposable
{
    private readonly string _ollamaUrl;
    private readonly string _ollamaModel;
    private readonly string? _bingApiKey;
    private AIAgent? _agent;
    private AgentConfiguration? _currentConfig;
    private bool _disposed;

    public AgentManager(string ollamaUrl, string ollamaModel, string? bingApiKey = null)
    {
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
        _bingApiKey = bingApiKey;
    }

    /// <summary>
    /// Configures the manager with the specified agent using Microsoft Agent Framework with Ollama
    /// </summary>
    public void ConfigureAgent(AgentConfiguration config)
    {
        _currentConfig = config;
        
        // Create OllamaApiClient which implements IChatClient
        var ollamaClient = new OllamaApiClient(new Uri(_ollamaUrl), _ollamaModel);

        // Create agent using ChatClientAgent from Microsoft Agent Framework
        // This works with any IChatClient implementation, including Ollama
        _agent = new ChatClientAgent(
            ollamaClient,
            instructions: _currentConfig.Instructions,
            name: _currentConfig.Name
        );

        // Agent is ready to use
    }

    /// <summary>
    /// Sends a message to the agent and returns the response
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage)
    {
        if (_agent == null || _currentConfig == null)
        {
            throw new InvalidOperationException("Agent not configured. Call ConfigureAgent first.");
        }

        try
        {
            // Run agent using Microsoft Agent Framework
            // Tools are not used for now
            var response = await _agent.RunAsync(userMessage);
            
            return response.Text ?? response.ToString() ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error: {ex.Message}";
            return errorMessage;
        }
    }


    /// <summary>
    /// Clears the chat history (recreates agent with same configuration)
    /// </summary>
    public void ClearHistory()
    {
        if (_currentConfig != null)
        {
            // Recreate agent to clear history
            ConfigureAgent(_currentConfig);
        }
    }

    /// <summary>
    /// Gets the current chat history count
    /// Note: With Microsoft Agent Framework, history is managed internally
    /// </summary>
    public int GetHistoryCount()
    {
        // History is managed by the agent framework internally
        // Return 0 as we don't have direct access to history count
        return 0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

