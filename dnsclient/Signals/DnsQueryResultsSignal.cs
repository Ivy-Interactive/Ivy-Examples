using DnsClient;

namespace DnsClientExample.Signals;

[Signal(BroadcastType.App)]
public class DnsQueryResultsSignal : AbstractSignal<DnsQueryResponse?, bool>
{
}
