using DnsClient;
using DnsClient.Protocol;
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
            return Layout.Vertical(
                Text.Muted("Enter a domain and click 'Query DNS' to see results")
            );

        return Layout.Vertical(
            RenderHeader(result),
            RenderAnswers(result),
            RenderAuthority(result)
        );
    }

    private object RenderHeader(IDnsQueryResponse result)
    {
        var statusText = result.HasError ? $"Error: {result.ErrorMessage}" : "Query successful";
        var recordCount = result.AllRecords.Count();

        return new Card()
            .Title($"{statusText}")
            | Layout.Vertical(
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.Muted("Name Server:"),
                        Text.Strong(result.NameServer.ToString())
                    ).Width(Size.Grow()),
                    Layout.Vertical(
                        Text.Muted("Records Found:"),
                        Text.Strong($"{recordCount} record(s)")
                    ).Width(Size.Grow()),
                    Layout.Vertical(
                        Text.Muted("Message Size:"),
                        Text.Strong($"{result.MessageSize} bytes")
                    ).Width(Size.Grow())
                ).Width(Size.Full())
            );
    }

    private object? RenderAnswers(IDnsQueryResponse result)
    {
        if (!result.Answers.Any())
            return null;

        return new Card()
            .Title($"Answers ({result.Answers.Count})")
            | Layout.Vertical(
                result.Answers.Select(record => RenderRecord(record)).ToArray()
            );
    }

    private object? RenderAuthority(IDnsQueryResponse result)
    {
        if (!result.Authorities.Any())
            return null;

        return new Card()
            .Title($"Authority ({result.Authorities.Count})")
            | Layout.Vertical(
                result.Authorities.Select(record => RenderRecord(record)).ToArray()
            );
    }

    private object RenderRecord(DnsResourceRecord record)
    {
        return Layout.Vertical(
            Layout.Horizontal(
                Text.Strong(record.DomainName.Value).Width(Size.Grow()),
                Text.Muted($"[{record.RecordType}] TTL: {record.TimeToLive}s")
            ).Width(Size.Full()),
            RenderRecordData(record),
            new Separator()
        );
    }

    private object? RenderRecordData(DnsResourceRecord record)
    {
        return record switch
        {
            ARecord a => Layout.Vertical(
                Text.Muted("IPv4 Address:"),
                Text.Code(a.Address.ToString())
            ),

            AaaaRecord aaaa => Layout.Vertical(
                Text.Muted("IPv6 Address:"),
                Text.Code(aaaa.Address.ToString())
            ),

            MxRecord mx => Layout.Horizontal(
                Layout.Vertical(
                    Text.Muted("Priority:"),
                    Text.Literal(mx.Preference.ToString())
                ).Width(0.3),
                Layout.Vertical(
                    Text.Muted("Mail Server:"),
                    Text.Code(mx.Exchange.Value)
                ).Width(Size.Grow())
            ).Width(Size.Full()),

            CNameRecord cname => Layout.Vertical(
                Text.Muted("Canonical Name:"),
                Text.Code(cname.CanonicalName.Value)
            ),

            NsRecord ns => Layout.Vertical(
                Text.Muted("Name Server:"),
                Text.Code(ns.NSDName.Value)
            ),

            PtrRecord ptr => Layout.Vertical(
                Text.Muted("Pointer Domain Name:"),
                Text.Code(ptr.PtrDomainName.Value)
            ),

            TxtRecord txt => Layout.Vertical(
                Text.Muted("Text Records:"),
                Layout.Vertical(
                    txt.Text.Select(t => Text.Code(t)).ToArray()
                )
            ),

            SrvRecord srv => Layout.Vertical(
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.Muted("Target:"),
                        Text.Code(srv.Target.Value)
                    ).Width(Size.Grow()),
                    Layout.Vertical(
                        Text.Muted("Port:"),
                        Text.Literal(srv.Port.ToString())
                    ).Width(0.2)
                ).Width(Size.Full()),
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.Muted("Priority:"),
                        Text.Literal(srv.Priority.ToString())
                    ).Width(0.5),
                    Layout.Vertical(
                        Text.Muted("Weight:"),
                        Text.Literal(srv.Weight.ToString())
                    ).Width(0.5)
                ).Width(Size.Full())
            ),

            SoaRecord soa => Layout.Vertical(
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.Muted("Master NS:"),
                        Text.Code(soa.MName.Value)
                    ).Width(Size.Grow()),
                    Layout.Vertical(
                        Text.Muted("Responsible:"),
                        Text.Code(soa.RName.Value)
                    ).Width(Size.Grow())
                ).Width(Size.Full()),
                Layout.Horizontal(
                    Layout.Vertical(
                        Text.Muted("Serial:"),
                        Text.Literal(soa.Serial.ToString())
                    ).Width(0.25),
                    Layout.Vertical(
                        Text.Muted("Refresh:"),
                        Text.Literal($"{soa.Refresh}s")
                    ).Width(0.25),
                    Layout.Vertical(
                        Text.Muted("Retry:"),
                        Text.Literal($"{soa.Retry}s")
                    ).Width(0.25),
                    Layout.Vertical(
                        Text.Muted("Expire:"),
                        Text.Literal($"{soa.Expire}s")
                    ).Width(0.25)
                ).Width(Size.Full())
            ),

            _ => Text.Muted($"Raw Data: {record}")
        };
    }
}
