namespace HandlebarsNetExample;

[App(icon: Icons.FileText, title: "Json template value mapping")]
public class TemplateApp : ViewBase
{
    public override object? Build()
    {
        // State for the Handlebars template string
        var templateState = this.UseState<string>("<h1>Hello {{name}}!</h1><p>Your favorite fruits are: {{#each items}}<li>{{this}}</li>{{/each}}</p>");

        // State for the JSON model string
        var modelState = this.UseState<string>(JsonSerializer.Serialize(new { name = "Alice", items = new[] { "Apple", "Banana", "Cherry" } }, new JsonSerializerOptions { WriteIndented = true }));

        // State for the rendered output
        var outputState = this.UseState<string?>();

        // Re-render whenever the template or model changes
        void Render()
        {
            try
            {
                // Parse the JSON model string
                var model = JsonNode.Parse(modelState.Value);

                // Compile the Handlebars template
                var template = Handlebars.Compile(templateState.Value);

                // Render the template with the model
                var result = template(model);

                // Update the output state
                outputState.Value = result;
            }
            catch (Exception ex)
            {
                // Display any errors that occur during parsing or rendering
                outputState.Value = $"Error: {ex.Message}";
            }
        }

        templateState.Subscribe(_ => Render());
        modelState.Subscribe(_ => Render());

        // Run once initially to populate the output
        Render();

        return Layout.Center().Width(800) | new Card(
            Layout.Vertical().Gap(10).Padding(2)
            | Text.H2("Handlebars.Net Demo")
            | Text.Block("Enter a Handlebars template and a JSON model to see the live output.")
            | Text.H3("Template")
            | templateState.ToCodeInput(placeholder: "Handlebars Template")
            | Text.H3("JSON Model")
            | modelState.ToCodeInput(language: Languages.Json, placeholder: "JSON Model")
            | new Separator()
            | Text.H3("Output")
            | new Html(outputState.Value ?? "Output will appear here...") // Use Html component to display the rendered string
        );
    }
}
