namespace UlidExample;

[App(icon: Icons.Key, title: "ULID")]
public class UlidApp : ViewBase
{
    public override object? Build()
    {
        var client = UseService<IClientProvider>();

        // States
        var actionMode = this.UseState("Generate"); // Default to Generate
        var inputUlid = this.UseState("");
        var parsedUlid = this.UseState<Ulid?>(() => null);
        var parseError = this.UseState<string?>(() => null);
        var generatedUlid = this.UseState<Ulid?>(() => null);

        // Action mode options
        var actionOptions = new[] { "Generate", "Parse" }.ToOptions();

        // Handle Generate action
        void HandleGenerate()
        {
            var newUlid = Ulid.NewUlid();
            generatedUlid.Value = newUlid;
            client.Toast("ULID generated successfully", "Success");
        }

        // Handle Parse action
        void HandleParse()
        {
            if (string.IsNullOrWhiteSpace(inputUlid.Value))
            {
                client.Toast("Please enter a ULID to parse", "Validation Error");
                parseError.Value = "ULID cannot be empty";
                parsedUlid.Value = null;
                return;
            }

            if (Ulid.TryParse(inputUlid.Value, out var ulid))
            {
                parsedUlid.Value = ulid;
                parseError.Value = null;
            }
            else
            {
                parsedUlid.Value = null;
                parseError.Value = "Invalid ULID string";
            }
        }

        // Generate section content
        object? generateContent;
        if (generatedUlid.Value is Ulid ulid)
        {
            generateContent = Layout.Vertical()
                | Text.Label("Generated ULID")
                | new Code(ulid.ToString(), Languages.Text)
                    .ShowCopyButton()
                    .Height(Size.Fit().Max(30))
                | Text.Label("Timestamp (UTC)")
                | new Code(ulid.Time.ToString("O"), Languages.Text)
                    .ShowCopyButton()
                    .Height(Size.Fit().Max(30));
        }
        else
        {
            generateContent = Text.Muted("Click the Generate button to create a new ULID");
        }

        // Parse section content
        var parseContent = Layout.Vertical()
            | inputUlid.ToInput(placeholder: "Paste ULID here...").WithField().Label("Paste ULID here...")
            | new Button("Parse", HandleParse).Width(Size.Full()).Primary();

        object? parseResult;
        if (parsedUlid.Value is Ulid parsedUlidValue)
        {
            parseResult = Layout.Vertical().Gap(4)
                | Text.Label("Parsed Timestamp (UTC)")
                | new Code(parsedUlidValue.Time.ToString("O"), Languages.Text)
                    .ShowCopyButton()
                    .Height(Size.Fit().Max(30))
                | Callout.Success("ULID parsed successfully!", "Success");
        }
        else if (!string.IsNullOrEmpty(parseError.Value))
        {
            parseResult = Callout.Error(parseError.Value, "Parse Error");
        }
        else
        {
            parseResult = Callout.Info("Enter a ULID string and click Parse to see detailed information", "Instructions");
        }

        return Layout.Center()
            | new Card(
                Layout.Vertical().Padding(3)
                | Text.H3("ULID Generator and Parser")
                | Text.Muted("Generate new ULIDs (Universally Unique Lexicographically Sortable Identifiers) or parse existing ULIDs to view their properties and timestamps")
                | Text.Label("Action")
                | actionMode.ToSelectInput(actionOptions)
                    .Placeholder("Select action...")
                | (actionMode.Value == "Generate"
                    ? Layout.Vertical()
                        | new Button("Generate ULID", HandleGenerate).Width(Size.Full()).Primary()
                        | generateContent
                    : Layout.Vertical()
                        | parseContent
                        | parseResult
                )
                | Text.Small("This demo uses Ulid library for generating and parsing ULIDs.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Ulid](https://github.com/Cysharp/Ulid)")
            ).Width(Size.Fraction(0.4f));
    }
}
