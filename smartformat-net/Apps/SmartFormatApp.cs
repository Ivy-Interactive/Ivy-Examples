using SmartFormat;
using SmartFormat.Extensions;

namespace IvySmartFormat.Apps
{
    record Product(string Name, decimal Price, int Quantity);

    /// <summary>Represents a selectable product option with localized display label and index value.</summary>
    /// <param name="Product">The actual product data</param>
    /// <param name="Index">The numeric index used to store and identify the selected product.</param>
    /// <param name="Culture">The culture string used to format the product price in the correct currency.</param>
    record OptionItemProduct(Product Product, int Index, string Culture) : IAnyOption
    {
        public string Label { get; set; } = Smart.Format($"{Product.Name}{{:tilde:50}}{Smart.Format(CultureInfo.GetCultureInfo(Culture), "{0:C}", Product.Price)}");
        public string? Group { get; set; }
        public object Value { get; set; } = Index;

        public Type GetOptionType() => typeof(int);
    }
    record User(string FirstName, string LastName,string Gender, string Mail, string Address, bool IsAdmin, long Phone);
    record Order(string CustomerName, DateTime OrderDate, string Address, long Phone, string Culture, Product[] Items);
    record Currency(string Name, string Culture);

    [App(icon: Icons.LetterText, title: "SmartFormat Demo")]
    public class SmartFormatApp : ViewBase
    {
        private const string DEFAULT_CULTURE = "en-US";
        IState<string> selectedLanguage = null!;
        public SmartFormatApp()
        {
            // Configure SmartFormat with localization and custom formatters
            Smart.Default.Settings.Localization.LocalizationProvider = new DictLocalizationProvider();
            Smart.Default.AddExtensions(
                new LocalizationFormatter(),
                new ListFormatter(),
                new PluralLocalizationFormatter(),
                new ConditionalFormatter(),
                new ReceiptFormatter(),
                new TildePadFormatter()
            );
        }
        public override object? Build()
        {
            #region Reactive State
            selectedLanguage = UseState("en");
            var client = UseService<IClientProvider>();
            var selectedNotice = UseState(new string[] { });
            var messageCount = UseState(0);
            var login = UseState("");
            var selectedProductIndexes = UseState(new int[] { });
            var selectedCurrency = UseState(DEFAULT_CULTURE);
            #endregion

            #region Data
            var currencies = new[]
            {
                new Currency("US Dollar", "en-US"),
                new Currency("Euro", "fr-FR"),
                new Currency("Swedish Krona", "sv-SE"),
                new Currency("British Pound", "en-GB"),
            };
            var options = new List<string>() { "Email", "Phone", "SMS", "Push Notification" };
            var users = new[] {
                new User(
                    "John",
                    "Doe",
                    "Male",
                    "john.doe@example.com",
                    "123 Main Street, Springfield, USA",
                    true,
                    15551234567
                ),
                new User(
                    "Anna",
                    "Smith",
                    "Female",
                    "anna.smith@example.com",
                    "Storgatan 15, Stockholm, Sweden",
                    false,
                    46701234567
                )
            };
            var items = new Product[]
            {
                new("Laptop", 1250m, 1),
                new("Mouse", 25m, 2),
                new("USB-C Cable", 10m, 3),
                new("Smartphone", 899.99m, 1),
                new("Bluetooth Headphones", 149.00m, 1),
                new("27-inch Monitor", 350.50m, 1),
                new("Wireless Mouse", 35.00m, 2),
                new("Mechanical Keyboard", 119.99m, 1),
                new("External 1 TB Drive", 65.40m, 1),
                new("Full HD Webcam", 45.75m, 1),
                new("Gaming Console", 599.00m, 1),
                new("10-inch Tablet", 450.25m, 1),
                new("E-reader", 99.99m, 1)
            };
            var currentUser = users.FirstOrDefault(m => m.Mail == login.Value) ?? users[0];
            var order = new Order(
                Smart.Format("{FirstName} {LastName}", users[0]),
                DateTime.Now,
                currentUser.Address,
                currentUser.Phone,
                GetCurrentCulture(),
                selectedProductIndexes.Value
                    .Where(index => index >= 0 && index < items.Length)
                    .Select(index => items[index])
                    .ToArray()
            );
            #endregion

            string GetCurrentCulture()
            {
                return currencies.FirstOrDefault(c => c.Name == selectedCurrency.Value)?.Culture ?? DEFAULT_CULTURE;
            }

            return Layout.Vertical()
                // Example showing usage of "choose" and "list" formatters
                | new Card(
                    Layout.Vertical()
                    | Text.Label(SmartF("How would you like to be notified?"))
                    | selectedNotice.ToSelectInput(options.ToOptions()).Variant(SelectInputs.List)
                    | Text.Small(SmartF(Smart.Format("{0:choose(0):|You selected}", selectedNotice.Value.Length), selectedNotice)))
                // Example showing usage of localization formatter
                | new Card(Layout.Vertical()
                    | Text.Label(SmartF("Select Language:"))
                    | selectedLanguage.ToSelectInput(new[] { "en", "sv", "es" }.ToOptions()).Variant(SelectInputs.Select)
                    )
                // Example showing usage of custom formatters
                | new Card(
                    Layout.Vertical()
                    | Text.Label(SmartF("Select products:"))
                    | selectedProductIndexes.ToSelectInput(items.Select((product, index) => new OptionItemProduct(product, index, GetCurrentCulture())).ToArray()).Variant(SelectInputs.List)
                    | Text.Label(SmartF("Select currency:"))
                    | selectedCurrency.ToSelectInput(currencies.Select(c => c.Name).ToOptions()).Variant(SelectInputs.Toggle)
                    | new TextInput(Smart.Format("{0:receipt:}", order)).Variant(TextInputs.Textarea).Height(100)
                    )
                // Example showing usage of "cond" and "plural" formatters
                | new Card(
                    Layout.Horizontal()
                    | Text.Label(SmartF("Change the number:"))
                    | new NumberInput<int>(messageCount).Min(0).Max(10).Width(2),
                    Layout.Horizontal()
                    | new Icon(messageCount.Value > 0 ? Icons.Mail : Icons.MailCheck)
                    | Text.Label(SmartF("You have", messageCount.Value))
                    )
                // Example showing usage of "choose" formatter and integration with reactive state
                | new Card(
                    Layout.Horizontal()
                    | Text.Block(SmartF("Login with:"))
                    | login.ToSelectInput(users.Select(p => p.Mail).ToArray().ToOptions()).Variant(SelectInputs.Select)
                    | new Button("Login", onClick: () => { client.Toast(SmartF("Hello", currentUser)); })
                    );
        }

        /// <summary>Formats a string using SmartFormat with the selected language.</summary>
        /// <param name="key">The localization key to look up.</param>
        /// <param name="obj">Optional object to provide values for placeholders in the localized string.</param>
        public string SmartF(string key, object? obj = null)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return Smart.Format($"{{:L({selectedLanguage.Value}):{key}}}", obj);
        }
    }
}
