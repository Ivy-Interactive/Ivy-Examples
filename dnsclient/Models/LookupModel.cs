using DnsClient;

namespace DnsClientExample.Models;

public record LookupModel(
        string Dns,
        QueryType QueryType = QueryType.A);
