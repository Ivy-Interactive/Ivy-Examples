namespace IbanNetExample
{
    [App(icon: Icons.PartyPopper, title: "IBAN Demo")]
    public class IbanNetDemoApp : ViewBase
    {
        private readonly IbanValidator _validator;
        private readonly IbanParser _parser;
        private readonly IbanRegistry _registry;
        private readonly SwiftRegistryProvider _swiftProvider = new();
        private readonly WikipediaRegistryProvider _wikiProvider = new();
        
        /// <summary>Initialize components for IBAN parsing, validation, and registry</summary>
        public IbanNetDemoApp()
        {
            _registry = new IbanRegistry { Providers = { _swiftProvider, _wikiProvider } };
            _parser = new IbanParser(_registry);
            _validator = new IbanValidator();
        }

        public override object? Build()
        {
            var ibanState = UseState<string>();
            var outputState = UseState<string>();
            var badgeState = UseState<string>();
            var selectedCountry = UseState<string>();

            var countries = _registry.OrderBy(c => c.TwoLetterISORegionName).Select(c => c.TwoLetterISORegionName).ToArray();
            var countrySelect = selectedCountry.ToSelectInput(countries.ToOptions());
            var ibanInput = new TextInput(ibanState.Value, e => HandleIbanChanged(e.Value), placeholder: "Enter IBAN here");
            bool hasValidIban = _parser.TryParse(ibanState.Value, out var iban);

            // Create a badge based on the validation state
            var badge = string.IsNullOrEmpty(badgeState.Value)
                ? null
                : new Badge(
                    badgeState.Value,
                    icon: badgeState.Value switch
                    {
                        "Valid" => Icons.Check,
                        "Invalid" => Icons.X,
                        _ => Icons.Info
                    },
                    variant: badgeState.Value switch
                    {
                        "Valid" => BadgeVariant.Primary,
                        "Invalid" => BadgeVariant.Destructive,
                        _ => BadgeVariant.Secondary
                    });

            var generateBtn = new Button("Generate Test IBAN", () =>
            {
                try
                {
                    var generator = new IbanGenerator();
                    var generated = generator.Generate(selectedCountry.Value);
                    HandleIbanChanged(generated.ToString());
                    outputState.Set("");

                }
                catch
                {
                    outputState.Set("Cannot generate IBAN for selected country");
                }
            });
            var inputLayout = Layout.Center().Height(Size.Full())
                        | new Card(
                            Layout.Vertical().Gap(6).Padding(2)
                            | Text.Label("IBAN")
                            | Layout.Horizontal(ibanInput, badge)
                            | new Separator()
                            | Text.Label("IBAN Generator")
                            | Layout.Horizontal(countrySelect, generateBtn)
                            | new Separator()
                            | Text.Danger(outputState.Value ?? "")
                            ).Width(Size.Units(120).Max(600));

            return Layout.Horizontal().Height(Size.Full())
                | Layout.Grid().Columns(2)
                    | inputLayout
                    | (hasValidIban
                    ? new IbanDetailsView(iban)
                    : null); ;

            /// <summary>Handles user input: validates IBAN, updates states and error messages.</summary>
            void HandleIbanChanged(string? value)
            {
                outputState.Set("");
                ibanState.Set(value);
                if (value.Length == 0)
                {
                    badgeState.Set("");

                }
                else
                {
                    var result = _validator.Validate(ibanState.Value);
                    if (result.IsValid)
                    {
                        badgeState.Set("Valid");
                    }
                    else
                    {
                        outputState.Set(result.Error.ErrorMessage);
                        badgeState.Set("Invalid");
                    }

                }
            }
        }
    }

    /// <summary>Displays detailed information about a parsed IBAN</summary>
    public class IbanDetailsView(Iban iban) : ViewBase
    {
        public override object? Build()
        {
            return Layout.Vertical()
                            | Text.H3($"IBAN Info: {iban}")
                            | new
                            {
                                Country = new
                                {
                                    iban.Country.EnglishName,
                                    iban.Country.NativeName,
                                    ISO = iban.Country.TwoLetterISORegionName,
                                    IsSepaMember = iban.Country.Sepa.IsMember,
                                    IncludedCountries = string.Join(", ", iban.Country.IncludedCountries),
                                    iban.Country.LastUpdatedDate,
                                }.ToDetails(),
                                BBAN = iban.Bban,
                                iban.BankIdentifier,
                                iban.BranchIdentifier,
                                IbanFormat = new
                                {
                                    Electronic = iban.ToString(IbanFormat.Electronic),
                                    Print = iban.ToString(IbanFormat.Print),
                                    Obfuscated = iban.ToString(IbanFormat.Obfuscated)
                                }.ToDetails()
                            }.ToDetails();
        }
    }
}
