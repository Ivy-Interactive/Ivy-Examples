namespace AsposeOcrExample.Connections.OCR;

public interface IOCRService
{
    string ExtractText(MemoryStream imageStream);
}
