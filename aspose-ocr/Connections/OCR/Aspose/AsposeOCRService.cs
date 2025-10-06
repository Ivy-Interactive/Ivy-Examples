using Aspose.OCR;

namespace AsposeOcrExample.Connections.OCR.Aspose;

public class AsposeOcrService: IOCRService
{
    private readonly AsposeOcr recognitionEngine;

    public AsposeOcrService()
    {
        recognitionEngine = new AsposeOcr();
    }

    public string ExtractText(MemoryStream imageStream)
    {
        // Add image to the recognition batch
        var source = new OcrInput(InputType.SingleImage);
        source.Add(imageStream);

        // Perform OCR
        List<RecognitionResult> results
             = recognitionEngine.Recognize(source);

        // OCR processing
        string result = string.Empty;
        if (results.Count > 0)
        {
            result = results[0].RecognitionText;
        }

        return result;
    }
}
