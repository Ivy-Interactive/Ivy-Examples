using DnsClient;
using DnsClientExample.Signals;

namespace DnsClientExample.Components;

public class DnsQueryResults : ViewBase
{
    public override object? Build()
    {
        var signal = Context.UseSignal<DnsQueryResultsSignal, DnsQueryResponse?, bool>();

        var queryResults = UseState<DnsQueryResponse?>(() => null);

        UseEffect(() => signal.Receive(results =>
        {
            queryResults.Set(results);           

            return true;
        }));

        var result = queryResults?.Value;

        if (result == null)
            return Text.Strong("Query to See results");

        var allRecords = result?.AllRecords;

        return Layout.Vertical()
         | Text.P($"Domain: {result?.NameServer}")

         | (allRecords.Any()
            ? Layout.Vertical()
                | Text.Strong("All Records")
                | allRecords.Select((record, index) =>
                    Layout.Vertical()
                        |(string.IsNullOrWhiteSpace(record.DomainName) ? null : Text.Strong($"Domain Name: {record.DomainName}"))
                        |Text.P($" Record Type: {record.RecordType}")
                        )
            : null);        
    }
}
