using System.Text.RegularExpressions;

namespace DnsClientExample.Utils;

public static class DnsValidator
{
    // RFC 1035/1123 rules
    private static readonly Regex DomainRegex = new Regex(
        @"^(?=.{1,253}$)(?!-)([a-zA-Z0-9-]{1,63}(?<!-)\.)+[a-zA-Z]{2,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidDomainName(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        return DomainRegex.IsMatch(domain);
    }
}
