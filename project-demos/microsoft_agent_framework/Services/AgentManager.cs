using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using MicrosoftAgentFramework.Models;
using System.ComponentModel;

namespace MicrosoftAgentFramework.Services;

/// <summary>
/// Service for managing AI agents and chat interactions using Microsoft Agent Framework with Ollama
/// </summary>
public class AgentManager : IDisposable
{
    private readonly string _ollamaUrl;
    private readonly string _ollamaModel;
    private AIAgent? _agent;
    private AgentConfiguration? _currentConfig;
    private bool _disposed;

    public AgentManager(string ollamaUrl, string ollamaModel)
    {
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
    }

    private static List<AITool> CreateTools() => new()
    {
        AIFunctionFactory.Create(AgentTools.GetCurrentTime),
        AIFunctionFactory.Create(AgentTools.Calculate)
    };

    private OllamaApiClient CreateClient(string model) => new(new Uri(_ollamaUrl), model);

    /// <summary>
    /// Configures the manager with the specified agent using Microsoft Agent Framework with Ollama
    /// </summary>
    public async Task ConfigureAgentAsync(AgentConfiguration config)
    {
        _currentConfig = config;
        var modelToUse = !string.IsNullOrWhiteSpace(config.OllamaModel) ? config.OllamaModel : _ollamaModel;
        var client = CreateClient(modelToUse);

        // Try with tools first, fallback to without tools if not supported during execution
        _agent = new ChatClientAgent(client, instructions: config.Instructions, name: config.Name, tools: CreateTools());
    }

    private void EnsureAgentConfigured()
    {
        if (_agent == null || _currentConfig == null)
            throw new InvalidOperationException("Agent not configured. Call ConfigureAgentAsync first.");
    }

    /// <summary>
    /// Sends a message to the agent and returns the response
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage)
    {
        EnsureAgentConfigured();
        try
        {
            var response = await _agent!.RunAsync(userMessage);
            return response.Text ?? response.ToString() ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Sends a message to the agent and returns streaming updates
    /// </summary>
    public IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(string userMessage)
    {
        EnsureAgentConfigured();
        return _agent!.RunStreamingAsync(userMessage);
    }

    /// <summary>
    /// Recreates the agent without tools (used when model doesn't support tools during execution)
    /// </summary>
    public async Task RecreateAgentWithoutToolsAsync()
    {
        if (_currentConfig == null) return;
        var modelToUse = !string.IsNullOrWhiteSpace(_currentConfig.OllamaModel) ? _currentConfig.OllamaModel : _ollamaModel;
        var client = CreateClient(modelToUse);
        _agent = new ChatClientAgent(client, instructions: _currentConfig.Instructions, name: _currentConfig.Name);
    }


    /// <summary>
    /// Clears the chat history (recreates agent with same configuration)
    /// </summary>
    public async Task ClearHistoryAsync()
    {
        if (_currentConfig != null)
        {
            // Recreate agent to clear history
            await ConfigureAgentAsync(_currentConfig);
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

