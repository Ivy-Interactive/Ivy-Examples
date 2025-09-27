using Ivy.Aspose.OCR.Examples.Connections.OCR.Aspose;
using Ivy.Connections;

namespace Ivy.Aspose.OCR.Examples.Connections.OCR;

public class OCRConnection : IConnection
{
    public string GetConnectionType()
    {
        return typeof(OCRConnection).ToString();
    }

    public string GetContext(string connectionPath)
    {
        throw new NotImplementedException();
    }

    public ConnectionEntity[] GetEntities()
    {
        throw new NotImplementedException();
    }

    public string GetName() => nameof(OCRConnection);

    public string GetNamespace() => typeof(OCRConnection).Namespace;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IOCRService, AsposeOcrService>();
    }
}
