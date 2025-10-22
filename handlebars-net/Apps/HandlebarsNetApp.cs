namespace HandlebarsNetExample;

[App(icon: Icons.FileText, title: "Handlebars.Net")]
public class HandlebarsNetApp : ViewBase
{
    public override object? Build()
    {
        // State for the Handlebars template string
        var templateState = this.UseState<string>(@"
    <div>
    <h1>Hello {{name}}!</h1>
    <p>Welcome to {{company}} - {{department}} Department</p>
    
    <h3>Your Tasks:</h3>
    <ul>
        {{#each tasks}}
        <li><strong>{{title}}</strong> - {{description}}</li>
        {{/each}}
    </ul>
    
    {{#if isManager}}
    <div>
        <strong>Manager Access:</strong> You can view all team reports.
    </div>
    {{/if}}
    
    <p>Generated on {{date}}</p>
</div>");

        // State for the JSON model string
        var modelState = this.UseState<string>(JsonSerializer.Serialize(new {
            name = "John Smith",
            company = "TechCorp",
            department = "Engineering",
            isManager = true,
            date = DateTime.Now.ToString("MMM dd, yyyy"),
            tasks = new[] {
                new { title = "Code Review", description = "Review pull requests" },
                new { title = "Team Meeting", description = "Weekly standup at 10 AM" },
                new { title = "Bug Fix", description = "Fix login issue" }
            }
        }, new JsonSerializerOptions { WriteIndented = true }));

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
                outputState.Value = $"<div><strong>Error:</strong> {ex.Message}</div>";
            }
        }

        templateState.Subscribe(_ => Render());
        modelState.Subscribe(_ => Render());

        // Run once initially to populate the output
        Render();

        return  (Layout.Horizontal().Gap(10).Padding(3).Align(Align.TopCenter)
            | new Card(
                Layout.Vertical()
            | Text.H2("Handlebars.Net Demo")
            | Text.Block("Simple example showing how Handlebars.Net works. Change template or data to see live results!")
            | Layout.Horizontal().Gap(3)
                | Layout.Vertical().Gap(2).Width(Size.Fraction(0.5f))
                    | Text.H3("Template")
                    | templateState.ToCodeInput().Language(Languages.Html).Height(Size.Auto())
                | Layout.Vertical().Gap(2).Width(Size.Fraction(0.5f))
                    | Text.H3("Data")
                    | modelState.ToCodeInput().Language(Languages.Json).Height(Size.Auto())).Width(Size.Fraction(0.6f)))
            | new Card(
                Layout.Vertical()
            | Text.H2("Result")
            | new Html(outputState.Value ?? "Result will appear here...")).Width(Size.Fraction(0.4f));
    }
}
