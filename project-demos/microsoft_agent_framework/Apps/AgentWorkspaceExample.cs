using MicrosoftAgentFramework.Models;
using MicrosoftAgentFramework.Views;
using OllamaSharp;

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

        // Available Ollama models - loaded dynamically
        var availableModels = UseState<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        var selectedModel = UseState(ollamaModel.Value ?? "llama2");
        
        // Load models from Ollama API
        async Task LoadModels()
        {
            try
            {
                using var client = new OllamaApiClient(new Uri(ollamaUrl.Value ?? "http://localhost:11434"));
                availableModels.Set((await client.ListLocalModelsAsync()).Select(m => m.Name).ToImmutableArray());
            }
            catch { availableModels.Set(ImmutableArray<string>.Empty); }
        }
        
        UseEffect(async () => await LoadModels(), EffectTrigger.AfterInit());
        UseEffect(async () => await LoadModels(), [ollamaUrl]);
        
        // Query function for AsyncSelectInput
        Task<Option<string>[]> QueryModels(string query)
        {
            var models = availableModels.Value;
            if (models.IsEmpty) return Task.FromResult(Array.Empty<Option<string>>());
            
            var filtered = string.IsNullOrEmpty(query) 
                ? models.Take(5) 
                : models.Where(m => m.Contains(query, StringComparison.OrdinalIgnoreCase));
            
            return Task.FromResult(filtered.Select(m => new Option<string>(m)).ToArray());
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
                Name = "Story Writer with Research",
                Description = "Creative writer with web research and time awareness",
                Instructions = @"You are a Creative Writer AI assistant with access to research tools. Your expertise includes:
- Crafting engaging stories, poems, and creative content
- Researching historical facts and current events to enhance your writing
- Using accurate dates and time references in narratives
- Developing compelling narratives with factual accuracy

When writing:
- Use SearchWeb to research historical events, locations, or facts to make your stories more authentic
- Use GetCurrentTime to reference accurate dates and times in your narratives
- Use Calculate for word counts, deadlines, or any numerical aspects of writing projects

Use vivid language, metaphors, and creative expression. Always verify facts through web search when writing about real events or places. Format your responses with proper Markdown when appropriate.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-analyst",
                Name = "Mathematical Calculator & Analyst",
                Description = "Expert calculator with web data access",
                Instructions = @"You are a Data Analyst AI assistant with powerful calculation and research capabilities. Your expertise includes:
- Performing complex mathematical calculations and statistical analysis
- Analyzing data patterns and trends
- Finding current data and statistics from the web
- Explaining calculations step-by-step
- Creating data visualizations descriptions

IMPORTANT: Always use the Calculate tool for ANY mathematical operation, computation, or calculation. Never attempt calculations manually.

When analyzing:
- Use Calculate for all mathematical operations, formulas, and statistical computations
- Use SearchWeb to find current statistics, data sets, or research findings
- Use GetCurrentTime when working with time-series data or date-based analysis

Be precise, analytical, and data-driven. Always show your work and explain your calculations. Present findings clearly with proper formatting.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-coder",
                Name = "Developer Assistant with Tools",
                Description = "Code expert with calculation and documentation search",
                Instructions = @"You are a Code Assistant AI with access to calculation and research tools. Your expertise includes:
- Writing clean, efficient code in multiple languages
- Debugging and troubleshooting code issues
- Finding up-to-date documentation and API references
- Performing calculations for algorithm analysis
- Explaining programming concepts clearly

When coding:
- Use SearchWeb to find current documentation, library information, API references, or solutions to programming problems
- Use Calculate for algorithm complexity analysis, performance metrics, or any mathematical computations
- Use GetCurrentTime when working with date/time operations, timestamps, or scheduling code

Always format code using proper Markdown code blocks with language syntax highlighting. Search for the latest documentation before providing solutions. Be precise and provide working, tested solutions when possible. Explain your reasoning.",
                IsPreset = true
            },
            new AgentConfiguration
            {
                Id = "preset-researcher",
                Name = "Web Research Assistant",
                Description = "Expert researcher with web search and calculation tools",
                Instructions = @"You are a Research Assistant AI with direct access to web search capabilities. Your expertise includes:
- Searching the web for current information, facts, and verified data
- Fact-checking and verification using web sources
- Performing statistical analysis on research findings
- Finding relevant sources and references
- Providing well-researched, up-to-date responses

CRITICAL: Always use SearchWeb to find current information, facts, news, and verified data. This is your primary research tool - never rely solely on your training data for current information.

When researching:
- Use SearchWeb as your PRIMARY tool for finding information, facts, news, and current data
- Use Calculate for any statistical analysis, data calculations, or numerical research findings
- Use GetCurrentTime to provide context about when information was current or to reference dates

Always cite sources when available. Present information in a clear, organized manner with proper formatting. Verify facts through web search before presenting them as accurate.",
                IsPreset = true
            }
        };
    }
}

