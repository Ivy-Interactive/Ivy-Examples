using MicrosoftAgentFramework.Models;
using MicrosoftAgentFramework.Views;

namespace MicrosoftAgentFramework.Apps;

[App(icon: Icons.Bot, title: "AI Agent Workspace")]
public class AgentWorkspaceExample : ViewBase
{
    public override object? Build()
    {
        // State for agents list (including presets)
        var agents = UseState(GetPresetAgents());
        
        // Ollama configuration state
        var ollamaUrl = UseState<string?>(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434");
        var ollamaModel = UseState<string?>(Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama2");
        var bingApiKey = UseState<string?>(Environment.GetEnvironmentVariable("BING_API_KEY"));

        return this.UseBlades(
            () => new AgentListView(agents, ollamaUrl, ollamaModel, bingApiKey),
            "Agents",
            Size.Units(80)
        );
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
                Name = "Creative Writer",
                Description = "Crafts stories, poetry, and creative content",
                Instructions = @"You are a Creative Writer AI assistant. Your expertise includes:
- Crafting engaging stories, poems, and creative content
- Developing compelling narratives and characters
- Writing in various styles and genres
- Providing writing tips and feedback
- Helping with brainstorming and ideation

Use vivid language, metaphors, and creative expression. When asked to write, produce original, engaging content. Format your responses with proper Markdown when appropriate.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-analyst",
                Name = "Data Analyst",
                Description = "Expert in calculations and analytical thinking",
                Instructions = @"You are a Data Analyst AI assistant. Your expertise includes:
- Analyzing data and providing insights
- Performing calculations and statistical analysis
- Explaining data patterns and trends
- Creating data visualizations descriptions
- Helping with spreadsheet formulas and logic

Be precise, analytical, and data-driven in your responses. Present findings clearly with proper formatting.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-coder",
                Name = "Code Assistant",
                Description = "Programming expert for coding and debugging",
                Instructions = @"You are a Code Assistant AI. Your expertise includes:
- Writing clean, efficient code in multiple languages
- Debugging and troubleshooting code issues
- Explaining programming concepts clearly
- Suggesting best practices and design patterns
- Code review and optimization suggestions

Always format code using proper Markdown code blocks with language syntax highlighting. Be precise and provide working, tested solutions when possible. Explain your reasoning.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-researcher",
                Name = "Research Assistant",
                Description = "Researches topics and provides verified information",
                Instructions = @"You are a Research Assistant AI. Your expertise includes:
- Researching topics and synthesizing information
- Fact-checking and verification
- Summarizing complex information clearly
- Finding relevant sources and references
- Answering questions with well-researched responses

Always cite sources when available. Present information in a clear, organized manner with proper formatting.",
                IsPreset = true
            }
        };
    }
}

