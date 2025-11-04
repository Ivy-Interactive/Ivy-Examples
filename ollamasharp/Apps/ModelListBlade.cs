namespace OllamaSharpExample;

public class ModelListBlade : ViewBase
{
    private const string Url = "http://localhost:11434";
    private record ModelListRecord(string Name);
    
    private IState<ImmutableArray<ModelListRecord>> _models;
    private IState<bool> _modelsLoaded;
    private OllamaApiClient? _ollamaApiClient;

    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        
        _models = UseState(ImmutableArray.Create<ModelListRecord>());
        _modelsLoaded = UseState(false);

        // Automatically load models on first render
        UseEffect(async () =>
        {
            if (!_modelsLoaded.Value && _models.Value.IsEmpty)
            {
                await OnRefreshClicked();
            }
        }, EffectTrigger.AfterInit());

        var onItemClicked = new Action<Event<ListItem>>(e =>
        {
            var model = (ModelListRecord)e.Sender.Tag!;
            blades.Push(this, new ChatBlade(model.Name), model.Name);
        });

        ListItem CreateItem(ModelListRecord record)
        {
            var item = new ListItem(title: record.Name, subtitle: "Ollama Model", onClick: onItemClicked, tag: record);
            return item;
        }

        if (_models.Value.IsEmpty && !_modelsLoaded.Value)
        {
            return Layout.Vertical().Gap(6).Padding(2)
                | Text.H3("Models")
                | Text.Muted("Loading models...");
        }

        if (_models.Value.IsEmpty)
        {
            return Layout.Vertical().Gap(6).Padding(2)
                | Text.H3("Models")
                | Text.Muted("No models available. Please ensure Ollama is running and models are installed.");
        }

        return new FilteredListView<ModelListRecord>(
            fetchRecords: (filter) => FetchModels(filter),
            createItem: CreateItem,
            onFilterChanged: _ =>
            {
                blades.Pop(this);
            }
        );
    }

    private Task<ModelListRecord[]> FetchModels(string filter)
    {
        var models = _models.Value;
        
        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim();
            models = models.Where(m => m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToImmutableArray();
        }

        return Task.FromResult(models.ToArray());
    }

    private async ValueTask OnRefreshClicked()
    {
        var client = UseService<IClientProvider>();
        
        _ollamaApiClient?.Dispose();
        _ollamaApiClient = new OllamaApiClient(Url);
        var connected = await _ollamaApiClient.IsRunningAsync();
        
        if (!connected)
        {
            client.Toast($"Ollama API is not running at {Url}", "Connection Error");
            _modelsLoaded.Set(false);
            _models.Set(ImmutableArray.Create<ModelListRecord>());
            return;
        }

        var ollamaModels = await _ollamaApiClient.ListLocalModelsAsync();
        _models.Set(ollamaModels.Select(m => new ModelListRecord(m.Name)).ToImmutableArray());
        _modelsLoaded.Set(true);
        
        if (ollamaModels.Any())
        {
            client.Toast($"Loaded {ollamaModels.Count()} model(s)", "Models Loaded");
        }
        else
        {
            client.Toast("No models found. Please download a model using 'ollama pull <model-name>'", "No Models");
        }
    }
}

