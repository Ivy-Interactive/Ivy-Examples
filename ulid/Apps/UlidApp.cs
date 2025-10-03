using Cysharp.Ulid;   // for Ulid
using Ivy;

namespace customApp.Apps;

[App(icon: Icons.Key, title: "ULID Demo")]
public class UlidApp : ViewBase
{
    public override object? Build()
    {
        // States
        var inputUlid = this.UseState("");
        var parsedUlid = this.UseState<Ulid?>(() => null);   // start with null
        var parseError = this.UseState<string?>(() => null); // start with null

        var currentUlid = this.UseState(Ulid.NewUlid);

        return Layout.Center()
            | new Card(
                Layout.Vertical().Gap(10).Padding(3)

                // Heading
                | Text.H2("ULID Demo in Ivy")

                // Show generated ULID
                | Text.Block($"Generated ULID: {currentUlid.Value}")
                | Text.Block($"Timestamp (UTC): {currentUlid.Value.Time.ToString("O")}")

                // Button to generate a new ULID
                | new Button("Generate New ULID", () =>
                {
                    currentUlid.Value = Ulid.NewUlid();
                }).Primary()

                | new Separator()

                // Input for parsing
                | Text.Block("Enter ULID to parse:")
                | inputUlid.ToInput(placeholder: "Paste ULID here...")
                | new Button("Parse", () =>
                {
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
                }).Primary()


                // Results
                | (parsedUlid.Value != null
                    ? Layout.Vertical()
                        | Text.Block($"Parsed ULID: {parsedUlid.Value}")
                        | Text.Block($"Parsed Timestamp: {parsedUlid.Value?.Time.ToString("O")}")
                        | Text.Block($"Comparison vs Generated: {CompareUlids(parsedUlid.Value.Value, currentUlid.Value)}")
                    : !string.IsNullOrEmpty(parseError.Value)
                        ? Text.Block($"Error: {parseError.Value}")
                        : null
                  )
            ).Width(Size.Units(120).Max(500));
    }

    private string CompareUlids(Ulid parsed, Ulid current)
    {
        int cmp = parsed.CompareTo(current);
        if (cmp < 0) return "Parsed < Generated";
        if (cmp > 0) return "Parsed > Generated";
        return "Parsed == Generated";
    }
}
