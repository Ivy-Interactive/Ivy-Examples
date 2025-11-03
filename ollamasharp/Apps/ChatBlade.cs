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
        var blades = UseContext<IBladeController>();
        var client = UseService<IClientProvider>();
        
        _messages = UseState(ImmutableArray.Create<ChatMessage>());

        // Ініціалізуємо клієнт при першому рендері
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

    private async ValueTask OnSendMessage(Event<Chat, string> @event)
    {
        if (_ollamaApiClient == null)
        {
            var clientWarn = UseService<IClientProvider>();
            clientWarn.Toast("Ollama API client is not initialized.", "Not Ready");
            return;
        }

        _messages.Set(_messages.Value.Add(new ChatMessage(ChatSender.User, @event.Value)));
        _ollamaApiClient.SelectedModel = _modelName;
        
        var chat = new OllamaSharp.Chat(_ollamaApiClient, @event.Value);
        var builder = new StringBuilder();
        
        await foreach (var answerToken in chat.SendAsync(@event.Value))
        {
            builder.Append(answerToken);
        }

        _messages.Set(_messages.Value.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
    }
}

