using OllamaSharp;

namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Blade 3: Chat interface with the agent
/// </summary>
public class AgentChatView : ViewBase
{
    private readonly AgentConfiguration _agent;
    private readonly IState<List<AgentConfiguration>> _agents;
    private readonly string _ollamaUrl;
    private readonly string _ollamaModel;
    private readonly string? _bingApiKey;

    public AgentChatView(
        AgentConfiguration agent,
        IState<List<AgentConfiguration>> agents,
        string ollamaUrl,
        string ollamaModel,
        string? bingApiKey)
    {
        _agent = agent;
        _agents = agents;
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
        _bingApiKey = bingApiKey;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // Dialog state for editing agent
        var isEditDialogOpen = UseState(false);
        var editForm = UseState(AgentFormModel.FromConfiguration(_agent));

        // Create agent manager
        var agentManager = UseState<AgentManager?>(default(AgentManager?));
        
        // Initialize welcome message
        var welcomeMessage = $"Hello! I'm **{_agent.Name}**. {_agent.Description}\n\n" +
            "How can I help you today?";
        
        var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
            new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(welcomeMessage))
        ));

        // Track agent changes to update manager when agent is edited
        var agentId = UseState(_agent.Id);
        
        // Initialize agent manager
        UseEffect(() =>
        {
            var manager = new AgentManager(_ollamaUrl, _ollamaModel, _bingApiKey);
            manager.ConfigureAgent(_agent);
            agentManager.Set(manager);
        }, []);

        // Update agent manager when agent is edited
        UseEffect(() =>
        {
            // Find updated agent from the list
            var updatedAgent = _agents.Value?.FirstOrDefault(a => a.Id == _agent.Id);
            if (updatedAgent != null && updatedAgent.Id == agentId.Value)
            {
                // Agent was updated, reconfigure manager
                agentManager.Value?.ConfigureAgent(updatedAgent);
                
                // Update welcome message if name or description changed
                var newWelcomeMessage = $"Hello! I'm **{updatedAgent.Name}**. {updatedAgent.Description}\n\n" +
                    "How can I help you today?";
                
                // Only update if messages still contain the original welcome message (only one message = welcome message)
                if (messages.Value.Length == 1)
                {
                    messages.Set(ImmutableArray.Create<Ivy.ChatMessage>(
                        new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(newWelcomeMessage))
                    ));
                }
            }
        }, [_agents]);

        async void HandleMessageAsync(Event<Ivy.Chat, string> @event)
        {
            if (agentManager.Value == null)
            {
                client.Toast("Agent not initialized", "Error");
                return;
            }

            // Add user message
            messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

            // Create initial assistant message with waiting status
            var assistantMessageIndex = messages.Value.Length;
            var streamingText = new System.Text.StringBuilder();
            var isWaitingForFirstWord = true;
            messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

            try
            {
                // Stream response word by word
                await foreach (var update in agentManager.Value.RunStreamingAsync(@event.Value))
                {
                    var textUpdate = update.Text ?? update.ToString() ?? "";
                    if (!string.IsNullOrEmpty(textUpdate))
                    {
                        streamingText.Append(textUpdate);
                        
                        // If this is the first word, replace waiting status with actual text
                        if (isWaitingForFirstWord)
                        {
                            isWaitingForFirstWord = false;
                        }
                        
                        // Update the assistant message with accumulated text
                        var currentMessagesList = messages.Value.ToList();
                        currentMessagesList[assistantMessageIndex] = new Ivy.ChatMessage(
                            ChatSender.Assistant, 
                            Text.Markdown(streamingText.ToString())
                        );
                        messages.Set(currentMessagesList.ToImmutableArray());
                    }
                }
            }
            catch (Exception ex)
            {
                // Replace streaming message with error
                var currentMessagesList = messages.Value.ToList();
                currentMessagesList[assistantMessageIndex] = new Ivy.ChatMessage(
                    ChatSender.Assistant, 
                    $"Error: {ex.Message}"
                );
                messages.Set(currentMessagesList.ToImmutableArray());
            }
        }

        // Handle form state updates
        var nameState = UseState(editForm.Value.Name);
        var descState = UseState(editForm.Value.Description);
        var instState = UseState(editForm.Value.Instructions);
        var modelState = UseState(editForm.Value.OllamaModel);

        // Available Ollama models - loaded dynamically
        var availableModels = UseState<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        
        // Load models from Ollama API
        async Task LoadModels()
        {
            if (string.IsNullOrWhiteSpace(_ollamaUrl)) return;
            try
            {
                using var client = new OllamaApiClient(new Uri(_ollamaUrl));
                availableModels.Set((await client.ListLocalModelsAsync()).Select(m => m.Name).ToImmutableArray());
            }
            catch 
            { 
                availableModels.Set(ImmutableArray<string>.Empty); 
            }
        }
        
        UseEffect(async () => await LoadModels(), EffectTrigger.OnMount());
        
        // Query function for AsyncSelectInput
        QueryResult<Option<string>[]> QueryModels(IViewContext context, string query)
        {
            return context.UseQuery<Option<string>[], (string, string)>(
                key: (nameof(QueryModels), query),
                fetcher: ct =>
                {
                    var models = availableModels.Value;
                    if (models.IsEmpty) return Task.FromResult(Array.Empty<Option<string>>());
                    
                    var filtered = string.IsNullOrEmpty(query) 
                        ? models.Take(10) 
                        : models.Where(m => m.Contains(query, StringComparison.OrdinalIgnoreCase));
                    
                    return Task.FromResult(filtered.Select(m => new Option<string>(m)).ToArray());
                });
        }

        // Lookup function for AsyncSelectInput
        QueryResult<Option<string>?> LookupModel(IViewContext context, string? model)
        {
            return context.UseQuery<Option<string>?, (string, string?)>(
                key: (nameof(LookupModel), model),
                fetcher: ct => Task.FromResult<Option<string>?>(
                    string.IsNullOrEmpty(model) ? null : new Option<string>(model)));
        }

        UseEffect(() =>
        {
            editForm.Set(editForm.Value with
            {
                Name = nameState.Value,
                Description = descState.Value,
                Instructions = instState.Value,
                OllamaModel = modelState.Value
            });
        }, [nameState, descState, instState, modelState]);

        // Handle save in dialog
        UseEffect(() =>
        {
            if (!isEditDialogOpen.Value && (editForm.Value.Name != _agent.Name || editForm.Value.OllamaModel != _agent.OllamaModel))
            {
                // Store old model before updating
                var oldModel = _agent.OllamaModel;
                
                // Form was saved, update agent
                editForm.Value.ApplyTo(_agent);
                _agents.Set(_agents.Value.ToList());
                client.Toast($"Agent '{_agent.Name}' updated", "Success");
                
                // If model changed, recreate agent manager with new model
                if (editForm.Value.OllamaModel != oldModel)
                {
                    var manager = new AgentManager(_ollamaUrl, editForm.Value.OllamaModel, _bingApiKey);
                    manager.ConfigureAgent(_agent);
                    agentManager.Set(manager);
                }
                else
                {
                    // Just reconfigure with updated agent
                    agentManager.Value?.ConfigureAgent(_agent);
                }
                
                // Reset form to current agent values
                editForm.Set(AgentFormModel.FromConfiguration(_agent));
                nameState.Set(_agent.Name);
                descState.Set(_agent.Description);
                instState.Set(_agent.Instructions);
                modelState.Set(_agent.OllamaModel);
            }
        }, [isEditDialogOpen]);

        // Edit button for header
        var editButton = Layout.Horizontal().Gap(2).Align(Align.Center)
            | Text.Label(_agent.Name).Bold()
            | new Button("Edit", icon: Icons.Pencil, onClick: _ =>
            {
                // Reset form to current agent values
                editForm.Set(AgentFormModel.FromConfiguration(_agent));
                nameState.Set(_agent.Name);
                descState.Set(_agent.Description);
                instState.Set(_agent.Instructions);
                modelState.Set(_agent.OllamaModel);
                isEditDialogOpen.Set(true);
            }).Ghost().Tooltip("Edit agent settings");

         
        
        var editDialog = isEditDialogOpen.Value
            ? editForm.ToForm()
                .Builder(e => e.Name, e => e.ToTextInput(placeholder: "Agent name..."))
                .Label(e => e.Name, "Name")
                .Builder(e => e.Description, e => e.ToTextInput(placeholder: "Short description..."))
                .Label(e => e.Description, "Description")
                .Builder(e => e.OllamaModel,e => modelState.ToAsyncSelectInput<string>(QueryModels, LookupModel, placeholder: "Search models..."))
                .Label(e => e.OllamaModel, "Ollama Model")
                .Builder(e => e.Instructions, e => e.ToTextAreaInput(placeholder: "Instructions for the AI agent...")
                    .Height(Size.Units(50)))
                .Label(e => e.Instructions, "Instructions (System Prompt)")
                .ToDialog(isEditDialogOpen,
                    title: "Edit Agent",
                    submitTitle: "Save",
                    width: Size.Fraction(0.8f))
            : null;

        var chatContent = Layout.Vertical().Gap(2)
            | new Ivy.Chat(messages.Value.ToArray(), HandleMessageAsync);

        return new Fragment()
            | new BladeHeader(editButton)
            | chatContent
            | editDialog;
    }
}

