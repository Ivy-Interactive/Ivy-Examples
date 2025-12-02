namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Blade 2: Agent configuration form
/// </summary>
public class AgentSettingsView : ViewBase
{
    private readonly AgentConfiguration _agent;
    private readonly IState<List<AgentConfiguration>> _agents;
    private readonly bool _isNew;

    public AgentSettingsView(
        AgentConfiguration agent,
        IState<List<AgentConfiguration>> agents,
        bool isNew)
    {
        _agent = agent;
        _agents = agents;
        _isNew = isNew;
    }

    public override object? Build()
    {
        var blades = this.UseContext<IBladeController>();
        var client = UseService<IClientProvider>();

        var form = UseState(AgentFormModel.FromConfiguration(_agent));
        var hasChanges = UseState(false);
        
        var nameState = UseState(form.Value.Name);
        var descState = UseState(form.Value.Description);
        var instState = UseState(form.Value.Instructions);

        UseEffect(() =>
        {
            form.Set(form.Value with 
            { 
                Name = nameState.Value,
                Description = descState.Value,
                Instructions = instState.Value
            });
        }, [nameState, descState, instState]);

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

        var formContent = Layout.Vertical().Gap(3).Padding(2)
            | (Layout.Vertical().Gap(1)
                | Text.Small("Name").Bold()
                | nameState.ToTextInput(placeholder: "Agent name...")
                    .Disabled(isReadOnly))
            | (Layout.Vertical().Gap(1)
                | Text.Small("Description").Bold()
                | descState.ToTextInput(placeholder: "Short description...")
                    .Disabled(isReadOnly))
            | (Layout.Vertical().Gap(1)
                | Text.Small("Instructions (System Prompt)").Bold()
                | instState.ToTextAreaInput(placeholder: "Instructions for the AI agent...")
                    .Height(Size.Units(150))
                    .Disabled(isReadOnly));

        // Action buttons
        var actions = isReadOnly
            ? Layout.Horizontal().Gap(1)
                | new Button("Close", onClick: _ => CancelEdit(), variant: ButtonVariant.Outline)
            : Layout.Horizontal().Gap(1)
                | new Button("Cancel", onClick: _ => CancelEdit(), variant: ButtonVariant.Outline)
                | new Button(_isNew ? "Create" : "Save", onClick: _ => SaveAgent());

        // Header info for preset agents
        var presetInfo = _agent.IsPreset && !_isNew
            ? new Card(
                Layout.Vertical().Gap(1).Padding(2)
                | Text.Small("This is a preset agent. Settings are read-only. Use 'Duplicate' from the list to create an editable copy.")
            )
            : null;

        return Layout.Vertical().Gap(3).Padding(2)
            | presetInfo
            | formContent
            | actions;
    }
}

