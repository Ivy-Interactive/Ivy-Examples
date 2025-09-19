using DnsClient;

namespace IvySample.DnsClient.Signals;

[Signal(BroadcastType.App)]
public class DnsQueryResultsSignal : AbstractSignal<DnsQueryResponse?, bool>
{
}
