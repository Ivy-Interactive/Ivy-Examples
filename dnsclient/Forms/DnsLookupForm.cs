using DnsClient;
using DnsClientExample.Models;
using DnsClientExample.Signals;
using DnsClientExample.Utils;

namespace DnsClientExample.Forms;

public class DnsLookupForm : ViewBase
{ 
    public override object? Build()
    {
        var signal = this.Context.CreateSignal<DnsQueryResultsSignal, DnsQueryResponse?, bool>();

        var lookup = this.UseState<LookupModel>(() => new LookupModel("samples.ivy.app", QueryType.A));

        var lookupClient = UseService<ILookupClient>();
        var client = UseService<IClientProvider>();

        var formBuilder = lookup.ToForm()
            .Validate<string>(model => model.DNS,
            dns => (DnsValidator.IsValidDomainName(dns), "Must be a valid Domain Name"))
            .Required(model => model.DNS)
            .Description(model => model.DNS, "Enter a valid domain name to query DNS records")
            .Description(model => model.QueryType, "Select the type of DNS record to query");

        var (onSubmit, formView, validationView, loading) = formBuilder.UseForm(this.Context);

        async void HandleSubmit()
        {
            if (await onSubmit())
            {
                try
                {
                    var queryResults = await lookupClient.QueryAsync(lookup.Value.DNS, lookup.Value.QueryType);
                    await signal.Send((DnsQueryResponse)queryResults);
                }
                catch (Exception ex)
                {
                    client.Toast($"DNS Query Error: {ex.Message}");
                }
            }
        }

        return new Card()
            | Layout.Vertical(
                Text.H3("DNS Client"),
                Text.Muted("Query DNS records for any domain with detailed information"),
                formView,
                Layout.Horizontal(
                    new Button("Query DNS", new Action(HandleSubmit))
                        .Variant(ButtonVariant.Primary)
                        .Icon(Icons.Search)
                        .Disabled(loading),
                    loading ? Text.Muted("Querying...") : null
                ),
                new Spacer(),
                Text.Small("This demo uses the DnsClient.NET library for DNS lookups."),
                Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [DnsClient.NET](https://github.com/MichaCo/DnsClient.NET)")
            );
    }
}
