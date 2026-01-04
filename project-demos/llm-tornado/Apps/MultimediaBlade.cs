namespace LlmTornadoExample.Apps;

public class MultimediaBlade : ViewBase
{
    private readonly string _ollamaUrl;
    private readonly string _modelName;
    
    private IState<ImmutableArray<ChatMessage>> _messages;
    private IState<string> _imageUrl;
    private TornadoApi? _api;

    public MultimediaBlade(string ollamaUrl, string modelName)
    {
        _ollamaUrl = ollamaUrl;
        _modelName = modelName;
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        
        _messages = UseState(ImmutableArray.Create<ChatMessage>(
            new ChatMessage(ChatSender.Assistant, 
                "Hello! I'm a multimodal assistant. I can analyze images you provide.\n\n" +
                "**Note:** This requires a vision-capable model like:\n" +
                "• llava (recommended)\n" +
                "• llama3.2-vision\n" +
                "• bakllava\n\n" +
                "Paste an image URL above and ask me about it!")
        ));

        _imageUrl = UseState("https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Cat03.jpg/481px-Cat03.jpg");

        // Initialize TornadoApi client
        UseEffect(async () =>
        {
            if (_api == null)
            {
                try
                {
                    _api = new TornadoApi(new Uri(_ollamaUrl));
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
                    | new Icon(Icons.Image).Size(8))
                | Text.H4($"Multimedia - {_modelName}"),
            Layout.Vertical().Width(Size.Units(200).Max(Size.Units(400)))
                | new Card(
                    Layout.Vertical().Gap(3).Padding(3)
                    | Text.H4("Multimodal Chat with Image Analysis")
                    | Layout.Vertical().Gap(2)
                        | (Layout.Vertical().Gap(1)
                            | Text.Small("Image URL").Bold()
                            | _imageUrl.ToTextInput(placeholder: "https://example.com/image.jpg"))
                        | Layout.Horizontal().Gap(2)
                            | new Button("Load Sample Cat")
                                .Variant(ButtonVariant.Secondary)
                                .HandleClick(_ => _imageUrl.Set("https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Cat03.jpg/481px-Cat03.jpg"))
                            | new Button("Load Sample Dog")
                                .Variant(ButtonVariant.Secondary)
                                .HandleClick(_ => _imageUrl.Set("https://upload.wikimedia.org/wikipedia/commons/thumb/6/6e/Golde33443.jpg/280px-Golde33443.jpg"))
                    | (_imageUrl.Value != "" 
                        ? Layout.Center()
                            | new Image(_imageUrl.Value)
                                .Width(Size.Units(80))
                                .Height(Size.Units(60))
                        : null)
                )
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
        var imageUrl = _imageUrl.Value;
        
        // Add user message
        var messagesWithUser = currentMessages.Add(new ChatMessage(ChatSender.User, userMessage));
        
        // Add loading state
        var messagesWithLoading = messagesWithUser.Add(
            new ChatMessage(ChatSender.Assistant, new ChatStatus("Analyzing..."))
        );
        
        _messages.Set(messagesWithLoading);
        
        // Process with image
        _ = Task.Run(async () =>
        {
            try
            {
                var conversation = _api.Chat.CreateConversation(_modelName);

                // For this simple demo, we'll just include the image URL in the prompt
                var prompt = userMessage;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    prompt = $"[Image URL: {imageUrl}]\n\n{userMessage}\n\nNote: This is a simplified demo. For full multimodal support, use vision-capable models like llava.";
                }
                
                conversation.AppendUserInput(prompt);

                var builder = new StringBuilder();
                var lastUpdate = DateTime.UtcNow;

                // Stream the response
                await conversation.StreamResponse(token =>
                {
                    builder.Append(token);
                    
                    // Update UI periodically
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
                
                var errorText = $"Error: {ex.Message}\n\n" +
                    "**Troubleshooting:**\n" +
                    "• Make sure you're using a vision-capable model (e.g., `llava`, `llama3.2-vision`)\n" +
                    "• Download with: `ollama pull llava`\n" +
                    "• Check that the image URL is accessible";
                
                _messages.Set(errorMessages.Add(
                    new ChatMessage(ChatSender.Assistant, errorText)
                ));
            }
        });
    }
}

