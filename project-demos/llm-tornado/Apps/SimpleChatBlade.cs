namespace LlmTornadoExample.Apps;

public class SimpleChatBlade : ViewBase
{
    private readonly string _ollamaUrl;
    private readonly string _modelName;
    
    private IState<ImmutableArray<ChatMessage>> _messages;
    private TornadoApi? _api;

    public SimpleChatBlade(string ollamaUrl, string modelName)
    {
        _ollamaUrl = ollamaUrl;
        _modelName = modelName;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        _messages = UseState(ImmutableArray.Create<ChatMessage>(
            new ChatMessage(ChatSender.Assistant, "Hello! I'm powered by LlmTornado. How can I help you today?")
        ));

        // Initialize TornadoApi client
        UseEffect(async () =>
        {
            if (_api == null)
            {
                try
                {
                    _api = new TornadoApi(new Uri(_ollamaUrl));
                    // Test connection
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    client.Toast($"Failed to connect to Ollama: {ex.Message}", "Connection Error");
                }
            }
        }, EffectTrigger.AfterInit());

        return BladeHelper.WithHeader(
            Layout.Horizontal().Gap(2)
                | (Layout.Vertical().Gap(2).Align(Align.Center).Width(Size.Fit())
                    | new Icon(Icons.MessageSquare).Size(8))
                | Text.H4($"Simple Chat - {_modelName}"),
            Layout.Vertical().Width(Size.Units(200).Max(Size.Units(400)))
                | new Chat(_messages.Value.ToArray(), OnSendMessage)
        );
    }

    private void OnSendMessage(Event<Chat, string> @event)
    {
        if (_api == null)
        {
            var clientWarn = UseService<IClientProvider>();
            clientWarn.Toast("LlmTornado API client is not initialized.", "Not Ready");
            return;
        }

        var userMessage = @event.Value;
        var currentMessages = _messages.Value;
        
        // Add user message
        var messagesWithUser = currentMessages.Add(new ChatMessage(ChatSender.User, userMessage));
        
        // Add loading state
        var messagesWithLoading = messagesWithUser.Add(
            new ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))
        );
        
        _messages.Set(messagesWithLoading);
        
        // Process streaming response
        _ = Task.Run(async () =>
        {
            try
            {
                var conversation = _api.Chat.CreateConversation(_modelName);
                
                // Add current user message
                conversation.AppendUserInput(userMessage);
                
                var builder = new StringBuilder();
                var lastUpdate = DateTime.UtcNow;
                
                // Stream the response
                await conversation.StreamResponse(token =>
                {
                    builder.Append(token);
                    
                    // Update UI every 100ms to reduce flicker
                    if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds > 100)
                    {
                        var updatedMessages = _messages.Value.Take(_messages.Value.Length - 1).ToImmutableArray();
                        _messages.Set(updatedMessages.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
                        lastUpdate = DateTime.UtcNow;
                    }
                });

                // Final update
                var finalMessages = _messages.Value.Take(_messages.Value.Length - 1).ToImmutableArray();
                _messages.Set(finalMessages.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
            }
            catch (Exception ex)
            {
                var errorMessages = _messages.Value;
                if (errorMessages.Length > 0 && errorMessages[errorMessages.Length - 1].Sender == ChatSender.Assistant)
                {
                    errorMessages = errorMessages.Take(errorMessages.Length - 1).ToImmutableArray();
                }
                _messages.Set(errorMessages.Add(
                    new ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")
                ));
            }
        });
    }
}

