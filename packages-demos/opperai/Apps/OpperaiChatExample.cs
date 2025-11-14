using Ivy;
using OpperDotNet;

namespace OpperaiExample.Apps
{
    [App(icon: Icons.MessageCircle, title: "Opper.ai Chat Demo")]
    public class OpperaiChatExample : ViewBase
    {
        private readonly OpperClient _opperClient;

        public OpperaiChatExample()
        {
            var opperApiKey = Environment.GetEnvironmentVariable("OPPER_API_KEY");
            if (string.IsNullOrWhiteSpace(opperApiKey))
                throw new InvalidOperationException("OPPER_API_KEY environment variable is not set.");

            _opperClient = new OpperClient(opperApiKey);
        }

        public override object? Build()
        {
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, "Hello! I'm an AI assistant powered by Opper.ai. How can I help you today?")
            ));

            var conversationHistory = UseState<List<string>>(new List<string>());
            const string DefaultModel = "azure/gpt-4o-eu";
            var selectedModel = UseState<string?>(Environment.GetEnvironmentVariable("OPPER_MODEL") ?? DefaultModel);

            // Query models asynchronously from API
            async Task<Option<string>[]> QueryModels(string query)
            {
                try
                {
                    var response = await _opperClient.ListModelsAsync(limit: 100);
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
                        var defaultModel = models.FirstOrDefault(m => m.Value == DefaultModel);
                        if (defaultModel != null)
                        {
                            return new[] { defaultModel }
                                .Concat(models.Where(m => m.Value != DefaultModel))
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
                if (string.IsNullOrWhiteSpace(modelName))
                    modelName = DefaultModel;

                try
                {
                    var response = await _opperClient.ListModelsAsync(limit: 100);
                    var model = response.Data.FirstOrDefault(m => m.Name == modelName);
                    return model != null
                        ? new Option<string>($"{model.HostingProvider}/{model.Name}", model.Name)
                        : null;
                }
                catch
                {
                    return null;
                }
            }

            async void HandleMessageAsync(Event<Chat, string> @event)
            {
                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

                var history = conversationHistory.Value;
                history.Add($"User: {@event.Value}");

                var currentMessages = messages.Value;
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

                try
                {
                    var contextualInput = string.Join("\n", history) + "\n";
                    var response = await _opperClient.CallAsync(new OpperCallRequest
                    {
                        Name = "chat",
                        Instructions = "You are a helpful AI assistant. Respond to the user's message in a friendly and informative way. Keep your responses concise and relevant.",
                        Input = contextualInput + $"User: {@event.Value}",
                        Model = selectedModel.Value ?? DefaultModel
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

            // Header card with model selection
            var headerCard = new Card(
                Layout.Vertical().Gap(2)
                | Text.H3("Opper.ai Chat")
                | selectedModel.ToAsyncSelectInput(QueryModels, LookupModel, placeholder: "Search and select model...")
            );

            // Chat card
            var chatCard = new Card(
                new Chat(messages.Value.ToArray(), HandleMessageAsync)
            );

            return Layout.Vertical()
            | (Layout.Vertical().Gap(2).Align(Align.TopCenter)
                | headerCard.Width(Size.Fraction(0.6f))
                | chatCard.Width(Size.Fraction(0.6f))
                );
        }
    }
}

