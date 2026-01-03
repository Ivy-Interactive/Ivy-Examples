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
        var ollamaUrl = UseState("http://localhost:11434");
        var selectedModel = UseState("llama3.2:1b");

        return BladeHelper.WithHeader(
            Text.H4("LlmTornado Examples"),
            Layout.Vertical().Gap(4).Padding(4)
                | new Card(
                    Layout.Vertical().Gap(3)
                    | Text.H4("Getting Started")
                    | Text.Muted("LlmTornado - Modern LLM Library for .NET")
                    | Text.Markdown("**1. Install Ollama** from [https://ollama.com/download](https://ollama.com/download)")
                    | Text.Markdown("**2. Download a Model:**")
                    | new Code("ollama pull llama3.2:1b").ShowCopyButton()
                    | Text.Markdown("**3. Ollama Configuration:**")
                    | Layout.Horizontal().Gap(2)
                        | (Layout.Vertical().Gap(1)
                            | Text.Small("Ollama URL").Bold()
                            | ollamaUrl.ToTextInput(placeholder: "http://localhost:11434")
                                .Width(Size.Units(60)))
                        | (Layout.Vertical().Gap(1)
                            | Text.Small("Model").Bold()
                            | selectedModel.ToTextInput(placeholder: "llama3.2:1b")
                                .Width(Size.Units(40)))
                    | new Separator()
                    | Text.Small("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [LlmTornado](https://llmtornado.ai)")
                )
                | Text.H4("Examples")
                | Layout.Grid(3).Gap(3)
                    | new Card(
                        Layout.Vertical().Gap(2)
                        | new Icon(Icons.MessageSquare).Size(32)
                        | Text.H4("Simple Chat")
                        | Text.Small("Basic conversation with streaming responses")
                        | new Button("Try It")
                            .Variant(ButtonVariant.Primary)
                            .HandleClick(_ => blades.Push(this, new SimpleChatBlade(ollamaUrl.Value, selectedModel.Value), "Simple Chat"))
                    )
                    | new Card(
                        Layout.Vertical().Gap(2)
                        | new Icon(Icons.Bot).Size(32)
                        | Text.H4("Agent with Tools")
                        | Text.Small("Agent with function calling capabilities")
                        | new Button("Try It")
                            .Variant(ButtonVariant.Primary)
                            .HandleClick(_ => blades.Push(this, new AgentChatBlade(ollamaUrl.Value, selectedModel.Value), "Agent Chat"))
                    )
                    | new Card(
                        Layout.Vertical().Gap(2)
                        | new Icon(Icons.Image).Size(32)
                        | Text.H4("Multimedia")
                        | Text.Small("Working with images and multimodal models")
                        | new Button("Try It")
                            .Variant(ButtonVariant.Primary)
                            .HandleClick(_ => blades.Push(this, new MultimediaBlade(ollamaUrl.Value, selectedModel.Value), "Multimedia"))
                    )
        );
    }
}

