namespace OllamaSharpExample;

[App(icon: Icons.BotMessageSquare, title: "OllamaSharp Chat")]
public class OllamaSharpChatApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new ModelListBlade(), "Models");
    }
}

[App(icon: Icons.Info, title: "OllamaSharp Introduction", isVisible: false)]
public class OllamaSharpIntroductionApp : ViewBase
{
    public override object? Build()
    {
        return Layout.Center()
               | new Card(
                   (Layout.Vertical()
                   | Text.H3("Getting Started with OllamaSharp")
                   | Text.Muted("Follow these steps to get started:")
                   | Text.Markdown("**1. Download Ollama** from [https://ollama.com/download](https://ollama.com/download)")
                   | Text.Markdown("**2. Download Models** for example:")
                   | new Code("ollama pull llama2")
                       .ShowCopyButton()
                   | Text.Markdown("**3. Default Configuration** Ollama runs on `http://localhost:11434` by default"))
                   | new Separator()
                   | Text.Small("This demo uses OllamaSharp library for interacting with Ollama API.")
                   | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [OllamaSharp](https://github.com/awaescher/OllamaSharp)")
                   ).Width(Size.Fit());
    }
}