namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Blade 1: List of all agents with create/edit/delete functionality
/// </summary>
public class AgentListView : ViewBase
{
    private readonly IState<List<AgentConfiguration>> _agents;
    private readonly IState<string?> _ollamaUrl;
    private readonly IState<string?> _ollamaModel;
    private readonly IState<string?> _bingApiKey;

    public AgentListView(
        IState<List<AgentConfiguration>> agents,
        IState<string?> ollamaUrl,
        IState<string?> ollamaModel,
        IState<string?> bingApiKey)
    {
        _agents = agents;
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
        _bingApiKey = bingApiKey;
    }

    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var client = UseService<IClientProvider>();
        var isSettingsOpen = UseState(false);
        var settingsForm = UseState(new ApiSettingsModel
        {
            OllamaUrl = _ollamaUrl.Value ?? "http://localhost:11434",
            OllamaModel = _ollamaModel.Value ?? "llama2",
            BingApiKey = _bingApiKey.Value ?? string.Empty
        });

        var hasOllamaConfig = !string.IsNullOrWhiteSpace(_ollamaUrl.Value) && !string.IsNullOrWhiteSpace(_ollamaModel.Value);

        // Handle settings save
        UseEffect(() =>
        {
            if (!isSettingsOpen.Value)
            {
                if (!string.IsNullOrWhiteSpace(settingsForm.Value.OllamaUrl) && settingsForm.Value.OllamaUrl != _ollamaUrl.Value)
                {
                    _ollamaUrl.Set(settingsForm.Value.OllamaUrl);
                }
                if (!string.IsNullOrWhiteSpace(settingsForm.Value.OllamaModel) && settingsForm.Value.OllamaModel != _ollamaModel.Value)
                {
                    _ollamaModel.Set(settingsForm.Value.OllamaModel);
                }
                if (settingsForm.Value.BingApiKey != _bingApiKey.Value)
                {
                    _bingApiKey.Set(settingsForm.Value.BingApiKey);
                }
            }
        }, [isSettingsOpen]);

        void CreateNewAgent()
        {
            var newAgent = new AgentConfiguration();
            blades.Push(this, new AgentSettingsView(newAgent, _agents, isNew: true), "New Agent");
        }

        void EditAgent(AgentConfiguration agent)
        {
            blades.Push(this, new AgentSettingsView(agent, _agents, isNew: false), agent.Name);
        }

        void StartChat(AgentConfiguration agent)
        {
            if (!hasOllamaConfig)
            {
                client.Toast("Please configure Ollama URL and model first", "Warning");
                isSettingsOpen.Set(true);
                return;
            }
            blades.Push(this, new AgentChatView(agent, _ollamaUrl.Value!, _ollamaModel.Value!, _bingApiKey.Value), agent.Name);
        }

        void DeleteAgent(AgentConfiguration agent)
        {
            if (agent.IsPreset)
            {
                client.Toast("Cannot delete preset agents", "Warning");
                return;
            }
            var list = _agents.Value.ToList();
            list.Remove(agent);
            _agents.Set(list);
        }

        void DuplicateAgent(AgentConfiguration agent)
        {
            var clone = agent.Clone();
            var list = _agents.Value.ToList();
            list.Add(clone);
            _agents.Set(list);
        }

        // Build list items
        var presetAgents = _agents.Value.Where(a => a.IsPreset).ToList();
        var customAgents = _agents.Value.Where(a => !a.IsPreset).ToList();


        var presetItems = presetAgents.Select(agent =>
        {
            var actions = Layout.Horizontal().Gap(1)
                | new Button(icon: Icons.MessageCircle, onClick: _ => StartChat(agent), variant: ButtonVariant.Outline).Tooltip("Chat")
                | new Button(icon: Icons.Copy, onClick: _ => DuplicateAgent(agent), variant: ButtonVariant.Outline).Tooltip("Duplicate")
                | new Button(icon: Icons.Eye, onClick: _ => EditAgent(agent), variant: ButtonVariant.Outline).Tooltip("View");

            return new Card(
                Layout.Horizontal().Gap(2).Padding(2)
                    | (Layout.Vertical().Gap(0)
                        | Text.Block(agent.Name).Bold()
                        | Text.Small(agent.Description).Color(Colors.Gray))
                    | new Spacer()
                    | actions
            );
        }).ToList();

        var customItems = customAgents.Select(agent =>
        {
            var actions = Layout.Horizontal().Gap(1)
                | new Button(icon: Icons.MessageCircle, onClick: _ => StartChat(agent), variant: ButtonVariant.Outline).Tooltip("Chat")
                | new Button(icon: Icons.Pencil, onClick: _ => EditAgent(agent), variant: ButtonVariant.Outline).Tooltip("Edit")
                | new Button(icon: Icons.Copy, onClick: _ => DuplicateAgent(agent), variant: ButtonVariant.Outline).Tooltip("Duplicate")
                | new Button(icon: Icons.Trash, onClick: _ => DeleteAgent(agent), variant: ButtonVariant.Outline).Tooltip("Delete");

            return new Card(
                Layout.Horizontal().Gap(2).Padding(2)
                    | (Layout.Vertical().Gap(0)
                        | Text.Block(agent.Name).Bold()
                        | Text.Small(string.IsNullOrEmpty(agent.Description) ? "Custom agent" : agent.Description).Color(Colors.Gray))
                    | new Spacer()
                    | actions
            );
        }).ToList();

        // Header with create button
        var header = Layout.Horizontal().Gap(1)
            | new Button(icon: Icons.Plus, onClick: _ => CreateNewAgent(), variant: ButtonVariant.Outline)
            | new Button(icon: Icons.Settings, onClick: _ => 
            {
                settingsForm.Set(new ApiSettingsModel
                {
                    OllamaUrl = _ollamaUrl.Value ?? "http://localhost:11434",
                    OllamaModel = _ollamaModel.Value ?? "llama2",
                    BingApiKey = _bingApiKey.Value ?? string.Empty
                });
                isSettingsOpen.Set(true);
            }, variant: ButtonVariant.Outline);

        // Status indicator
        var statusBadge = hasOllamaConfig 
            ? new Badge($"Ollama: {_ollamaModel.Value}", BadgeVariant.Success)
            : new Badge("Ollama Config Required", BadgeVariant.Destructive);

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

        // Main content
        var content = Layout.Vertical().Gap(2)
            | statusBadge
            | (presetItems.Any() 
                ? (Layout.Vertical().Gap(1)
                    | Text.Small("Preset Agents").Bold().Color(Colors.Gray)
                    | (Layout.Vertical().Gap(1) | presetItems))
                : null)
            | (customItems.Any() 
                ? (Layout.Vertical().Gap(1)
                    | Text.Small("Custom Agents").Bold().Color(Colors.Gray)
                    | (Layout.Vertical().Gap(1) | customItems))
                : null)
            | (!customItems.Any() && !hasOllamaConfig
                ? Text.Muted("Configure Ollama URL and model, then create agents to get started")
                : null);

        return new Fragment()
            | BladeHelper.WithHeader(header, content)
            | settingsDialog;
    }
}

