namespace IvyQrCodeProfileSharing.Services;

public interface IQrCodeService
{
    string GenerateQrCodeAsBase64(string text, int pixelsPerModule = 8);
    string GenerateVCardQrCodeAsBase64(string firstName, string lastName, string email, string? phone = null, string? linkedin = null, string? github = null, int pixelsPerModule = 8);
}