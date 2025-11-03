namespace OllamaSharpExample;

[App(icon: Icons.TextQuote, title: "OllamaSharp")]
public class OllamaSharpApp : ViewBase
{
    public override object? Build()
    {
        return this.UseBlades(() => new ModelListBlade(), "Models");
    }
}