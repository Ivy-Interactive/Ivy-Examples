namespace OllamaSharpExample;

[App(icon: Icons.TextQuote, title: "OllamaSharp")]
public class OllamaSharpApp : ViewBase
{
    private const string Url = "http://localhost:11434";
    private IState<ImmutableArray<ChatMessage>> _messages;
    private IState<ImmutableArray<ModelListItem>> _models;
    private IState<ModelListItem?> _selectedModel;
    private OllamaApiClient? _ollamaApiClient;

    public override object? Build()
    {
        _messages = UseState(ImmutableArray.Create<ChatMessage>());

        _models = UseState(ImmutableArray.Create<ModelListItem>);
        _selectedModel = UseState<ModelListItem?>();
        
        if (_selectedModel.Value == null)
            return Layout.Center()
                   | (new Card(
                           Layout.Vertical().Gap(6).Padding(2)
                           | new Button("Refresh Models", onClick: _ => OnRefreshClicked())
                           | Layout.Vertical(_models.Value)
                       )
                   )
                   .Width(Size.Units(120).Max(500));

        return Layout.Vertical().Padding(0, 10, 0, 10)
               | Text.H4($"Chatting with model: {_selectedModel.Value?.ModelName}")
               | new Button("Back to Models", onClick: _ =>
               {
                   ClearModelSelection();
               })
               | new Chat(_messages.Value.ToArray(), OnSendMessage).Width(Size.Full().Max(200));
    }

    private async ValueTask OnSendMessage(Event<Chat, string> @event)
    {
        if (_ollamaApiClient == null)
        {
            var clientWarn = UseService<IClientProvider>();
            clientWarn.Toast("Click 'Refresh Models' first to initialize the API client.", "Not Ready");
            return;
        }
        if (_selectedModel.Value == null)
        {
            var clientWarn = UseService<IClientProvider>();
            clientWarn.Toast("Select a model before chatting.", "Model Required");
            return;
        }
        _messages.Set(_messages.Value.Add(new ChatMessage(ChatSender.User, @event.Value)));
        _ollamaApiClient.SelectedModel = _selectedModel.Value.ModelName;
        var chat = new OllamaSharp.Chat(_ollamaApiClient, @event.Value);
        var builder = new StringBuilder();
        await foreach (var answerToken in chat.SendAsync(@event.Value))
        {
            builder.Append(answerToken);
        }

        _messages.Set(_messages.Value.Add(new ChatMessage(ChatSender.Assistant, builder.ToString())));
    }

    private async ValueTask OnRefreshClicked()
    {
        _ollamaApiClient?.Dispose();
        _ollamaApiClient = new OllamaApiClient(Url);
        var connected = await _ollamaApiClient.IsRunningAsync();
        if (!connected)
        {
            var client = UseService<IClientProvider>();
            client.Toast($"Ollama API is not running at {Url}", "Connection Error");
            return;
        }

        var ollamaModels = await _ollamaApiClient.ListLocalModelsAsync();
        _models.Set(ollamaModels.Select(m => new ModelListItem(s => { _selectedModel.Set(s); }, m.Name))
            .ToImmutableArray());
        ClearModelSelection();
    }

    private void ClearModelSelection()
    {
        _selectedModel.Set((ModelListItem?)null);
        _messages.Set([]);
    }
}

public class ModelListItem(Action<ModelListItem> modelSelect, string modelName) : ViewBase
{
    public string ModelName { get; } = modelName;

    public override object? Build()
    {
        return Layout.Vertical(
            Layout.Horizontal(
                new Button(ModelName, _ => { modelSelect(this); }).Icon(Icons.NotebookText)
                    .Variant(ButtonVariant.Outline)
            ).Align(Align.Left).Width(Size.Units(120).Max(500)),
            new Separator()
        );
    }
}