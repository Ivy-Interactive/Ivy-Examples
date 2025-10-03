using QRCoder;
using System.Text;

namespace IvyQrCodeProfileSharing.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateQrCodeAsBase64(string text, int pixelsPerModule = 8)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        return Convert.ToBase64String(qrCodeBytes);
    }

    public string GenerateVCardQrCodeAsBase64(string firstName, string lastName, string email, string? phone = null, string? linkedin = null, string? github = null, int pixelsPerModule = 8)
    {
        var vCard = GenerateVCard(firstName, lastName, email, phone, linkedin, github);
        return GenerateQrCodeAsBase64(vCard, pixelsPerModule);
    }

    private static string GenerateVCard(string firstName, string lastName, string email, string? phone, string? linkedin, string? github)
    {
        var vCard = new StringBuilder();
        
        // vCard header
        vCard.AppendLine("BEGIN:VCARD");
        vCard.AppendLine("VERSION:3.0");
        
        // Full name
        vCard.AppendLine($"FN:{firstName} {lastName}");
        
        // Structured name (Last;First;;;)
        vCard.AppendLine($"N:{lastName};{firstName};;;");
        
        // Email
        vCard.AppendLine($"EMAIL;TYPE=INTERNET:{email}");
        
        // Phone (if provided)
        if (!string.IsNullOrWhiteSpace(phone))
        {
            vCard.AppendLine($"TEL;TYPE=CELL:{phone}");
        }
        
        // LinkedIn URL as a URL field
        if (!string.IsNullOrWhiteSpace(linkedin))
        {
            vCard.AppendLine($"URL;TYPE=LinkedIn:{linkedin}");
        }
        
        // GitHub URL as a URL field
        if (!string.IsNullOrWhiteSpace(github))
        {
            vCard.AppendLine($"URL;TYPE=GitHub:{github}");
        }
        
        // vCard footer
        vCard.AppendLine("END:VCARD");
        
        return vCard.ToString();
    }
}
