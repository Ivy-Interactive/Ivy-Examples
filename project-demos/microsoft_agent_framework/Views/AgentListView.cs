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

        void StartChat(AgentConfiguration agent)
        {
            if (!hasOllamaConfig)
            {
                client.Toast("Please configure Ollama URL and model first", "Warning");
                isSettingsOpen.Set(true);
                return;
            }
            blades.Push(this, new AgentChatView(agent, _agents, _ollamaUrl.Value!, _ollamaModel.Value!, _bingApiKey.Value), agent.Name, Size.Units(220));
        }

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var agent = (AgentConfiguration)e.Sender.Tag!;
            StartChat(agent);
        });

        ListItem CreateItem(AgentConfiguration agent) =>
            new(
                title: agent.Name,
                subtitle: string.IsNullOrEmpty(agent.Description) ? (agent.IsPreset ? "Preset agent" : "Custom agent") : agent.Description,
                onClick: onItemClicked,
                tag: agent
            );

        async Task<AgentConfiguration[]> FetchAgents(string filter)
        {
            await Task.CompletedTask; // Make it async for FilteredListView
            
            var allAgents = _agents.Value?.ToList() ?? new List<AgentConfiguration>();
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filter = filter.Trim();
                allAgents = allAgents
                    .Where(a => a != null && 
                               (a.Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (a.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            return allAgents.ToArray();
        }

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            var newAgent = new AgentConfiguration();
            blades.Push(this, new AgentSettingsView(newAgent, _agents, isNew: true), "New Agent", width: Size.Units(150));
        }).Ghost().Tooltip("Create Agent");

        return new Fragment()
            | new FilteredListView<AgentConfiguration>(
                fetchRecords: FetchAgents,
                createItem: CreateItem,
                toolButtons: createBtn
            );
    }
}

