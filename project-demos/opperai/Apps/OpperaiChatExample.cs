using Ivy;
using OpperDotNet;

namespace OpperaiExample.Apps
{
    public record SettingsRequest
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Instructions { get; set; } = "You are a helpful AI assistant. When responding:\n\n1. Use Markdown formatting for better readability (headers, lists, code blocks, etc.)\n2. For mathematical expressions, use LaTeX notation with proper delimiters:\n   - Inline math: $expression$ (e.g., $\\sqrt{-1}$ or $x^2 + y^2 = r^2$)\n   - Block math: $$expression$$ for displayed equations\n   - Examples: $\\sqrt{-1} = i$, $E = mc^2$, $\\int_0^\\infty e^{-x^2} dx = \\frac{\\sqrt{\\pi}}{2}$\n3. Ensure all math expressions render clearly and correctly\n4. Keep your responses concise, relevant, and well-formatted.";
    }

    [App(icon: Icons.MessageCircle, title: "OpperAI Chat")]
    public class OpperaiChatExample : ViewBase
    {
        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            
            // API Key state - initialize from environment variable if available
            var apiKey = UseState<string?>(Environment.GetEnvironmentVariable("OPPER_API_KEY"));
            var opperClient = UseState<OpperClient?>(default(OpperClient?));
            var isValidating = UseState<bool>(false);
            var customInstructions = UseState<string>("You are a helpful AI assistant. When responding:\n\n1. Use Markdown formatting for better readability (headers, lists, code blocks, etc.)\n2. For mathematical expressions, use LaTeX notation with proper delimiters:\n   - Inline math: $expression$ (e.g., $\\sqrt{-1}$ or $x^2 + y^2 = r^2$)\n   - Block math: $$expression$$ for displayed equations\n   - Examples: $\\sqrt{-1} = i$, $E = mc^2$, $\\int_0^\\infty e^{-x^2} dx = \\frac{\\sqrt{\\pi}}{2}$\n3. Ensure all math expressions render clearly and correctly\n4. Keep your responses concise, relevant, and well-formatted.");
            var isSettingsDialogOpen = UseState(false);
            var settingsForm = UseState(new SettingsRequest 
            { 
                ApiKey = apiKey.Value ?? string.Empty,
                Instructions = customInstructions.Value
            });
            var conversationHistory = UseState<List<string>>(new List<string>());
            var messages = UseState(ImmutableArray.Create<Ivy.ChatMessage>(
                new Ivy.ChatMessage(ChatSender.Assistant, "Hello! I'm an AI assistant powered by Opper.ai. How can I help you today?")
            ));
            var selectedModel = UseState<string>("aws/claude-3.5-sonnet-eu");

            // Create or recreate client when API key changes
            UseEffect(() =>
            {
                // Validate API key when it changes
                _ = ValidateApiKeyAsync(apiKey.Value);
            }, [apiKey]);

            // Update form when settings dialog opens
            UseEffect(() =>
            {
                if (isSettingsDialogOpen.Value)
                {
                    // Update form with current values when dialog opens
                    settingsForm.Set(new SettingsRequest
                    {
                        ApiKey = apiKey.Value ?? string.Empty,
                        Instructions = customInstructions.Value
                    });
                }
            }, [isSettingsDialogOpen]);

            // Handle settings dialog submission
            UseEffect(() =>
            {
                if (!isSettingsDialogOpen.Value)
                {
                    // Update API key if changed
                    if (!string.IsNullOrWhiteSpace(settingsForm.Value.ApiKey) && settingsForm.Value.ApiKey != apiKey.Value)
                    {
                        apiKey.Set(settingsForm.Value.ApiKey);
                    }
                    // Update instructions if changed
                    if (!string.IsNullOrWhiteSpace(settingsForm.Value.Instructions) && settingsForm.Value.Instructions != customInstructions.Value)
                    {
                        customInstructions.Set(settingsForm.Value.Instructions);
                    }
                }
            }, [isSettingsDialogOpen]);

            // Reset messages when API key is removed
            UseEffect(() =>
            {
                if (opperClient.Value == null)
                {
                    conversationHistory.Set(new List<string>());
                }
            }, [opperClient]);

            // Constants and computed values
            const string DefaultModel = "aws/claude-3.5-sonnet-eu";
            const string DefaultModelName = "aws/claude-3.5-sonnet-eu";
            var hasApiKey = !string.IsNullOrWhiteSpace(apiKey.Value) && opperClient.Value != null;

            // Validate API key asynchronously
            async Task ValidateApiKeyAsync(string? key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    opperClient.Value?.Dispose();
                    opperClient.Set(default(OpperClient?));
                    return;
                }

                isValidating.Set(true);
                try
                {
                    opperClient.Value?.Dispose();
                    var testClient = new OpperClient(key);
                    
                    // Try to make a simple API call to validate the key
                    await testClient.ListModelsAsync(limit: 1);
                    
                    // If successful, set the client and show success toast
                    opperClient.Set(testClient);
                    client.Toast("API key validated successfully!", "Success");
                }
                catch (Exception ex)
                {
                    // If validation fails, show error toast
                    opperClient.Set(default(OpperClient?));
                    var errorMessage = ex is OpperException opperEx 
                        ? $"API key validation error: {opperEx.Message}"
                        : $"API key validation error: {ex.Message}";
                    client.Toast(errorMessage, "Error");
                }
                finally
                {
                    isValidating.Set(false);
                }
            }

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
                        return new Option<string>($"{DefaultModel}", DefaultModelName);
                    }
                    
                    return null;
                }
                catch
                {
                    // On error, still return default model option
                    if (modelName == DefaultModelName)
                    {
                        return new Option<string>($"{DefaultModel}", DefaultModelName);
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
                        "Please click the 'Enter API Key' button above to enter your Opper.ai API key and start chatting. " +
                        "You can get your API key at https://platform.opper.ai/settings/api-keys")));
                    return;
                }

                // Check if model is selected
                if (string.IsNullOrWhiteSpace(selectedModel.Value))
                {
                    messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.User, @event.Value)));
                    messages.Set(messages.Value.Add(new Ivy.ChatMessage(ChatSender.Assistant, 
                        "Please select a model from the dropdown above before sending a message. " +
                        "The model selection is located in the header area.")));
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
                        Instructions = customInstructions.Value,
                        Input = contextualInput + $"User: {@event.Value}",
                        Model = selectedModel.Value ?? DefaultModelName
                    });

                    history.Add($"Assistant: {response.Message}");
                    conversationHistory.Set(history);
                    // Use Text.Markdown to render markdown and LaTeX expressions
                    var messageContent = Text.Markdown(response.Message);
                    messages.Set(currentMessages.Add(new Ivy.ChatMessage(ChatSender.Assistant, messageContent)));
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

            // Header: Title (left) | Model Selection and buttons (right)
            var header = Layout.Vertical()
                | (Layout.Horizontal()
                | (Layout.Vertical().Align(Align.Left)
                    | Text.H4("OpperAI Chat")).Width(Size.Fraction(0.4f))
                | (Layout.Horizontal().Align(Align.Right)
                    | selectedModel.ToAsyncSelectInput(QueryModels, LookupModel, placeholder: "Search and select model...")
                        .Disabled(!hasApiKey))
                | (Layout.Vertical().Align(Align.Right)
                    | new Button(
                        "API key/Settings",
                        onClick: _ => isSettingsDialogOpen.Set(true)
                    ).Outline().Icon(Icons.Settings)).Width(Size.Fraction(0.25f))
                );

            // Chat area - show instruction if no API key, otherwise show chat
            var chatCard = hasApiKey
                ? Layout.Vertical()
                    | new Chat(messages.Value.ToArray(), HandleMessageAsync)
                : Layout.Center()
                    | new Card(
                        Layout.Vertical().Gap(3).Padding(4)
                        | Text.H4("Welcome to OpperAI Chat!")
                        | Text.Muted("To get started, you need an API key from Opper.ai:")
                        
                        | (Layout.Vertical().Gap(1).Padding(2)
                            | Text.Markdown(@"1. Visit [https://platform.opper.ai](https://platform.opper.ai)
2. Sign up or log in to your [account](https://platform.opper.ai/settings/details)
3. Go to [Settings → API Keys](https://platform.opper.ai/settings/api-keys)
4. Create a new [API key](https://platform.opper.ai/settings/api-keys/create)
5. Click the 'API Key' button above to enter your API key"))
                        | Text.Muted("Once you enter your API key, you'll be able to chat with AI models!")
                        | new Embed("https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fopperai%2Fdevcontainer.json&location=EuropeWest").Width(Size.Fraction(0.6f))

                        ).Width(Size.Fraction(0.45f));

            // var body = Layout.Vertical().Gap(2).Align(Align.TopCenter)
            //     | ;

            return Layout.Horizontal()
                    | (Layout.Vertical().Gap(2).Align(Align.TopCenter)
                        | header.Width(Size.Fraction(0.6f)).Height(Size.Fit().Max(Size.Fraction(0.1f)))
                        | chatCard.Width(Size.Fraction(0.6f)).Height(Size.Full().Max(Size.Fraction(0.9f)))
                        )
                    | (isSettingsDialogOpen.Value ? 
                        settingsForm.ToForm()
                            .Builder(e => e.ApiKey, e => e.ToPasswordInput(placeholder: "Enter your Opper.ai API key..."))
                            .Label(e => e.ApiKey, "API Key:")
                            .Builder(e => e.Instructions, e => e.ToTextAreaInput(placeholder: "Enter instructions for the AI assistant...").Height(Size.Units(30)))
                            .Label(e => e.Instructions, "Instructions:")
                            .ToDialog(isSettingsDialogOpen,
                                title: "API key/Settings",
                                submitTitle: "Save",
                                width: Size.Units(600)
                            ) : null);
        }
    }
}

