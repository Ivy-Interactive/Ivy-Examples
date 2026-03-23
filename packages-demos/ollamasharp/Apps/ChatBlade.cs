namespace OllamaSharpExample;

public class ChatBlade(string modelName) : ViewBase
{
    private const string Url = "http://localhost:11434";
    private readonly string _modelName = modelName;
    private OllamaApiClient? _ollamaApiClient;

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var messages = UseState(ImmutableArray.Create<ChatMessage>(
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
        }, []);

        void OnSendMessage(Event<Chat, string> @event)
        {
            if (_ollamaApiClient == null)
            {
                client.Toast("Ollama API client is not initialized.", "Not Ready");
                return;
            }

            var currentMessages = messages.Value;

            // Add user message immediately
            var messagesWithUser = currentMessages.Add(new ChatMessage(ChatSender.User, @event.Value));

            // Add loading state immediately after user message
            var messagesWithLoading = messagesWithUser.Add(new ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking...")));

            // Update UI with user message and loading state
            messages.Set(messagesWithLoading);

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
                    var updatedMessages = messages.Value.Take(messages.Value.Length - 1).ToImmutableArray();
                    messages.Set(updatedMessages.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
                }
                catch (Exception ex)
                {
                    // Handle errors gracefully
                    var errorMessages = messages.Value;
                    // Remove loading if it exists (last message from assistant)
                    if (errorMessages.Length > 0 && errorMessages[errorMessages.Length - 1].Sender == ChatSender.Assistant)
                    {
                        errorMessages = errorMessages.Take(errorMessages.Length - 1).ToImmutableArray();
                    }
                    messages.Set(errorMessages.Add(new ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")));
                }
            });
        }

        var chatContent = Layout.Vertical().Width(Size.Units(200).Max(Size.Units(350)))
                | new Chat(messages.Value.ToArray(), OnSendMessage);

        return new Fragment()
               | new BladeHeader(Text.H4(_modelName))
               | chatContent;
    }
}

