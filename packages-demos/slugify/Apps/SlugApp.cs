namespace SlugApp;

[App(icon: Icons.Pencil, title: "Slug Generator")]
public class SlugApp : ViewBase
{
    public override object? Build()
    {
        
        //Declare states
        var inputState = UseState("");
        var slugState = UseState("");
        var errorState = UseState("");

        var yesNoOptions = new[] { "Yes", "No" }.ToOptions();
        
        var lowerCaseState = UseState("Yes");
        var collapseWhitespaceState = UseState("Yes");
        var collapseDashesState = UseState("Yes");
        var trimWhitespaceState = UseState("Yes");

        bool ToBool(string? v) => (v ?? "no").ToLowerInvariant() == "yes";
        
        
        var onSlugify = (Event<Button> e) =>
        {
            try
            {
                errorState.Set("");

                // Configure SlugHelper with user-selected options
                var config = new SlugHelperConfiguration
                {
                    ForceLowerCase = ToBool(lowerCaseState.Value),
                    TrimWhitespace = ToBool(trimWhitespaceState.Value),
                    CollapseDashes = ToBool(collapseDashesState.Value)
                };

                // Create SlugHelper instance with the configuration
                var slugifier = new SlugHelper(config);
                
                var slug = slugifier.GenerateSlug(inputState.Value ?? "");

                // Update state
                slugState.Set(slug);
            }
            catch (Exception ex)
            {
                errorState.Set($"Error: {ex.Message}");
            }
        };

        //  UI
        return Layout.Vertical(

            // TOP PANEL
            new Card(
                Layout.Vertical().Gap(5)
                    | Text.H2("Slug Generator")
                    | Text.Muted("Convert any text into SEO-friendly slugs")

                    | inputState.ToTextInput().Placeholder("Enter text…").WithLabel("Input")
                
                    //Two rows of two inputs each
                    //ROW 1
                    | Layout.Horizontal(
                        lowerCaseState.ToSelectInput(yesNoOptions).WithLabel("Force lowercase"),
                        collapseWhitespaceState.ToSelectInput(yesNoOptions).WithLabel("Collapse whitespace")
                    )

                    //ROW 2
                    | Layout.Horizontal(
                        collapseDashesState.ToSelectInput(yesNoOptions).WithLabel("Collapse dashes"),
                        trimWhitespaceState.ToSelectInput(yesNoOptions).WithLabel("Trim whitespace")
                    )

                    | new Button("Generate Slug", onSlugify).Primary().Width(Size.Full())

                    | (errorState.Value.Length > 0
                        ? new Callout(errorState.Value).Variant(CalloutVariant.Error)
                        : null)
            ).Width(Size.Full()),

            // BOTTOM PANEL
            new Card(
                Layout.Vertical().Gap(4)
                    | Text.H2("Result")
                    | (string.IsNullOrEmpty(slugState.Value)
                        ? Text.Muted("Slug will appear here…")
                        : new Expandable("Generated Slug", Text.Code(slugState.Value)))
            )
        );
    }
}