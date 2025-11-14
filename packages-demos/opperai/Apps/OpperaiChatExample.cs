using Ivy;
using OpperDotNet;

namespace OpperaiExample.Apps
{
    [App(icon: Icons.MessageCircle, title: "Opper.ai Chat Demo")]
    public class OpperaiChatExample : ViewBase
    {
        private readonly OpperClient _opperClient;
        private readonly string _defaultModel;

        public OpperaiChatExample()
        {
            // Retrieve Opper API key from environment variable
            var opperApiKey = Environment.GetEnvironmentVariable("OPPER_API_KEY");
            if (string.IsNullOrWhiteSpace(opperApiKey))
                throw new InvalidOperationException("OPPER_API_KEY environment variable is not set.");

            // You can specify a model or leave it null to use Opper's default
            _defaultModel = Environment.GetEnvironmentVariable("OPPER_MODEL") ?? string.Empty;

            // Initialize Opper client
            _opperClient = new OpperClient(opperApiKey);
        }

        public override object? Build()
        {
            // Initialize chat messages with a greeting from the assistant
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, "Hello! I'm an AI assistant powered by Opper.ai. How can I help you today?")
            ));

            // Keep conversation history for context
            var conversationHistory = UseState<List<string>>(new List<string>());

            async void HandleMessageAsync(Event<Chat, string> @event)
            {
                // Add user message to chat
                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

                // Add to conversation history
                var history = conversationHistory.Value;
                history.Add($"User: {@event.Value}");

                // Add assistant "Thinking..." status
                var currentMessages = messages.Value;
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

                try
                {
                    // Build context from conversation history
                    var contextualInput = string.Join("\n", history) + "\n";

                    // Create Opper API request
                    var request = new OpperCallRequest
                    {
                        Name = "chat",
                        Instructions = "You are a helpful AI assistant. Respond to the user's message in a friendly and informative way. Keep your responses concise and relevant.",
                        Input = contextualInput + $"User: {@event.Value}",
                    };

                    // Only set model if it's specified
                    if (!string.IsNullOrWhiteSpace(_defaultModel))
                    {
                        request.Model = _defaultModel;
                    }

                    // Call Opper API
                    var response = await _opperClient.CallAsync(request);
                    string aiResponse = response.Message;

                    // Add to conversation history
                    history.Add($"Assistant: {aiResponse}");
                    conversationHistory.Set(history);

                    // Replace "Thinking..." with actual response
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, aiResponse)));
                }
                catch (OpperException ex)
                {
                    var errorMessage = $"Opper API Error: {ex.Message}";
                    if (ex.StatusCode.HasValue)
                        errorMessage += $" (Status: {ex.StatusCode})";
                    
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, errorMessage)));
                }
                catch (Exception ex)
                {
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, $"Error: {ex.Message}")));
                }
            }

            return Layout.Center().Padding(0, 10, 0, 10)
            | new Chat(messages.Value.ToArray(), HandleMessageAsync).Width(Size.Full().Max(200));
        }
    }
}

