namespace StripeNetExample
{
    [App(icon: Icons.Box)]
    public class StripeNetApp : ViewBase
    {
        private string StripAPIKey;
        private string BaseURL;
        public override object? Build()
        {

            IConfiguration _config = UseService<IConfiguration>();
            StripAPIKey = _config["Stripe:SecretKey"];
            BaseURL = _config["BaseURL"];
            string? session_id = GetPaymentSessionID();
            string? paymentstatus = GetPaymentStatus();


            if (!string.IsNullOrEmpty(paymentstatus))
            {
                if (paymentstatus == "success")
                {
                    if (!string.IsNullOrEmpty(session_id))
                    {
                        try
                        {
                            StripeConfiguration.ApiKey = StripAPIKey;
                            SessionService service = new SessionService();
                            Session session = service.Get(session_id);

                            Decimal amount = (session.AmountTotal ?? 0) / 100.0m;
                            string currency = session.Currency?.ToUpper() ?? "USD";

                            IClientProvider client = UseService<IClientProvider>();
                            return Layout.Vertical().Align(Align.Center)
                                | "✅ Payment succeeded!"
                                | $"Session ID: {session.Id}"
                                | $"Payment ID: {session.PaymentIntentId}"
                                | $"Payment Status: {session.PaymentStatus}"
                                | $"Amount Paid: {amount} {currency}"
                                | new Button("Go to Homepage", onClick: _ =>
                                {
                                    client.OpenUrl(BaseURL);
                                });
                        }
                        catch (Exception ex)
                        {
                            return "✅ Payment succeeded, but no session ID was provided in the URL.";
                        }
                    }

                    return "✅ Payment succeeded, but no session ID was provided in the URL.";
                }
                else
                {
                    return "Payment Canceled";

                }
            }
            else
            {
                IClientProvider client = UseService<IClientProvider>();
                return Layout.Horizontal().Align(Align.Left)
                  | new Button("Make Payment", onClick: _ =>
                  {
                      string paymentURL = GetPaymentURL();
                      client.OpenUrl(paymentURL);
                  })
                  ;
            }
        }


        public string GetPaymentURL()
        {
            StripeConfiguration.ApiKey = StripAPIKey;

            SessionCreateOptions options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
            {
                "card",
            },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = 2000, // $20.00
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Test Product",
                        },
                    },
                    Quantity = 1,
                },
            },
                Mode = "payment",
                SuccessUrl = BaseURL + "?appId=payment&appArgs=paymentstatus=success;session_id={CHECKOUT_SESSION_ID};",
                CancelUrl = BaseURL + "?appId=payment&appArgs=paymentstatus=cancel;",

            };
            SessionService service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }


        private string GetPaymentStatus()
        {
            return GetPaymentInfo()["paymentstatus"];
        }
        private string GetPaymentSessionID()
        {
            return GetPaymentInfo()["session_id"];
        }
        private Dictionary<string, string> GetPaymentInfo()
        {

            HttpContext httpContext = UseService<IHttpContextAccessor>()?.HttpContext;
            string queryString = httpContext?.Request?.QueryString.Value;
            Dictionary<string, string> result = new Dictionary<string, string>
                {
                    { "paymentstatus", "" },
                    { "session_id", "" }
                };

            if (string.IsNullOrEmpty(queryString)) return result;

            // Parse the full query string
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);

            // Extract appArgs
            string appArgs = query["appArgs"];

            if (!string.IsNullOrEmpty(appArgs))
            {
                string[] pairs = appArgs.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (string pair in pairs)
                {
                    string[] kv = pair.Split('=', 2);
                    if (kv.Length == 2)
                    {
                        string key = kv[0].Trim();
                        string value = kv[1].Trim();

                        if (result.ContainsKey(key))
                            result[key] = value;
                    }
                }
            }

            return result;
        }

    }
}
