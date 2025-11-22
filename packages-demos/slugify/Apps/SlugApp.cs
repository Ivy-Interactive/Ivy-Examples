namespace SlugApp;

[App(icon: Icons.Pencil, title: "Slug Generator")]
public class SlugApp : ViewBase
{
    public override object? Build()
    {
        // Declare states
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

                var config = new SlugHelperConfiguration
                {
                    ForceLowerCase = ToBool(lowerCaseState.Value),
                    TrimWhitespace = ToBool(trimWhitespaceState.Value),
                    CollapseDashes = ToBool(collapseDashesState.Value),
                };

                var slugifier = new SlugHelper(config);
                slugState.Set(slugifier.GenerateSlug(inputState.Value ?? ""));
            }
            catch (ArgumentException ex)
            {
                errorState.Set($"Error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                errorState.Set($"Error: {ex.Message}");
            }
            catch (Exception ex) when (
                ex is not OutOfMemoryException &&
                ex is not StackOverflowException &&
                ex is not ThreadAbortException
            )
            {
                errorState.Set($"Unexpected error: {ex.Message}");
            }
        };

        // UI
        return Layout.Vertical(
            new Card(
                Layout.Vertical().Gap(5)
                    | Text.H2("Slug Generator")
                    | Text.Muted("Convert any text into SEO-friendly slugs")

                    // Input
                    | inputState.ToTextInput().Placeholder("Enter text…").WithLabel("Input")

                    // Options Row 1
                    | Layout.Horizontal(
                        lowerCaseState.ToSelectInput(yesNoOptions).WithLabel("Force lowercase"),
                        collapseWhitespaceState.ToSelectInput(yesNoOptions).WithLabel("Collapse whitespace")
                    )

                    // Options Row 2
                    | Layout.Horizontal(
                        collapseDashesState.ToSelectInput(yesNoOptions).WithLabel("Collapse dashes"),
                        trimWhitespaceState.ToSelectInput(yesNoOptions).WithLabel("Trim whitespace")
                    )

                    // Generate Button
                    | new Button("Generate Slug", onSlugify).Primary().Width(Size.Full())

                    // Result
                    | Text.H2("Result")
                    | (string.IsNullOrEmpty(slugState.Value)
                        ? Text.Muted("Slug will appear here…")
                        : Text.Code(slugState.Value))

                    | (string.IsNullOrEmpty(errorState.Value)
                        ? null
                        : new Callout(errorState.Value).Variant(CalloutVariant.Error))
            ).Width(Size.Full())
        );
    }
}