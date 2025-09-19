using DnsClient;
using IvySample.DnsClient.Models;
using IvySample.DnsClient.Signals;
using IvySample.DnsClient.Utils;

namespace IvySample.DnsClient.Forms;

public class DnsLookupForm : ViewBase
{ 
    public override object? Build()
    {
        var signal = this.Context.CreateSignal<DnsQueryResultsSignal, DnsQueryResponse?, bool>();

        var lookup = this.UseState<LookupModel>(() => new LookupModel("", QueryType.A));

        var lookupClient = UseService<ILookupClient>();

        var formBuilder = lookup.ToForm()
            .Validate<string>(model => model.Dns,
            dns => (DnsValidator.IsValidDomainName(dns), "Must be a valid Domain Name"))
            .Required(model => model.Dns)
            .Description(model =>model.Dns, "Enter a valid DNS to query it's records.");

        var (onSumbit, formView, validationView, loading) = formBuilder.UseForm(this.Context);

        async void HandleSubmit()
        {
            if (await onSumbit())
            {
                var queryResults = await lookupClient.QueryAsync(lookup.Value.Dns, lookup.Value.QueryType);

                var result = await signal.Send((DnsQueryResponse)queryResults);
                
            }
        }

        return Layout.Vertical()
            | formView
            | new Button("Query Dns").Tooltip("Query Dns Results").HandleClick(new Action(HandleSubmit)).Loading(loading).Disabled(loading);
    }
}
