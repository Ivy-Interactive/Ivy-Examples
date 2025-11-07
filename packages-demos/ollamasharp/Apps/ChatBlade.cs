namespace OllamaSharpExample;

public class ChatBlade : ViewBase
{
    private const string Url = "http://localhost:11434";
    private readonly string _modelName;
    
    private IState<ImmutableArray<ChatMessage>> _messages;
    private OllamaApiClient? _ollamaApiClient;

    public ChatBlade(string modelName)
    {
        _modelName = modelName;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        _messages = UseState(ImmutableArray.Create<ChatMessage>(
            new ChatMessage(ChatSender.Assistant, "Hello! How are you? How can I help you today?")
        ));

        // Initialize client on first render
        UseEffect(async () =>
        {
            if (_ollamaApiClient == null)
            {
                _ollamaApiClient = new OllamaApiClient(Url);
                var connected = await _ollamaApiClient.IsRunningAsync();
                if (!connected)
                {
                    client.Toast($"Ollama API is not running at {Url}", "Connection Error");
                    _ollamaApiClient?.Dispose();
                    _ollamaApiClient = null;
                }
            }
        }, EffectTrigger.AfterInit());

        return BladeHelper.WithHeader(
            Text.H4(_modelName),
            Layout.Vertical().Width(Size.Units(200).Max(Size.Units(350)))
                | new Chat(_messages.Value.ToArray(), OnSendMessage)
        );
    }

    private void OnSendMessage(Event<Chat, string> @event)
    {
        if (_ollamaApiClient == null)
        {
            var clientWarn = UseService<IClientProvider>();
            clientWarn.Toast("Ollama API client is not initialized.", "Not Ready");
            return;
        }

        var currentMessages = _messages.Value;
        
        // Add user message immediately
        var messagesWithUser = currentMessages.Add(new ChatMessage(ChatSender.User, @event.Value));
        
        // Add loading state immediately after user message
        var messagesWithLoading = messagesWithUser.Add(new ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking...")));
        
        // Update UI with user message and loading state
        _messages.Set(messagesWithLoading);
        
        // Process the request asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                _ollamaApiClient.SelectedModel = _modelName;
                
                var chat = new OllamaSharp.Chat(_ollamaApiClient, @event.Value);
                var builder = new StringBuilder();
                
                await foreach (var answerToken in chat.SendAsync(@event.Value))
                {
                    builder.Append(answerToken);
                }

                // Remove loading message and add actual response
                var updatedMessages = _messages.Value.Take(_messages.Value.Length - 1).ToImmutableArray();
                _messages.Set(updatedMessages.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                var errorMessages = _messages.Value;
                // Remove loading if it exists (last message from assistant)
                if (errorMessages.Length > 0 && errorMessages[errorMessages.Length - 1].Sender == ChatSender.Assistant)
                {
                    errorMessages = errorMessages.Take(errorMessages.Length - 1).ToImmutableArray();
                }
                _messages.Set(errorMessages.Add(new ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")));
            }
        });
    }
}

