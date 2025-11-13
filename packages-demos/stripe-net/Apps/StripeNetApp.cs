namespace StripeNetExample
{
    [App(icon: Icons.Box, title: "Stripe.Net")]
    public class StripeNetApp : ViewBase, IHaveSecrets
    {
        private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf",
            "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
        };

        public override object? Build()
        {
            var configuration = UseService<IConfiguration>();
            var client = UseService<IClientProvider>();

            var apiKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe secret key is not configured.");
            var baseUrl = "http://localhost:5010/stripe-net-example";

            var productName = UseState("Test Product");
            var amount = UseState(20.00m);
            var quantity = UseState(1);
            var currency = UseState("usd");
            var checkoutUrl = UseState(string.Empty);
            var isCreatingCheckout = UseState(false);

            var currencyOptions = new[]
            {
                new Option<string>("USD • US Dollar", "usd"),
                new Option<string>("EUR • Euro", "eur"),
                new Option<string>("GBP • British Pound", "gbp"),
                new Option<string>("AUD • Australian Dollar", "aud"),
                new Option<string>("CAD • Canadian Dollar", "cad"),
                new Option<string>("JPY • Japanese Yen", "jpy"),
                new Option<string>("CHF • Swiss Franc", "chf"),
                new Option<string>("PLN • Polish Złoty", "pln"),
                new Option<string>("UAH • Ukrainian Hryvnia", "uah"),
            };

            var isZeroDecimal = ZeroDecimalCurrencies.Contains(currency.Value);
            var totalAmount = Math.Max(amount.Value, 0m) * Math.Max(quantity.Value, 0);
            var totalLabel = FormatCurrency(totalAmount, currency.Value, isZeroDecimal);

            void StartCheckout()
            {
                if (amount.Value <= 0 || quantity.Value <= 0)
                {
                    client.Toast("Amount and quantity must be greater than zero.", "Invalid input");
                    return;
                }

                isCreatingCheckout.Value = true;
                try
                {
                    var session = CreateCheckoutSession(productName.Value, currency.Value, amount.Value, quantity.Value, apiKey, baseUrl);
                    checkoutUrl.Value = session.Url ?? string.Empty;
                    client.Toast(string.IsNullOrEmpty(checkoutUrl.Value) 
                        ? "Stripe did not return a checkout URL." 
                        : "Checkout session created. Complete it below.", "Stripe");
                }
                catch (Exception ex)
                {
                    client.Toast(ex.Message, "Stripe error");
                }
                finally
                {
                    isCreatingCheckout.Value = false;
                }
            } 

            if (!string.IsNullOrEmpty(checkoutUrl.Value))
            {
                client.Redirect(checkoutUrl.Value);
                checkoutUrl.Value = string.Empty;
            }

            return Layout.Vertical().Align(Align.TopCenter)
                | (Layout.Vertical().Width(Size.Fraction(0.4f))
                | Text.H2("Stripe.Net")
                | Text.Muted("This demo uses the Stripe Checkout library for payments; enter the details below, create a session, and complete it.")
                | new Ivy.Card(
                    Layout.Vertical().Gap(2)
                        | Text.H3("Configure the payment")
                        | Text.Muted("Choose currency, amount and quantity. Everything feeds directly into the next checkout session.")

                        | productName
                            .ToTextInput()
                            .Placeholder("Product name...")
                            .WithField()
                            .Label("Product")

                        | currency
                            .ToSelectInput(currencyOptions)
                            .WithField()
                            .Label("Currency")

                        | amount
                            .ToNumberInput()
                            .Min(0.01)
                            .Step(isZeroDecimal ? 1 : 0.50)
                            .WithField()
                            .Label("Unit amount")

                        | quantity
                            .ToNumberInput()
                            .Min(1)
                            .Step(1)
                            .WithField()
                            .Label("Quantity")

                        | Text.Muted($"Total: {totalLabel}")
                        | (isZeroDecimal
                            ? Text.Muted("Note: selected currency does not use fractional units.")
                            : Text.Muted("Amounts are calculated in major currency units (e.g. dollars, euros)."))

                        | new Button("Create checkout session", onClick: _ => StartCheckout())
                            .Primary()
                            .Icon(Icons.Play)
                            .Disabled(isCreatingCheckout.Value)
                )
                | Text.Small("This demo uses Stripe.Net library for creating checkout sessions.")
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Stripe.Net](https://github.com/stripe/stripe-dotnet)")
            );
        }

        private static Session CreateCheckoutSession(string productName, string currencyCode, decimal amount, int quantity, string apiKey, string baseUrl)
        {
            var normalizedCurrency = currencyCode.Trim().ToLowerInvariant();
            var multiplier = ZeroDecimalCurrencies.Contains(normalizedCurrency) ? 1m : 100m;
            var unitAmount = (long)Math.Round(amount * multiplier, 0);

            StripeConfiguration.ApiKey = apiKey;

            // Build success URL with query parameters for PaymentSuccessApp
            var encodedProductName = Uri.EscapeDataString(productName);
            var successUrl = baseUrl + "/payment-success";
            var cancelUrl = baseUrl + "/stripe-net";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = normalizedCurrency,
                            UnitAmount = unitAmount,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = string.IsNullOrWhiteSpace(productName) ? "Custom product" : productName.Trim(),
                            },
                        },
                        Quantity = Math.Max(quantity, 1),
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            return new SessionService().Create(options);
        }

        private static string FormatCurrency(decimal amount, string currencyCode, bool isZeroDecimal)
        {
            var format = isZeroDecimal ? "N0" : "N2";
            return $"{amount.ToString(format, CultureInfo.InvariantCulture)} {currencyCode.ToUpper()}";
        }

        public Secret[] GetSecrets()
        {
            return
            [
                new Secret("Stripe:SecretKey")
            ];
        }
    }
}
