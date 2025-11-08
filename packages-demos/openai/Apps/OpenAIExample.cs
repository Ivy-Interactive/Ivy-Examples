using Ivy;
using OpenAI.Chat;

namespace OpenAIExample.Apps
{
    [App(icon: Icons.MessageCircle, title: "OpenAI Chat Demo")]
    public class OpenAIExample : ViewBase
    {
        private readonly ChatClient _aiClient;

        public OpenAIExample()
        {
            // Retrieve OpenAI API key from environment variable
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(openAiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            const string Model = "gpt-4o-mini"; // recommended lightweight model

            // Initialize OpenAI ChatClient
            _aiClient = new ChatClient(Model, openAiKey);
        }

        public override object? Build()
        {
            // Initialize chat messages with a greeting from the assistant
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, "Hello! I'm an OpenAI bot. How can I assist you!")
            ));

            async void HandleMessageAsync(Event<Chat, string> @event)
            {
                // Add user message
                messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));

                // Add assistant "Thinking..." status
                var currentMessages = messages.Value;
                messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))));

                try
                {
                    // Call OpenAI API
                    ChatCompletion completion = await _aiClient.CompleteChatAsync(@event.Value);
                    string aiResponse = completion.Content[0].Text;

                    // Replace "Thinking..." with actual response
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, aiResponse)));
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