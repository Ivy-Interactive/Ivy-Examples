namespace LlmTornadoExample.Apps;

[App(icon: Icons.Sparkles, title: "LlmTornado Examples")]
public class LlmTornadoApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new MainMenuBlade(), "Examples");
    }
}

public class MainMenuBlade : ViewBase
{
    public override object? Build()
    {
        var blades = UseContext<IBladeController>();
        var client = UseService<IClientProvider>();
        var ollamaUrl = UseState("http://localhost:11434");
        var selectedModel = UseState<string?>(() => null);
        var availableModels = UseState<ImmutableArray<string>>(ImmutableArray<string>.Empty);
        var isLoadingModels = UseState(false);
        
        // Load models from Ollama API
        async Task LoadModels()
        {
            if (string.IsNullOrWhiteSpace(ollamaUrl.Value)) return;
            
            isLoadingModels.Set(true);
            try
            {
                var url = $"{ollamaUrl.Value.TrimEnd('/')}/api/tags";
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("models", out var modelsElement) && modelsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var modelNames = modelsElement.EnumerateArray()
                            .Select(m => m.TryGetProperty("name", out var name) ? name.GetString() : null)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Cast<string>()
                            .ToImmutableArray();
                        
                        availableModels.Set(modelNames);
                        
                        // Auto-select first model if no model is selected
                        if (modelNames.Length > 0 && (selectedModel.Value == null || !modelNames.Contains(selectedModel.Value)))
                        {
                            selectedModel.Set(modelNames[0]);
                        }
                    }
                    else
                    {
                        // If JSON structure is not as expected
                        availableModels.Set(ImmutableArray<string>.Empty);
                    }
                }
                else
                {
                    availableModels.Set(ImmutableArray<string>.Empty);
                    var errorText = await response.Content.ReadAsStringAsync();
                    client.Toast($"Failed to load models: {response.StatusCode}", "Error");
                }
            }
            catch (TaskCanceledException)
            {
                availableModels.Set(ImmutableArray<string>.Empty);
                client.Toast("Connection timeout to Ollama. Please check if Ollama is running.", "Error");
            }
            catch (HttpRequestException ex)
            {
                availableModels.Set(ImmutableArray<string>.Empty);
                client.Toast($"Connection error to Ollama: {ex.Message}", "Error");
            }
            catch (Exception ex)
            {
                availableModels.Set(ImmutableArray<string>.Empty);
                client.Toast($"Error loading models: {ex.Message}", "Error");
            }
            finally
            {
                isLoadingModels.Set(false);
            }
        }
        
        // Load models on initialization and when URL changes
        UseEffect(async () => await LoadModels(), EffectTrigger.AfterInit());
        UseEffect(async () => await LoadModels(), [ollamaUrl]);

        return BladeHelper.WithHeader(
            Text.H4("LlmTornado Examples"),
            Layout.Vertical()
                | new Card(
                    Layout.Vertical().Gap(3)
                    | Text.H3("Getting Started")
                    | Text.Muted("Follow these steps to get started:")
                    | Text.Markdown("**1. Download Ollama** from [https://ollama.com/download](https://ollama.com/download)")
                    | Text.Markdown("**2. Download Models** for example:")
                    | new Code("ollama pull llama2")
                        .ShowCopyButton()
                   | Text.Markdown("**3. Configuration:**")
                   | Layout.Horizontal()
                       | (Layout.Vertical().Width(Size.Full())
                           | ollamaUrl.ToTextInput(placeholder: "http://localhost:11434")
                            .WithField()
                            .Label("Ollama URL"))
                       | (Layout.Vertical().Gap(3).Width(Size.Full())
                           | Text.Small("Model")
                           | (isLoadingModels.Value
                               ? selectedModel.ToSelectInput(Array.Empty<Option<string>>(), placeholder: "Loading...").Disabled(true) as object
                               : selectedModel.ToSelectInput(availableModels.Value.Select(m => new Option<string>(m)).ToArray(), placeholder: "Select model...") as object))
                   | new Separator()
                   | Text.Markdown("This demo uses LlmTornado library for interacting with LLM models.")
                   | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [LlmTornado](https://llmtornado.ai)")
                )
                 | Layout.Grid().Gap(3)
                     | new Card(
                         Layout.Horizontal().Gap(3)
                         | (Layout.Vertical().Gap(2).Align(Align.Center).Width(Size.Fit())
                            | new Icon(Icons.MessageSquare).Size(16))
                         | (Layout.Vertical().Gap(2)
                             | Text.Large("Simple Chat")
                             | Text.Small("Basic conversation with streaming responses").Muted()
                             | new Button("Try It")
                                 .Variant(ButtonVariant.Primary)
                                 .Disabled(selectedModel.Value == null)
                                 .HandleClick(_ => blades.Push(this, new SimpleChatBlade(ollamaUrl.Value, selectedModel.Value ?? "llama3.2:1b"), "Simple Chat")))
                     )
                    | new Card(
                        Layout.Horizontal().Gap(3)
                        | (Layout.Vertical().Gap(2).Align(Align.Center).Width(Size.Fit())
                            | new Icon(Icons.Bot).Size(16))
                        | (Layout.Vertical().Gap(2)
                            | Text.Large("Agent with Tools")
                            | Text.Small("Agent with function calling capabilities").Muted()
                            | new Button("Try It")
                                .Variant(ButtonVariant.Primary)
                                .Disabled(selectedModel.Value == null)
                                .HandleClick(_ => blades.Push(this, new AgentChatBlade(ollamaUrl.Value, selectedModel.Value ?? "llama3.2:1b"), "Agent Chat")))
                    )
        );
    }
}

