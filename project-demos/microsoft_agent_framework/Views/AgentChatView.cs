namespace MicrosoftAgentFramework.Views;

/// <summary>
/// Blade 3: Chat interface with the agent
/// </summary>
public class AgentChatView : ViewBase
{
    private readonly AgentConfiguration _agent;
    private readonly string _ollamaUrl;
    private readonly string _ollamaModel;
    private readonly string? _bingApiKey;

    public AgentChatView(
        AgentConfiguration agent,
        string ollamaUrl,
        string ollamaModel,
        string? bingApiKey)
    {
        _agent = agent;
        _ollamaUrl = ollamaUrl;
        _ollamaModel = ollamaModel;
        _bingApiKey = bingApiKey;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // Create agent manager
        var agentManager = UseState<AgentManager?>(default(AgentManager?));
        
        // Initialize welcome message
        var welcomeMessage = $"Hello! I'm **{_agent.Name}**. {_agent.Description}\n\n" +
            "How can I help you today?";
        
        var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
            new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(welcomeMessage))
        ));

        // Initialize agent manager
        UseEffect(() =>
        {
            var manager = new AgentManager(_ollamaUrl, _ollamaModel, _bingApiKey);
            manager.ConfigureAgent(_agent);
            agentManager.Set(manager);
        }, []);

        void ClearChat()
        {
            agentManager.Value?.ClearHistory();
            messages.Set(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown($"Chat cleared. I'm **{_agent.Name}**. How can I help you?"))
            ));
        }

        async void HandleMessageAsync(Event<Chat, string> @event)
        {
            if (agentManager.Value == null)
            {
                client.Toast("Agent not initialized", "Error");
                return;
            }

            // Add user message
            messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

            // Show thinking indicator
            var currentMessages = messages.Value;
            messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

            try
            {
                var response = await agentManager.Value.SendMessageAsync(@event.Value);

                // Add assistant response
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(response))));
            }
            catch (Exception ex)
            {
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")));
            }
        }

        // Header with agent info and clear button
        var header = Layout.Horizontal().Gap(2).Align(Align.Center)
            | new Spacer()
            | new Button("Clear", onClick: _ => ClearChat(), variant: ButtonVariant.Outline, icon: Icons.RefreshCw);

        return Layout.Vertical().Gap(2)
            | header.Padding(2)
            | new Chat(messages.Value.ToArray(), HandleMessageAsync);
    }
}

