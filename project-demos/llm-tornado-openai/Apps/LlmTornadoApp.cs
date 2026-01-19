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
        var blades = UseContext<IBladeService>();
        var client = UseService<IClientProvider>();
        var configuration = UseService<IConfiguration>();
        
        // Get OpenAI API key and model from configuration (dotnet secrets)
        var openAiApiKey = UseState(configuration["OpenAI:ApiKey"] ?? "");
        var selectedModel = UseState<string>(configuration["OpenAI:Model"] ?? "");

        var content = Layout.Vertical()
                | new Card(
                    Layout.Horizontal().Gap(3)
                    | (Layout.Vertical().Gap(2).Align(Align.Center).Width(Size.Fit())
                    | new Icon(Icons.MessageSquare).Size(16))
                    | (Layout.Vertical().Gap(2)
                        | Text.H3("Simple Chat").Bold()
                        | Text.Block("Basic conversation with streaming responses").Muted()
                        | new Button("Try It")
                            .Variant(ButtonVariant.Primary)
                            .Disabled(string.IsNullOrWhiteSpace(selectedModel.Value) || string.IsNullOrWhiteSpace(openAiApiKey.Value))
                            .HandleClick(_ => blades.Push(this, new SimpleChatBlade(openAiApiKey.Value, selectedModel.Value), "Simple Chat")))
                )
                | new Card(
                    Layout.Horizontal().Gap(3)
                    | (Layout.Vertical().Gap(2).Align(Align.Center).Width(Size.Fit())
                        | new Icon(Icons.Bot).Size(16))
                    | (Layout.Vertical().Gap(2)
                        | Text.H3("Agent with Tools").Bold()
                        | Text.Block("Agent with function calling capabilities").Muted()
                        | new Button("Try It")
                            .Variant(ButtonVariant.Primary)
                            .Disabled(string.IsNullOrWhiteSpace(selectedModel.Value) || string.IsNullOrWhiteSpace(openAiApiKey.Value))
                            .HandleClick(_ => blades.Push(this, new AgentChatBlade(openAiApiKey.Value, selectedModel.Value), "Agent Chat")))
                );

        return new Fragment()
               | new BladeHeader(Text.H4("LlmTornado Examples"))
               | content;
    }
}

