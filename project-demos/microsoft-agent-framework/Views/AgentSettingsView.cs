namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Blade 2: Agent configuration form
/// </summary>
public class AgentSettingsView : ViewBase
{
    private readonly AgentConfiguration _agent;
    private readonly IState<List<AgentConfiguration>> _agents;
    private readonly bool _isNew;
    private readonly string? _ollamaUrl;

    public AgentSettingsView(
        AgentConfiguration agent,
        IState<List<AgentConfiguration>> agents,
        bool isNew,
        string? ollamaUrl = null)
    {
        _agent = agent;
        _agents = agents;
        _isNew = isNew;
        _ollamaUrl = ollamaUrl;
    }

    public override object? Build()
    {
        var blades = this.UseContext<IBladeService>();
        var client = UseService<IClientProvider>();

        var form = UseState(AgentFormModel.FromConfiguration(_agent));
        var hasChanges = UseState(false);

        var nameState = UseState(form.Value.Name);
        var descState = UseState(form.Value.Description);
        var instState = UseState(form.Value.Instructions);
        var modelState = UseState(form.Value.OllamaModel);

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
            form.Set(form.Value with
            {
                Name = nameState.Value,
                Description = descState.Value,
                Instructions = instState.Value,
                OllamaModel = modelState.Value
            });
        }, [nameState, descState, instState, modelState]);

        void SaveAgent()
        {
            if (string.IsNullOrWhiteSpace(form.Value.Name))
            {
                client.Toast("Agent name is required", "Error");
                return;
            }

            if (_isNew)
            {
                var newAgent = form.Value.ToConfiguration();
                var list = _agents.Value.ToList();
                list.Add(newAgent);
                _agents.Set(list);
                client.Toast($"Agent '{newAgent.Name}' created", "Success");
            }
            else
            {
                form.Value.ApplyTo(_agent);
                // Trigger refresh
                _agents.Set(_agents.Value.ToList());
                client.Toast($"Agent '{_agent.Name}' updated", "Success");
            }

            blades.Pop(refresh: true);
        }

        void CancelEdit()
        {
            blades.Pop();
        }

        // Build form content
        var isReadOnly = _agent.IsPreset && !_isNew;
        // Action buttons
        var actions = isReadOnly
            ? Layout.Horizontal().Gap(1)
                | new Button("Close", onClick: _ => CancelEdit(), variant: ButtonVariant.Outline)
            : Layout.Horizontal().Gap(1)
                | new Button("Cancel", onClick: _ => CancelEdit(), variant: ButtonVariant.Outline)
                | new Button(_isNew ? "Create" : "Save", onClick: _ => SaveAgent());

        // Model selector using AsyncSelectInput or TextInput as fallback
        var modelInput = modelState.ToAsyncSelectInput<string>(QueryModels, LookupModel, placeholder: "Search models...").Disabled(isReadOnly);
        

        var formContent = new Card(Layout.Vertical().Gap(3).Padding(2)
            | (Layout.Vertical().Gap(1)
                | Text.Small("Name").Bold()
                | nameState.ToTextInput(placeholder: "Agent name...")
                    .Disabled(isReadOnly))
            | (Layout.Vertical().Gap(1)
                | Text.Small("Description").Bold()
                | descState.ToTextInput(placeholder: "Short description...")
                    .Disabled(isReadOnly))
            | (Layout.Vertical().Gap(1)
                | Text.Small("Ollama Model").Bold()
                | modelInput)
            | (Layout.Vertical().Gap(1)
                | Text.Small("Instructions (System Prompt)").Bold()
                | instState.ToTextAreaInput(placeholder: "Instructions for the AI agent...")
                    .Height(Size.Units(50))
                    .Disabled(isReadOnly))
            | (Layout.Vertical().Gap(1)
               | actions));


        // Header info for preset agents
        var presetInfo = _agent.IsPreset && !_isNew
            ? new Card(
                Layout.Vertical().Gap(1).Padding(2)
                | Text.Small("This is a preset agent. Settings are read-only. Use 'Duplicate' from the list to create an editable copy.")
            )
            : null;

        return Layout.Vertical().Gap(3).Padding(2)
            | presetInfo
            | formContent;
    }
}

