using MicrosoftAgentFramework.Models;
using MicrosoftAgentFramework.Views;
using OllamaSharp;

namespace MicrosoftAgentFramework.Apps;

[App(icon: Icons.Bot, title: "Microsoft Agent Framework")]
public class AgentWorkspaceExample : ViewBase
{
    public override object? Build()
    {
        // State for agents list (including presets)
        var agents = UseState(GetPresetAgents());
        
        // Ollama configuration state
        var ollamaUrl = UseState<string?>(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434");
        var ollamaModel = UseState<string?>(Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama2");

        // Content with blades
        var content = this.UseBlades(
            () => new AgentListView(agents, ollamaUrl, ollamaModel),
            "Agents"
        );

        return new Fragment()
            | content;
    }

    /// <summary>
    /// Returns the preset agent configurations
    /// </summary>
    private static List<AgentConfiguration> GetPresetAgents()
    {
        return new List<AgentConfiguration>
        {
            new AgentConfiguration
            {
                Id = "preset-writer",
                Name = "Story Writer with Research",
                Description = "Creative writer with time awareness and calculation tools",
                OllamaModel = "llama2",
                Instructions = @"You are a Creative Writer AI assistant with access to tools. Your expertise includes:
- Crafting engaging stories, poems, and creative content
- Researching historical facts and current events to enhance your writing
- Using accurate dates and time references in narratives
- Developing compelling narratives with factual accuracy

When writing:
- Use GetCurrentTime to reference accurate dates and times in your narratives
- Use Calculate for word counts, deadlines, or any numerical aspects of writing projects

Use vivid language, metaphors, and creative expression. Format your responses with proper Markdown when appropriate.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-analyst",
                Name = "Mathematical Calculator & Analyst",
                Description = "Expert calculator and data analyst",
                OllamaModel = "llama2",
                Instructions = @"You are a Data Analyst AI assistant with powerful calculation capabilities. Your expertise includes:
- Performing complex mathematical calculations and statistical analysis
- Analyzing data patterns and trends
- Explaining calculations step-by-step
- Creating data visualizations descriptions

IMPORTANT: Always use the Calculate tool for ANY mathematical operation, computation, or calculation. Never attempt calculations manually.

When analyzing:
- Use Calculate for all mathematical operations, formulas, and statistical computations
- Use GetCurrentTime when working with time-series data or date-based analysis

Be precise, analytical, and data-driven. Always show your work and explain your calculations. Present findings clearly with proper formatting.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-coder",
                Name = "Developer Assistant with Tools",
                Description = "Code expert with calculation and time tools",
                OllamaModel = "llama2",
                Instructions = @"You are a Code Assistant AI with access to calculation tools. Your expertise includes:
- Writing clean, efficient code in multiple languages
- Debugging and troubleshooting code issues
- Finding up-to-date documentation and API references
- Performing calculations for algorithm analysis
- Explaining programming concepts clearly

When coding:
- Use Calculate for algorithm complexity analysis, performance metrics, or any mathematical computations
- Use GetCurrentTime when working with date/time operations, timestamps, or scheduling code

Always format code using proper Markdown code blocks with language syntax highlighting. Be precise and provide working, tested solutions when possible. Explain your reasoning.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-researcher",
                Name = "Web Research Assistant",
                Description = "Expert researcher with calculation and time tools",
                OllamaModel = "llama2",
                Instructions = @"You are a Research Assistant AI with calculation and time tools. Your expertise includes:
- Researching topics and providing well-researched responses
- Fact-checking and verification
- Performing statistical analysis on research findings
- Finding relevant sources and references
- Providing clear, organized information

When researching:
- Use Calculate for any statistical analysis, data calculations, or numerical research findings
- Use GetCurrentTime to provide context about when information was current or to reference dates

Present information in a clear, organized manner with proper formatting. Always cite sources when available.",
                IsPreset = true
            }
        };
    }
}

