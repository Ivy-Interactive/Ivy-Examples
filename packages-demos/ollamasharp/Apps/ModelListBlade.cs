namespace OllamaSharpExample;

public class ModelListBlade : ViewBase
{
    private const string Url = "http://localhost:11434";
    private record ModelListRecord(string Name);

    public class ModelListBlade : ViewBase
    {
        private const string Url = "http://localhost:11434";
        private record ModelListRecord(string Name);

        private OllamaApiClient? _ollamaApiClient;

        public override object? Build()
        {
            var blades = UseContext<IBladeService>();
            var client = UseService<IClientProvider>();
            var models = UseState(ImmutableArray.Create<ModelListRecord>());
            var modelsLoaded = UseState(false);

            async Task OnRefreshClicked()
            {
                _ollamaApiClient?.Dispose();
                _ollamaApiClient = new OllamaApiClient(Url);
                var connected = await _ollamaApiClient.IsRunningAsync();

                if (!connected)
                {
                    client.Toast($"Ollama API is not running at {Url}", "Connection Error");
                    modelsLoaded.Set(false);
                    models.Set(ImmutableArray.Create<ModelListRecord>());
                    return;
                }

                var ollamaModels = await _ollamaApiClient.ListLocalModelsAsync();
                models.Set(ollamaModels.Select(m => new ModelListRecord(m.Name)).ToImmutableArray());
                modelsLoaded.Set(true);

                if (ollamaModels.Any())
                {
                    client.Toast($"Loaded {ollamaModels.Count()} model(s)", "Models Loaded");
                }
                else
                {
                    client.Toast("No models found. Please download a model using 'ollama pull <model-name>'", "No Models");
                }
            }

            // Automatically load models on first render
            UseEffect(async () =>
            {
                if (!modelsLoaded.Value && models.Value.IsEmpty)
                {
                    await OnRefreshClicked();
                }
            }, []);

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

            if (models.Value.IsEmpty && !modelsLoaded.Value)
            {
                return Layout.Vertical().Gap(6).Padding(2)
                    | Text.H3("Models")
                    | Text.Muted("Loading models...");
            }

            if (models.Value.IsEmpty)
            {
                return Layout.Vertical().Gap(6).Padding(2)
                    | Text.H3("Models")
                    | Text.Muted("No models available. Please ensure Ollama is running and models are installed.");
            }

            return new FilteredListView<ModelListRecord>(
                fetchRecords: (filter) =>
                {
                    var filtered = models.Value;
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        filter = filter.Trim();
                        filtered = filtered.Where(m => m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            .ToImmutableArray();
                    }
                    return Task.FromResult(filtered.ToArray());
                },
                createItem: CreateItem,
                onFilterChanged: _ =>
                {
                    blades.Pop(this);
                }
            );
        }
    }
}

