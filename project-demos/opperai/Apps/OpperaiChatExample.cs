using Ivy;
using OpperDotNet;

namespace OpperaiExample.Apps
{
    [App(icon: Icons.MessageCircle, title: "OpperAI Chat")]
    public class OpperaiChatExample : ViewBase
    {
        public override object? Build()
        {
            // API Key state - initialize from environment variable if available
            var apiKey = UseState<string?>(Environment.GetEnvironmentVariable("OPPER_API_KEY"));
            var opperClient = UseState<OpperClient?>(default(OpperClient?));

            // Create or recreate client when API key changes
            UseEffect(() =>
            {
                if (!string.IsNullOrWhiteSpace(apiKey.Value))
                {
                    try
                    {
                        opperClient.Value?.Dispose();
                        opperClient.Set(new OpperClient(apiKey.Value!));
                    }
                    catch
                    {
                        opperClient.Set(default(OpperClient?));
                    }
                }
                else
                {
                    opperClient.Value?.Dispose();
                    opperClient.Set(default(OpperClient?));
                }
            }, [apiKey]);

            var conversationHistory = UseState<List<string>>(new List<string>());
            
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, "Hello! I'm an AI assistant powered by Opper.ai. How can I help you today?")
            ));

            // Reset messages when API key is removed
            UseEffect(() =>
            {
                if (opperClient.Value == null)
                {
                    conversationHistory.Set(new List<string>());
                }
            }, [opperClient]);
            const string DefaultModel = "azure/gpt-4o-eu";
            const string DefaultModelName = "azure/gpt-4o-eu";
            
            // Extract model name from environment variable or use default
            var envModel = Environment.GetEnvironmentVariable("OPPER_MODEL");
            var initialModel = !string.IsNullOrWhiteSpace(envModel) 
                ? (envModel.Contains('/') ? envModel.Split('/').Last() : envModel)
                : DefaultModelName;
            var selectedModel = UseState<string?>(initialModel);

            // Query models asynchronously from API
            async Task<Option<string>[]> QueryModels(string query)
            {
                if (opperClient.Value == null)
                    return Array.Empty<Option<string>>();

                try
                {
                    var response = await opperClient.Value.ListModelsAsync(limit: 100);
                    var models = response.Data
                        .Where(m => string.IsNullOrWhiteSpace(query) ||
                                   m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   m.HostingProvider.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(m => m.HostingProvider)
                        .ThenBy(m => m.Name)
                        .Select(m => new Option<string>($"{m.HostingProvider}/{m.Name}", m.Name))
                        .ToArray();

                    // Add default model option if query is empty
                    if (string.IsNullOrWhiteSpace(query) && models.Length > 0)
                    {
                        var defaultModel = models.FirstOrDefault(m => string.Equals(m.Value as string, DefaultModelName, StringComparison.Ordinal));
                        if (defaultModel != null)
                        {
                            return new[] { defaultModel }
                                .Concat(models.Where(m => !string.Equals(m.Value as string, DefaultModelName, StringComparison.Ordinal)))
                                .ToArray();
                        }
                    }

                    return models;
                }
                catch
                {
                    return Array.Empty<Option<string>>();
                }
            }

            // Lookup model by name
            async Task<Option<string>?> LookupModel(string? modelName)
            {
                if (opperClient.Value == null)
                    return null;

                if (string.IsNullOrWhiteSpace(modelName))
                    modelName = DefaultModelName;

                try
                {
                    var response = await opperClient.Value.ListModelsAsync(limit: 100);
                    var model = response.Data.FirstOrDefault(m => m.Name == modelName);
                    if (model != null)
                    {
                        return new Option<string>($"{model.HostingProvider}/{model.Name}", model.Name);
                    }
                    
                    // If model not found, return default model option anyway
                    if (modelName == DefaultModelName)
                    {
                        var parts = DefaultModel.Split('/');
                        var defaultModelFromApi = response.Data.FirstOrDefault(m => 
                            m.HostingProvider == parts[0] && m.Name == parts[1]);
                        if (defaultModelFromApi != null)
                        {
                            return new Option<string>($"{defaultModelFromApi.HostingProvider}/{defaultModelFromApi.Name}", defaultModelFromApi.Name);
                        }
                        // Fallback: return default model even if not in API response
                        return new Option<string>($"{DefaultModel} - Default model", DefaultModelName);
                    }
                    
                    return null;
                }
                catch
                {
                    // On error, still return default model option
                    if (modelName == DefaultModelName)
                    {
                        return new Option<string>($"{DefaultModel} - Default model", DefaultModelName);
                    }
                    return null;
                }
            }

            async void HandleMessageAsync(Event<Chat, string> @event)
            {
                if (opperClient.Value == null)
                {
                    messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));
                    messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.Assistant, 
                        "Please enter your Opper.ai API key in the field above to start chatting. " +
                        "You can get your API key at https://platform.opper.ai/settings/api-keys")));
                    return;
                }

                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

                var history = conversationHistory.Value;
                history.Add($"User: {@event.Value}");

                var currentMessages = messages.Value;
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

                try
                {
                    var contextualInput = string.Join("\n", history) + "\n";
                    var response = await opperClient.Value.CallAsync(new OpperCallRequest
                    {
                        Name = "chat",
                        Instructions = "You are a helpful AI assistant. Respond to the user's message in a friendly and informative way. Keep your responses concise and relevant.",
                        Input = contextualInput + $"User: {@event.Value}",
                        Model = selectedModel.Value ?? DefaultModelName
                    });

                    history.Add($"Assistant: {response.Message}");
                    conversationHistory.Set(history);
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, response.Message)));
                }
                catch (OpperException ex)
                {
                    var errorMsg = $"Opper API Error: {ex.Message}";
                    if (ex.StatusCode.HasValue) errorMsg += $" (Status: {ex.StatusCode})";
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, errorMsg)));
                }
                catch (Exception ex)
                {
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")));
                }
            }

            // Check if API key is set
            var hasApiKey = !string.IsNullOrWhiteSpace(apiKey.Value) && opperClient.Value != null;

            // Header card: Title (left) | Model Selection (center)
            var headerCard =
            Layout.Vertical()
                | (Layout.Horizontal()
                | (Layout.Vertical()
                | Text.H4("OpperAI Chat")).Width(Size.Fraction(0.2f))

                | (Layout.Vertical().Margin(3, 3, 0, 0)
                    | selectedModel.ToAsyncSelectInput(QueryModels, LookupModel, placeholder: "Search and select model...")
                    ).Width(Size.Fraction(0.4f))
                | (Layout.Vertical().Margin(3, 3, 0, 0)
                    | apiKey.ToPasswordInput(placeholder: "Enter your Opper.ai API key...")
                    ).Width(Size.Fraction(0.4f))
                );

            // Chat card - show instruction if no API key, otherwise show chat
            var chatCard = new Card(
                hasApiKey
                    ? new Chat(messages.Value.ToArray(), HandleMessageAsync) as object
                    : Layout.Vertical().Gap(3).Padding(4)
                        | Text.H4("Welcome to OpperAI Chat!")
                        | Text.Muted("To get started, you need an API key from Opper.ai:")
                        | (Layout.Vertical().Gap(1).Padding(4)
                            | Text.Markdown("1 Visit [https://platform.opper.ai](https://platform.opper.ai)")
                            | Text.Markdown("2 Sign up or log in to your [account](https://platform.opper.ai/settings/details)")
                            | Text.Markdown("3 Go to [Settings → API Keys](https://platform.opper.ai/settings/api-keys)")
                            | Text.Markdown("4 Create a new [API key](https://platform.opper.ai/settings/api-keys/create)")
                            | Text.Markdown("5 Copy your API key and paste it in the field above"))

                        | Text.Muted("Once you enter your API key, you'll be able to chat with AI models!")
            );

            return Layout.Horizontal()
            | (Layout.Vertical().Gap(2).Align(Align.TopCenter)
                | headerCard.Width(Size.Fraction(0.6f)).Height(Size.Fit().Min(Size.Fraction(0.1f)))
                | chatCard.Width(Size.Fraction(0.6f)).Height(Size.Fit().Min(Size.Fraction(0.9f)))
                );
        }
    }
}

