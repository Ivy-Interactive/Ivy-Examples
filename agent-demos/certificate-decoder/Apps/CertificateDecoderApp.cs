using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Ivy;

namespace Certificate.Decoder.Apps;

[App(icon: Icons.ShieldCheck)]
public class CertificateDecoderApp : ViewBase
{
    public override object? Build()
    {
        var certInput = UseState("");
        var certData = UseState<CertInfo?>(null);
        var errorMsg = UseState<string?>(null);

        void Decode()
        {
            errorMsg.Set(null);
            certData.Set(null);

            var raw = certInput.Value?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                errorMsg.Set("Please paste a PEM or Base64-encoded certificate.");
                return;
            }

            try
            {
                var cert = ParseCertificate(raw);
                certData.Set(ExtractCertInfo(cert));
                cert.Dispose();
            }
            catch (Exception ex)
            {
                errorMsg.Set($"Failed to decode certificate: {ex.Message}");
            }
        }

        void Clear()
        {
            certInput.Set("");
            certData.Set(null);
            errorMsg.Set(null);
        }

        return Layout.TopCenter()
            | (Layout.Vertical().Width(Size.Full().Max(200)).Margin(10)
                | Text.H2("Certificate Decoder")
                | Text.Muted("Paste a PEM or Base64-encoded X.509 certificate to inspect its details.")
                | certInput.ToTextareaInput().Rows(8).Placeholder("-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----")
                    .WithField().Label("Certificate (PEM / Base64)")
                | (Layout.Horizontal()
                    | new Button("Decode", Decode).Primary()
                    | new Button("Clear", Clear).Outline())
                | BuildResult(errorMsg.Value, certData.Value));
    }

    static object? BuildResult(string? error, CertInfo? info)
    {
        if (error is not null)
            return Callout.Error(error, "Decode Error");

        if (info is null)
            return null;

        return new Fragment(
            new Separator(),
            BuildStatusBadges(info),
            new Separator("General Information"),
            BuildGeneralInfo(info),
            new Separator("Validity"),
            BuildValidity(info),
            new Separator("Key Information"),
            BuildKeyInfo(info),
            BuildSanSection(info),
            BuildKeyUsageSection(info),
            BuildExtendedKeyUsageSection(info),
            BuildRawExtensionsSection(info)
        );
    }

    static object BuildStatusBadges(CertInfo info)
    {
        var now = DateTime.UtcNow;
        var isExpired = now > info.NotAfter;
        var isSelfSigned = info.Subject == info.Issuer;
        var daysLeft = (info.NotAfter - now).TotalDays;

        return Layout.Horizontal()
            | new Badge(isExpired ? "Expired" : "Valid", isExpired ? BadgeVariant.Destructive : BadgeVariant.Success)
            | (isSelfSigned ? new Badge("Self-Signed", BadgeVariant.Warning) : new Badge("CA-Signed", BadgeVariant.Info))
            | new Badge(isExpired ? "Expired" : $"{(int)daysLeft} days left",
                isExpired ? BadgeVariant.Destructive :
                daysLeft < 30 ? BadgeVariant.Warning : BadgeVariant.Success);
    }

    static object BuildGeneralInfo(CertInfo info) =>
        new Details([
            new Detail("Subject", info.Subject, false),
            new Detail("Issuer", info.Issuer, false),
            new Detail("Serial Number", info.SerialNumber, false),
            new Detail("Thumbprint (SHA-256)", info.Thumbprint, false),
            new Detail("Version", $"V{info.Version}", false),
        ]);

    static object BuildValidity(CertInfo info)
    {
        var isExpired = DateTime.UtcNow > info.NotAfter;
        return new Details([
            new Detail("Not Before", info.NotBefore.ToString("yyyy-MM-dd HH:mm:ss UTC"), false),
            new Detail("Not After", info.NotAfter.ToString("yyyy-MM-dd HH:mm:ss UTC"), false),
            new Detail("Expired", isExpired ? "Yes" : "No", false),
        ]);
    }

    static object BuildKeyInfo(CertInfo info) =>
        new Details([
            new Detail("Signature Algorithm", info.SignatureAlgorithm, false),
            new Detail("Public Key Algorithm", info.PublicKeyAlgorithm, false),
            new Detail("Key Size", $"{info.KeySize} bits", false),
        ]);

    static object? BuildSanSection(CertInfo info)
    {
        if (info.SubjectAltNames.Length == 0)
            return null;

        return new Fragment(
            new Separator("Subject Alternative Names"),
            Layout.Vertical()
                | info.SubjectAltNames.Select(san => (object)new Badge(san, BadgeVariant.Outline)).ToArray()
        );
    }

    static object? BuildKeyUsageSection(CertInfo info)
    {
        if (info.KeyUsages.Length == 0)
            return null;

        return new Fragment(
            new Separator("Key Usage"),
            Layout.Horizontal()
                | info.KeyUsages.Select(ku => (object)new Badge(ku, BadgeVariant.Secondary)).ToArray()
        );
    }

    static object? BuildExtendedKeyUsageSection(CertInfo info)
    {
        if (info.ExtendedKeyUsages.Length == 0)
            return null;

        return new Fragment(
            new Separator("Extended Key Usage"),
            Layout.Horizontal()
                | info.ExtendedKeyUsages.Select(eku => (object)new Badge(eku, BadgeVariant.Secondary)).ToArray()
        );
    }

    static object? BuildRawExtensionsSection(CertInfo info)
    {
        if (info.Extensions.Length == 0)
            return null;

        var headerRow = new TableRow(
            new TableCell("OID").IsHeader(),
            new TableCell("Friendly Name").IsHeader(),
            new TableCell("Critical").IsHeader()
        ).IsHeader();

        var dataRows = info.Extensions.Select(ext =>
            new TableRow(
                new TableCell(ext.Oid),
                new TableCell(ext.FriendlyName),
                new TableCell(ext.Critical ? "Yes" : "No")
            )
        ).ToArray();

        var table = new Table([headerRow, .. dataRows]);

        return new Fragment(
            new Separator("Raw Extensions"),
            table
        );
    }

    static X509Certificate2 ParseCertificate(string input)
    {
        
        var pem = input
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Replace("-----END CERTIFICATE-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        var bytes = Convert.FromBase64String(pem);
        return X509CertificateLoader.LoadCertificate(bytes);
    }

    static CertInfo ExtractCertInfo(X509Certificate2 cert)
    {
        var thumbprint = Convert.ToHexString(SHA256.HashData(cert.RawData));
        var formattedThumb = FormatHex(thumbprint);

        var sanNames = new List<string>();
        var keyUsages = new List<string>();
        var extKeyUsages = new List<string>();
        var extensions = new List<ExtensionInfo>();

        foreach (var ext in cert.Extensions)
        {
            extensions.Add(new ExtensionInfo(
                ext.Oid?.Value ?? "Unknown",
                ext.Oid?.FriendlyName ?? "Unknown",
                ext.Critical
            ));

            switch (ext)
            {
                case X509SubjectAlternativeNameExtension san:
                    foreach (var name in san.EnumerateDnsNames())
                        sanNames.Add($"DNS: {name}");
                    foreach (var ip in san.EnumerateIPAddresses())
                        sanNames.Add($"IP: {ip}");
                    break;
                case X509KeyUsageExtension ku:
                    keyUsages.AddRange(ParseKeyUsageFlags(ku.KeyUsages));
                    break;
                case X509EnhancedKeyUsageExtension eku:
                    foreach (var oid in eku.EnhancedKeyUsages)
                        extKeyUsages.Add(oid.FriendlyName ?? oid.Value ?? "Unknown");
                    break;
            }
        }

        var publicKeyAlg = cert.PublicKey.Oid.FriendlyName ?? cert.PublicKey.Oid.Value ?? "Unknown";
        var keySize = cert.PublicKey.GetRSAPublicKey()?.KeySize
                      ?? cert.PublicKey.GetECDsaPublicKey()?.KeySize
                      ?? cert.PublicKey.GetDSAPublicKey()?.KeySize
                      ?? 0;

        return new CertInfo(
            Subject: cert.Subject,
            Issuer: cert.Issuer,
            SerialNumber: FormatHex(cert.SerialNumber),
            Thumbprint: formattedThumb,
            Version: cert.Version,
            NotBefore: cert.NotBefore.ToUniversalTime(),
            NotAfter: cert.NotAfter.ToUniversalTime(),
            SignatureAlgorithm: cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value ?? "Unknown",
            PublicKeyAlgorithm: publicKeyAlg,
            KeySize: keySize,
            SubjectAltNames: [.. sanNames],
            KeyUsages: [.. keyUsages],
            ExtendedKeyUsages: [.. extKeyUsages],
            Extensions: [.. extensions]
        );
    }

    static List<string> ParseKeyUsageFlags(X509KeyUsageFlags flags)
    {
        List<string> result = [];
        foreach (X509KeyUsageFlags flag in Enum.GetValues<X509KeyUsageFlags>())
        {
            if (flag != X509KeyUsageFlags.None && flags.HasFlag(flag))
                result.Add(flag.ToString());
        }
        return result;
    }

    static string FormatHex(string hex) =>
        string.Join(":", Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)));
}

record CertInfo(
    string Subject,
    string Issuer,
    string SerialNumber,
    string Thumbprint,
    int Version,
    DateTime NotBefore,
    DateTime NotAfter,
    string SignatureAlgorithm,
    string PublicKeyAlgorithm,
    int KeySize,
    string[] SubjectAltNames,
    string[] KeyUsages,
    string[] ExtendedKeyUsages,
    ExtensionInfo[] Extensions
);

record ExtensionInfo(string Oid, string FriendlyName, bool Critical);
