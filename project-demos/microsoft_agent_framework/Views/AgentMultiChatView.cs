namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Multi-agent tabbed chat view that shares state with parent
/// </summary>
public class AgentMultiChatView(
    IState<List<AgentConfiguration>> agents,
    IState<string?> ollamaUrl,
    IState<string?> ollamaModel,
    IState<List<AgentConfiguration>> openAgentsList) : ViewBase
{
    private readonly IState<List<AgentConfiguration>> _agents = agents;
    private readonly IState<string?> _ollamaUrl = ollamaUrl;
    private readonly IState<string?> _ollamaModel = ollamaModel;
    private readonly IState<List<AgentConfiguration>> _openAgentsList = openAgentsList;

    private class AgentChatView : ViewBase
    {
        private readonly AgentConfiguration _agent;
        private readonly IState<List<AgentConfiguration>> _agents;
        private readonly IState<string?> _ollamaUrl;
        private readonly IState<string?> _ollamaModel;

        public AgentChatView(AgentConfiguration agent, IState<List<AgentConfiguration>> agents, IState<string?> ollamaUrl, IState<string?> ollamaModel)
        {
            _agent = agent;
            _agents = agents;
            _ollamaUrl = ollamaUrl;
            _ollamaModel = ollamaModel;
        }

        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            var agentManager = UseState<AgentManager?>(default(AgentManager?));
            var isInitializing = UseState(false);
            var welcomeMessage = $"Hello! I'm **{_agent.Name}**. {_agent.Description}\n\nHow can I help you today?";
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(welcomeMessage))));

            UseEffect(async () =>
            {
                isInitializing.Set(true);
                try
                {
                    var manager = new AgentManager(_ollamaUrl.Value ?? "", _ollamaModel.Value ?? "");
                    await manager.ConfigureAgentAsync(_agent);
                    agentManager.Set(manager);
                }
                finally { isInitializing.Set(false); }
            }, EffectTrigger.AfterInit());

            UseEffect(async () =>
            {
                var updatedAgent = _agents.Value?.FirstOrDefault(a => a.Id == _agent.Id);
                if (updatedAgent != null && agentManager.Value != null)
                {
                    await agentManager.Value.ConfigureAgentAsync(updatedAgent);
                    if (messages.Value.Length == 1)
                    {
                        var newWelcome = $"Hello! I'm **{updatedAgent.Name}**. {updatedAgent.Description}\n\nHow can I help you today?";
                        messages.Set(ImmutableArray.Create<Ivy.ChatMessage>(new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(newWelcome))));
                    }
                }
            }, [_agents]);

            async void HandleMessageAsync(Event<Ivy.Chat, string> @event)
            {
                if (agentManager.Value == null)
                {
                    client.Toast("Agent is initializing, please wait...", "Info");
                    return;
                }

                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));
                var assistantIndex = messages.Value.Length;
                var streamingText = new System.Text.StringBuilder();
                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

                void UpdateMessage(string text)
                {
                    var list = messages.Value.ToList();
                    list[assistantIndex] = new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(text));
                    messages.Set(list.ToImmutableArray());
                }
                void SetError(string error)
                {
                    var list = messages.Value.ToList();
                    list[assistantIndex] = new Ivy.ChatMessage(ChatSender.Assistant, $"Error: {error}");
                    messages.Set(list.ToImmutableArray());
                }

                async Task ProcessStream(string message)
                {
                    await foreach (var update in agentManager.Value!.RunStreamingAsync(message))
                    {
                        var text = update.Text ?? update.ToString() ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            streamingText.Append(text);
                            UpdateMessage(streamingText.ToString());
                        }
                    }
                }

                try
                {
                    await ProcessStream(@event.Value);
                }
                catch (Exception ex) when (ex.Message.Contains("does not support tools", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await agentManager.Value.RecreateAgentWithoutToolsAsync();
                        streamingText.Clear();
                        await ProcessStream(@event.Value);
                    }
                    catch (Exception retryEx) { SetError(retryEx.Message); }
                }
                catch (Exception ex) { SetError(ex.Message); }
            }

            return Layout.Vertical().Gap(2)
                | new Ivy.Chat(messages.Value.ToArray(), isInitializing.Value ? null : HandleMessageAsync)
                | (isInitializing.Value ? Layout.Center().Padding(2) | Text.Muted("Initializing agent...") : null);
        }
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var selectedTabIndex = UseState<int?>(Math.Max(0, _openAgentsList.Value.Count - 1));
        var isEditDialogOpen = UseState(false);
        var editForm = UseState(new AgentFormModel());
        var modelState = UseState(editForm.Value.OllamaModel);
        var availableModels = UseState<ImmutableArray<string>>(ImmutableArray<string>.Empty);

        AgentConfiguration? GetCurrentAgent() => 
            selectedTabIndex.Value.HasValue && selectedTabIndex.Value.Value < _openAgentsList.Value.Count
                ? _openAgentsList.Value[selectedTabIndex.Value.Value]
                : null;

        UseEffect(async () =>
        {
            var url = _ollamaUrl.Value;
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    using var apiClient = new OllamaApiClient(new Uri(url));
                    availableModels.Set((await apiClient.ListLocalModelsAsync()).Select(m => m.Name).ToImmutableArray());
                }
                catch { availableModels.Set(ImmutableArray<string>.Empty); }
            }
        }, EffectTrigger.AfterInit());

        Task<Option<string>[]> QueryModels(string query)
        {
            var models = availableModels.Value;
            if (models.IsEmpty) return Task.FromResult(Array.Empty<Option<string>>());
            var filtered = string.IsNullOrEmpty(query) ? models : models.Where(m => m.Contains(query, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(filtered.Select(m => new Option<string>(m)).ToArray());
        }

        Task<Option<string>?> LookupModel(string? model) => 
            Task.FromResult<Option<string>?>(string.IsNullOrEmpty(model) ? null : new Option<string>(model));

        UseEffect(() =>
        {
            if (modelState.Value != editForm.Value.OllamaModel)
                editForm.Set(editForm.Value with { OllamaModel = modelState.Value });
        }, [modelState]);

        UseEffect(() =>
        {
            var agent = GetCurrentAgent();
            if (agent != null)
            {
                var form = AgentFormModel.FromConfiguration(agent);
                editForm.Set(form);
                modelState.Set(form.OllamaModel);
            }
        }, [selectedTabIndex, _openAgentsList]);

        UseEffect(async () =>
        {
            var agent = GetCurrentAgent();
            if (!isEditDialogOpen.Value && agent != null && (editForm.Value.Name != agent.Name || editForm.Value.OllamaModel != agent.OllamaModel))
            {
                editForm.Value.ApplyTo(agent);
                _agents.Set(_agents.Value.ToList());
                client.Toast($"Agent '{agent.Name}' updated", "Success");
                var form = AgentFormModel.FromConfiguration(agent);
                editForm.Set(form);
                modelState.Set(form.OllamaModel);
            }
        }, [isEditDialogOpen]);

        ValueTask OnTabSelect(Event<TabsLayout, int> e)
        {
            selectedTabIndex.Set(e.Value);
            return ValueTask.CompletedTask;
        }

        ValueTask OnTabClose(Event<TabsLayout, int> e)
        {
            if (e.Value >= 0 && e.Value < _openAgentsList.Value.Count)
            {
                var updated = _openAgentsList.Value.ToList();
                updated.RemoveAt(e.Value);
                _openAgentsList.Set(updated);
                selectedTabIndex.Set(updated.Count == 0 ? null : Math.Min(selectedTabIndex.Value ?? 0, updated.Count - 1));
            }
            return ValueTask.CompletedTask;
        }

        var tabs = _openAgentsList.Value
            .Select(agent => new Tab(agent.Name, new AgentChatView(agent, _agents, _ollamaUrl, _ollamaModel))
                .Icon(Icons.MessageSquare))
            .ToArray();

        if (tabs.Length == 0)
            return Layout.Vertical().Gap(3).Padding(4).Align(Align.Center)
                | Text.Large("No agents open")
                | Text.Muted("Select an agent from the list to start chatting");

        var currentIndex = Math.Min(selectedTabIndex.Value ?? 0, tabs.Length - 1);
        if (currentIndex != selectedTabIndex.Value) selectedTabIndex.Set(currentIndex);

        var currentAgent = GetCurrentAgent();
        var editButton = currentAgent != null
            ? Layout.Horizontal().Gap(2).Align(Align.Center)
                | Text.Label(currentAgent.Name).Bold()
                | new Button("Edit", icon: Icons.Pencil, onClick: _ =>
                {
                    var agent = GetCurrentAgent();
                    if (agent != null)
                    {
                        var form = AgentFormModel.FromConfiguration(agent);
                        editForm.Set(form);
                        modelState.Set(form.OllamaModel);
                        isEditDialogOpen.Set(true);
                    }
                }).Ghost().Tooltip("Edit agent settings")
            : null;

        var editDialog = isEditDialogOpen.Value && currentAgent != null
            ? editForm.ToForm()
                .Builder(e => e.Name, e => e.ToTextInput(placeholder: "Agent name..."))
                .Label(e => e.Name, "Name")
                .Builder(e => e.Description, e => e.ToTextInput(placeholder: "Short description..."))
                .Label(e => e.Description, "Description")
                .Builder(e => e.OllamaModel, e => modelState.ToAsyncSelectInput(QueryModels, LookupModel, placeholder: "Search models..."))
                .Label(e => e.OllamaModel, "Ollama Model")
                .Builder(e => e.Instructions, e => e.ToTextAreaInput(placeholder: "Instructions for the AI agent...").Height(Size.Units(50)))
                .Label(e => e.Instructions, "Instructions (System Prompt)")
                .ToDialog(isEditDialogOpen, title: "Edit Agent", submitTitle: "Save", width: Size.Fraction(0.8f))
            : null;

        var tabsLayout = CreateTabsLayout(OnTabSelect, OnTabClose, currentIndex, tabs).Variant(TabsVariant.Tabs);
        return new Fragment()
            | (editButton != null ? BladeHelper.WithHeader(editButton, tabsLayout) : tabsLayout)
            | editDialog;
    }

    // Helper to create TabsLayout (max 10 tabs due to constructor limitation)
    private static TabsLayout CreateTabsLayout(
        Func<Event<TabsLayout, int>, ValueTask> onSelect,
        Func<Event<TabsLayout, int>, ValueTask> onClose,
        int index,
        Tab[] t) => t.Length switch
    {
        1 => new TabsLayout(onSelect, onClose, null, null, index, t[0]),
        2 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1]),
        3 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2]),
        4 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3]),
        5 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4]),
        6 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5]),
        7 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6]),
        8 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7]),
        9 => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7], t[8]),
        _ => new TabsLayout(onSelect, onClose, null, null, index, t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7], t[8], t[9])
    };
}

