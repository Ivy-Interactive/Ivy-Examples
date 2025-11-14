namespace StripeNetExample
{
    [App(icon: Icons.BadgeCheck, title: "Payment", isVisible: false)]
    public class PaymentSuccessApp : ViewBase
    {
        public override object? Build()
        {
            var client = UseService<IClientProvider>();
            return Layout.Center()
                | (Layout.Vertical().Width(Size.Fraction(0.4f))
                | new Ivy.Card(
                    Layout.Vertical().Gap(3)
                    | Text.H2("Payment Successful")
                    | Text.Muted("Thank you for your purchase! Your payment has been processed successfully.")

                    | Callout.Success(
                        "Your payment has been completed successfully.",
                        "Payment Confirmed"
                ))

                | new Button("Back to Store", onClick: _ => client.Redirect("http://localhost:5010/stripe-net-example/stripe-net"))
                    .Primary()
                    .Icon(Icons.ArrowLeft)
            );
        }
    }
}

