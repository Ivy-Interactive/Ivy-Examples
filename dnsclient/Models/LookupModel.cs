using DnsClient;

namespace DnsClientExample.Models;

public record LookupModel(
        string DNS,
        QueryType QueryType = QueryType.A);
