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

        async void HandleMessageAsync(Event<Chat, string> @event)
        {
            if (agentManager.Value == null)
            {
                client.Toast("Agent not initialized", "Error");
                return;
            }

            // Add user message
            messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

            // Create initial empty assistant message for streaming
            var assistantMessageIndex = messages.Value.Length;
            var streamingText = new System.Text.StringBuilder();
            messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.Assistant, Text.Markdown(""))));

            try
            {
                // Stream response word by word
                await foreach (var update in agentManager.Value.RunStreamingAsync(@event.Value))
                {
                    var textUpdate = update.Text ?? update.ToString() ?? "";
                    if (!string.IsNullOrEmpty(textUpdate))
                    {
                        streamingText.Append(textUpdate);
                        
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

        return Layout.Vertical().Gap(2)
            | new Chat(messages.Value.ToArray(), HandleMessageAsync);
    }
}

