using System;
using System.Linq;
using System.Threading.Tasks;
using IbanNet;               // IbanNet provides validation, parsing, and generation of IBANs
using IbanNet.Registry;      // Access to country-specific IBAN formats and metadata
using Ivy;                   // Ivy UI framework for building interactive apps

namespace IbanApp.Apps;

// Ivy app declaration with icon and title
[App(icon: Icons.Globe, title: "IBAN Demo App")]
public class IbanModel : ViewBase
{
    public override object? Build()
    {
        // Core IbanNet components
        var validator = new IbanValidator();                     // Validates IBAN structure, length, and checksum
        var parser = new IbanParser(IbanRegistry.Default);       // Parses IBAN into structured components
        var registry = IbanRegistry.Default;                     // Registry of 126 supported countries and formats

        // Ivy state hooks for UI interactivity
        var selectedCountry = UseState<string?>(default(string)); // Tracks selected country code (e.g., "GB")
        var ibanInput = UseState(() => "");                       // Tracks IBAN input from user or generator
        var result = UseState(() => (string?)null);               // Stores validation result message
        var breakdown = UseState(() => "");                       // Stores parsed IBAN details

        // Ivy async select input: searchable dropdown for country codes
        Task<Option<string>[]> QueryCountries(string query)
        {
            var countries = registry
                .Select(c => c.TwoLetterISORegionName) // Extract ISO country codes
                .Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(c => c)
                .ToArray();

            return Task.FromResult(countries.Select(c => new Option<string>(c)).ToArray());
        }

        // Ivy async select input: resolves selected country
        Task<Option<string>?> LookupCountry(string country)
        {
            if (string.IsNullOrEmpty(country)) return Task.FromResult<Option<string>?>(null);
            return Task.FromResult<Option<string>?>(new Option<string>(country));
        }

        // Generates a valid IBAN using IbanNet's built-in generator
        void GenerateSampleIban()
        {
            var countryCode = selectedCountry.Value ?? "GB"; // Default to GB if none selected
            var generator = new IbanGenerator(IbanRegistry.Default); // Uses registry to generate valid IBAN

            try
            {
                var iban = generator.Generate(countryCode); // Generates a checksum-valid IBAN
                ibanInput.Value = iban.ToString();          // Update input field with generated IBAN
                result.Value = null;
                breakdown.Value = "";
            }
            catch (Exception ex)
            {
                // Handles unsupported countries or generation errors
                ibanInput.Value = "";
                result.Value = $"❌ Could not generate IBAN for {countryCode}";
                breakdown.Value = ex.Message;
            }
        }

        // Validates the IBAN using IbanNet
        void ValidateIban()
        {
            var validation = validator.Validate(ibanInput.Value); // Checks structure, length, checksum

            if (!validation.IsValid)
            {
                result.Value = "❌ Invalid IBAN";
                breakdown.Value = "";
                return;
            }

            // Parses IBAN into structured components
            var iban = parser.Parse(ibanInput.Value);
            result.Value = $"✅ Valid IBAN";
            breakdown.Value =
                $"Country: {iban.Country.TwoLetterISORegionName}\n" +
                $"Bank ID: {iban.BankIdentifier}\n" +
                $"Branch ID: {iban.BranchIdentifier}\n" +
                $"Obfuscated: {iban.ToString(IbanFormat.Obfuscated)}"; // Masks sensitive digits
        }

        // Simulates copying the IBAN to clipboard
        var copyMessage = UseState(() => "");
        void CopyIban() => copyMessage.Value = $"📋 Copied: {ibanInput.Value}";

        // Ivy UI layout: vertical stack with spacing and padding
        return Layout.Vertical().Gap(5).Padding(5)

            | Text.H2("🌍 IBAN Explorer") // App title

            // Country selector
            | Text.Label("Select a country:") // Prompt
            | selectedCountry.ToAsyncSelectInput(QueryCountries, LookupCountry, placeholder: "Search countries...")
            | Text.Small($"Selected: {selectedCountry.Value ?? "None"}") // Display selected country

            // IBAN generator
            | new Button("Generate Sample IBAN", GenerateSampleIban) // Triggers dynamic generation

            // Manual IBAN input
            | Text.Label("Enter or edit IBAN:") // Prompt
            | new TextInput(ibanInput).Placeholder("Enter IBAN here...") // Input field

            // Validate and copy actions
            | Layout.Horizontal().Gap(8)
                | new Button("Validate IBAN", ValidateIban) // Validates current input
                | new Button("Copy IBAN", CopyIban)         // Simulates copy action

            // Result panel
            | (result.Value != null ? Text.Block(result.Value) : null) // Shows validation result
            | (breakdown.Value != "" ? Text.Block(breakdown.Value) : null) // Shows parsed details
            | (copyMessage.Value != "" ? Text.Small(copyMessage.Value) : null); // Shows copy confirmation
    }
}