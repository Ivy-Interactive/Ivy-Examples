using System.ComponentModel;
using System.Linq;
using LlmTornado.Agents.DataModels;

namespace LlmTornadoExample.Apps;

public class AgentChatBlade : ViewBase
{
    private record InstructionsModel(string Instructions);

    private readonly string _ollamaUrl;
    private readonly string _modelName;

    private TornadoApi? _api;

    public AgentChatBlade(string ollamaUrl, string modelName)
    {
        _ollamaUrl = ollamaUrl;
        _modelName = modelName;
    }

    [Description("Get the current date and time. Useful for answering questions about what day it is, what time it is, or scheduling-related queries.")]
    private string GetCurrentTime(
        [Description("The timezone to get the time for (optional, defaults to local time). Examples: 'UTC', 'Eastern Standard Time', 'Central European Standard Time'.")]
        string? timezone = null)
    {
        var now = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(timezone))
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
                return $"Current date and time in {timezone}: {now:yyyy-MM-dd HH:mm:ss} ({now:dddd}, {timeZoneInfo.DisplayName})";
            }
            catch
            {
                // If timezone is invalid, use local time
                return $"Current date and time (local): {now:yyyy-MM-dd HH:mm:ss} ({now:dddd})\nNote: Invalid timezone '{timezone}', using local time instead.";
            }
        }

        return $"Current date and time: {now:yyyy-MM-dd HH:mm:ss} ({now:dddd})";
    }

    [Description("Get the weather information for a specific city.")]
    [ToolName("GetWeatherTool")]
    private string GetWeather([Description("The city name to get weather information for")] string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return "Error: City name cannot be empty. Please provide a valid city name.";
        }

        // Simulated weather data - in a real application, this would call a weather API
        var cityLower = city.Trim().ToLowerInvariant();
        var temperature = cityLower.GetHashCode() % 30 + 5; // Generate consistent "fake" temperature between 5-35°C
        var conditions = new[] { "Sunny", "Partly Cloudy", "Cloudy", "Rainy", "Clear" };
        var condition = conditions[Math.Abs(cityLower.GetHashCode()) % conditions.Length];

        return $"Weather in {city}: {temperature}°C, {condition}";
    }

    [Description("Perform mathematical calculations: add, subtract, multiply, or divide two numbers.")]
    private string Calculate(
        [Description("The math operation to perform: 'add', 'subtract', 'multiply', or 'divide'")]
        string operation,
        [Description("The first number")]
        double a,
        [Description("The second number")]
        double b)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return "Error: Operation cannot be empty. Please specify: 'add', 'subtract', 'multiply', or 'divide'.";
        }

        var operationLower = operation.Trim().ToLowerInvariant();
        double result = operationLower switch
        {
            "add" or "+" => a + b,
            "subtract" or "sub" or "-" => a - b,
            "multiply" or "*" => a * b,
            "divide" or "/" => b != 0 ? a / b : double.NaN,
            _ => double.NaN
        };

        if (double.IsNaN(result))
        {
            if (operationLower == "divide" && b == 0)
            {
                return $"Error: Division by zero is not allowed.";
            }
            return $"Error: Unknown operation '{operation}'. Supported operations: 'add', 'subtract', 'multiply', 'divide'.";
        }

        return $"{a} {GetOperationSymbol(operationLower)} {b} = {result}";
    }

    private string GetOperationSymbol(string operation)
    {
        return operation switch
        {
            "add" or "+" => "+",
            "subtract" or "sub" or "-" => "-",
            "multiply" or "*" => "×",
            "divide" or "/" => "÷",
            _ => "?"
        };
    }

    public override object? Build()
    {
        var client = UseService<IClientProvider>();
        var messages = UseState(ImmutableArray.Create<ChatMessage>(
            new ChatMessage(ChatSender.Assistant, Text.Markdown(
                "Hello! I'm an AI agent with access to tools. I can:\n\n" +
                "• **Get the current time**\n\n" +
                "• **Calculate mathematical expressions**\n\n" +
                "• **Get weather information**\n\n" +
                "Try asking me: 'What time is it?' or 'Calculate 15 * 23'"))
        ));
        var instructions = UseState("You are a helpful assistant with access to tools. You can get the current time, perform calculations, and get weather information. Use these tools when appropriate to answer user questions.");
        var instructionsForm = UseState(() => new InstructionsModel(instructions.Value));
        var showSettings = UseState(false);

        UseEffect(async () =>
        {
            if (_api == null)
            {
                try
                {
                    _api = new TornadoApi(new Uri(_ollamaUrl));
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    client.Toast($"Failed to connect to Ollama: {ex.Message}", "Connection Error");
                }
            }
        }, EffectTrigger.OnMount());

        UseEffect(() =>
        {
            if (!showSettings.Value && instructionsForm.Value.Instructions != instructions.Value)
            {
                instructions.Set(instructionsForm.Value.Instructions);
                client.Toast("Agent instructions updated", "Settings");
            }
        }, [showSettings]);


        var instructionsDialog = instructionsForm.ToForm()
            .Builder(m => m.Instructions, m => m.ToTextareaInput(placeholder: "Enter instructions for the agent...")
                .Height(Size.Units(100)))
            .Label(m => m.Instructions, "Agent Instructions")
            .ToDialog(showSettings,
                title: "Agent Instructions",
                description: "Configure the system instructions for the AI agent",
                width: Size.Units(125));

        var header = Layout.Horizontal().Gap(2)
                    | (Layout.Vertical().Gap(2).AlignContent(Align.Center).Width(Size.Fit())
                        | new Icon(Icons.Bot).Size(Size.Units(8)))
                    | Text.H4($"Agent Chat - {_modelName}")
                    | (Layout.Vertical().Gap(2).AlignContent(Align.Center).Width(Size.Fit())
                        | new Button("Settings", icon: Icons.Settings, onClick: _ =>
                        {
                            instructionsForm.Set(new InstructionsModel(instructions.Value));
                            showSettings.Set(true);
                        }).Ghost().Tooltip("Edit agent instructions"));

        void OnSendMessage(Event<Chat, string> @event)
        {
            if (_api == null)
            {
                client.Toast("LlmTornado API client is not initialized.", "Not Ready");
                return;
            }

            var userMessage = @event.Value;
            var currentMessages = messages.Value;

            // Add user message
            var messagesWithUser = currentMessages.Add(new ChatMessage(ChatSender.User, userMessage));

            // Add loading state
            var messagesWithLoading = messagesWithUser.Add(
                new ChatMessage(ChatSender.Assistant, new ChatStatus("Thinking..."))
            );

            messages.Set(messagesWithLoading);

            // Process agent response
            _ = Task.Run(async () =>
            {
                var assistantMessageIndex = messages.Value.Length - 1;
                var streamingText = new StringBuilder();
                var isWaitingForFirstWord = true;
                var lastUpdate = DateTime.UtcNow;

                try
                {
                    // Create agent with tools and streaming enabled
                    TornadoAgent agent = new TornadoAgent(
                        client: _api,
                        model: _modelName,
                        instructions: instructions.Value,
                        tools: [GetCurrentTime, GetWeather, Calculate],
                        streaming: true
                    );

                    // Define streaming handler
                    ValueTask streamHandler(AgentRunnerEvents runEvent)
                    {
                        if (runEvent is AgentRunnerStreamingEvent streamingEvent)
                        {
                            if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                            {
                                streamingText.Append(deltaTextEvent.DeltaText);

                                // If this is the first word, replace waiting status with actual text
                                if (isWaitingForFirstWord)
                                {
                                    isWaitingForFirstWord = false;
                                }

                                // Update UI every 100ms to reduce flicker
                                if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds > 100)
                                {
                                    var currentMessagesList = messages.Value.ToList();
                                    if (currentMessagesList.Count > assistantMessageIndex)
                                    {
                                        currentMessagesList[assistantMessageIndex] = new ChatMessage(
                                            ChatSender.Assistant,
                                            Text.Markdown(streamingText.ToString())
                                        );
                                        messages.Set(currentMessagesList.ToImmutableArray());
                                        lastUpdate = DateTime.UtcNow;
                                    }
                                }
                            }
                        }
                        return ValueTask.CompletedTask;
                    }

                    // Run with streaming
                    Conversation result = await agent.Run(userMessage, onAgentRunnerEvent: streamHandler);

                    // Final update with complete response
                    var response = streamingText.ToString();

                    // Check if model supports tools (if response is empty, null, or default message, model might not support tools)
                    if (string.IsNullOrWhiteSpace(response) ||
                        response.Trim().Equals("I couldn't generate a response.", StringComparison.OrdinalIgnoreCase))
                    {
                        response = $"**Tools Not Supported**\n\n" +
                                  $"The model `{_modelName}` does not support function calling/tools.\n\n" +
                                  $"**What this means:**\n" +
                                  $"This model cannot use tools to perform actions like getting weather, calculating, or checking the current time.\n\n" +
                                  $"**Recommendations:**\n\n" +
                                  $"• Use the **Simple Chat** mode instead (works with any model)\n\n" +
                                  $"• For Ollama: check if your model version supports tools/function calling";
                    }

                    // Final update
                    var finalMessages = messages.Value.Take(messages.Value.Length - 1).ToImmutableArray();
                    messages.Set(finalMessages.Add(new ChatMessage(ChatSender.Assistant, Text.Markdown(response))));
                }
                catch (Exception ex)
                {
                    var errorMessages = messages.Value;
                    if (errorMessages.Length > 0 && errorMessages[errorMessages.Length - 1].Sender == ChatSender.Assistant)
                    {
                        errorMessages = errorMessages.Take(errorMessages.Length - 1).ToImmutableArray();
                    }
                    messages.Set(errorMessages.Add(
                        new ChatMessage(ChatSender.Assistant, Text.Markdown($"**Error:** {ex.Message}"))
                    ));
                }
            });
        }

        var chatContent = Layout.Horizontal()
                | (Layout.Vertical().Width(Size.Units(200).Max(Size.Units(400))).Height(Size.Auto())
                    | new Chat(messages.Value.ToArray(), OnSendMessage));

        return new Fragment()
            | new BladeHeader(header)
            | chatContent
            | instructionsDialog;
    }
}