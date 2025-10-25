namespace IbanNetExample;

// Country data with code and name
public record CountryInfo(string Code, string Name)
{
    public string DisplayText => $"{Name} ({Code})";
}

// Ivy app declaration with icon and title
[App(icon: Icons.Globe, title: "IbanNet")]
public class IbanNetApp : ViewBase
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

        // Helper method to get country name from code
        string GetCountryName(string code)
        {
            try
            {
                var regionInfo = new RegionInfo(code);
                return regionInfo.EnglishName;
            }
            catch
            {
                return code;
            }
        }

        // Create country list with both code and name
        var countriesList = registry
            .Select(c => new CountryInfo(
                c.TwoLetterISORegionName,
                GetCountryName(c.TwoLetterISORegionName)))
            .Distinct()
            .OrderBy(c => c.Name)
            .ToArray();

        // Ivy async select input: searchable dropdown for country codes
        Task<Option<string>[]> QueryCountries(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(countriesList
                    .Select(c => new Option<string>(c.DisplayText, c.Code))
                    .ToArray());
            }

            var matches = countriesList
                .Where(c => c.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(c => new Option<string>(c.DisplayText, c.Code))
                .ToArray();

            return Task.FromResult(matches);
        }

        // Ivy async select input: resolves selected country
        Task<Option<string>?> LookupCountry(string country)
        {
            if (string.IsNullOrEmpty(country)) return Task.FromResult<Option<string>?>(null);
            
            var countryInfo = countriesList.FirstOrDefault(c => c.Code == country);
            if (countryInfo == null) return Task.FromResult<Option<string>?>(null);
            
            return Task.FromResult<Option<string>?>(new Option<string>(
                countryInfo.DisplayText, 
                countryInfo.Code));
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
                result.Value = $"Could not generate IBAN for {countryCode}";
                breakdown.Value = ex.Message;
            }
        }

        // Validates the IBAN using IbanNet
        void ValidateIban()
        {
            var validation = validator.Validate(ibanInput.Value); // Checks structure, length, checksum

            if (!validation.IsValid)
            {
                result.Value = "Invalid IBAN";
                breakdown.Value = "";
                return;
            }

            // Parses IBAN into structured components
            var iban = parser.Parse(ibanInput.Value);
            result.Value = $"Valid IBAN";
            breakdown.Value =
                $"Country: {iban.Country.TwoLetterISORegionName}\n" +
                $"Bank ID: {iban.BankIdentifier}\n" +
                $"Branch ID: {iban.BranchIdentifier}\n" +
                $"Obfuscated: {iban.ToString(IbanFormat.Obfuscated)}"; // Masks sensitive digits
        }

        // Simulates copying the IBAN to clipboard
        var copyMessage = UseState(() => "");
        void CopyIban() => copyMessage.Value = $"Copied: {ibanInput.Value}";

        // Left card: IBAN generation and input
        var cardLeft = new Card(Layout.Vertical().Gap(6).Padding(2)
            | Text.H2("IBAN Explorer") // App title

            // Country selector
            | Text.Label("Select a country:") // Prompt
            | selectedCountry.ToAsyncSelectInput(QueryCountries, LookupCountry, placeholder: "Search countries...")

            // IBAN generator
            | new Button("Generate Sample IBAN", GenerateSampleIban)); // Triggers dynamic generatio

        // Right card: Results
        var cardRight = new Card(Layout.Vertical().Gap(6).Padding(2)
            | Text.H3("Results")
            
            // Manual IBAN input
            | Text.Label("Enter or edit IBAN:") // Prompt
            | new TextInput(ibanInput).Placeholder("Enter IBAN here...") // Input field

            // Validate and copy actions
            | Layout.Horizontal().Gap(8)
                | new Button("Validate IBAN", ValidateIban) // Validates current input
                | new Button("Copy IBAN", CopyIban)         // Simulates copy action
            | (copyMessage.Value != "" ? Text.Small(copyMessage.Value) : null)

            | (result.Value != null ? Text.Block(result.Value) : Text.Small("No results yet..."))
            | (breakdown.Value != "" ? Text.Block(breakdown.Value) : null)); // Shows parsed details

        // Main layout: centered cards with gap 14
        return Layout.Center().Gap(14)
            | cardLeft.Width(Size.Fraction(0.45f)).Height(Size.Fraction(0.65f))
            | cardRight.Width(Size.Fraction(0.45f)).Height(Size.Fit().Min(Size.Fraction(0.65f)));
    }
}