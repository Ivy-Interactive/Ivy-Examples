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

        // Available Ollama models
        var availableModels = new[] { "llama2", "llama3", "mistral", "phi3", "gemma", "codellama", "qwen" };
        var selectedModel = UseState(ollamaModel.Value ?? "llama2");
        
        // Query function for AsyncSelectInput
        Task<Option<string>[]> QueryModels(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Task.FromResult(availableModels.Take(5).Select(m => new Option<string>(m)).ToArray());

            return Task.FromResult(availableModels
                .Where(m => m.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(m => new Option<string>(m))
                .ToArray());
        }

        // Lookup function for AsyncSelectInput
        Task<Option<string>?> LookupModel(string? model)
        {
            if (string.IsNullOrEmpty(model)) return Task.FromResult<Option<string>?>(null);
            return Task.FromResult<Option<string>?>(new Option<string>(model));
        }
        
        // Update model when selected
        UseEffect(() =>
        {
            if (selectedModel.Value != ollamaModel.Value)
            {
                ollamaModel.Set(selectedModel.Value);
            }
        }, [selectedModel]);

        // Settings state
        var isSettingsOpen = UseState(false);
        var settingsForm = UseState(new ApiSettingsModel
        {
            OllamaUrl = ollamaUrl.Value ?? "http://localhost:11434",
            OllamaModel = ollamaModel.Value ?? "llama2",
            BingApiKey = bingApiKey.Value ?? string.Empty
        });

        // Handle settings save
        UseEffect(() =>
        {
            if (!isSettingsOpen.Value)
            {
                if (!string.IsNullOrWhiteSpace(settingsForm.Value.OllamaUrl) && settingsForm.Value.OllamaUrl != ollamaUrl.Value)
                {
                    ollamaUrl.Set(settingsForm.Value.OllamaUrl);
                }
                if (!string.IsNullOrWhiteSpace(settingsForm.Value.OllamaModel) && settingsForm.Value.OllamaModel != ollamaModel.Value)
                {
                    ollamaModel.Set(settingsForm.Value.OllamaModel);
                    selectedModel.Set(settingsForm.Value.OllamaModel);
                }
                if (settingsForm.Value.BingApiKey != bingApiKey.Value)
                {
                    bingApiKey.Set(settingsForm.Value.BingApiKey);
                }
            }
        }, [isSettingsOpen]);

        // Settings button
        var settingsBtn = Icons.Settings.ToButton(_ =>
        {
            settingsForm.Set(new ApiSettingsModel
            {
                OllamaUrl = ollamaUrl.Value ?? "http://localhost:11434",
                OllamaModel = ollamaModel.Value ?? "llama2",
                BingApiKey = bingApiKey.Value ?? string.Empty
            });
            isSettingsOpen.Set(true);
        }).Ghost().Tooltip("Settings");

        // Model selector using AsyncSelectInput
        var modelSelector = selectedModel.ToAsyncSelectInput(QueryModels, LookupModel, placeholder: "Search models...");

        // Header with title, model selector, and settings - full width at top
        var header = Layout.Vertical()
            | (Layout.Horizontal()
                | (Layout.Vertical().Align(Align.Left).Margin(10, 0, 0, 0)
                    | Text.H4("Microsoft Agent Framework"))
                | (Layout.Vertical().Align(Align.Center)
                    | (Layout.Vertical()
                        | modelSelector)).Width(Size.Fraction(0.3f))
                | (Layout.Vertical().Align(Align.Right)
                    | settingsBtn).Width(Size.Units(5)).Margin(2, 0, 10, 0)
            );

        // Settings dialog
        var settingsDialog = isSettingsOpen.Value
            ? settingsForm.ToForm()
                .Builder(e => e.OllamaUrl, e => e.ToTextInput(placeholder: "http://localhost:11434"))
                .Label(e => e.OllamaUrl, "Ollama URL")
                .Builder(e => e.OllamaModel, e => e.ToTextInput(placeholder: "llama2"))
                .Label(e => e.OllamaModel, "Ollama Model")
                .Builder(e => e.BingApiKey, e => e.ToPasswordInput(placeholder: "Optional - for web search"))
                .Label(e => e.BingApiKey, "Bing Search API Key")
                .ToDialog(isSettingsOpen,
                    title: "Ollama Settings",
                    submitTitle: "Save",
                    width: Size.Fraction(0.5f))
            : null;

        // Content with blades
        var content = this.UseBlades(
            () => new AgentListView(agents, ollamaUrl, ollamaModel, bingApiKey),
            "Agents"
        );

        return new Fragment()
            | header
            | new Separator()
            | content
            | settingsDialog;
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

