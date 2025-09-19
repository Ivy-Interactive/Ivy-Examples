using DnsClient;

namespace IvySample.DnsClient.Models;

public record LookupModel(
        string Dns,
        QueryType QueryType = QueryType.A);
