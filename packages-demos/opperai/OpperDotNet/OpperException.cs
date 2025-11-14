namespace OpperDotNet;

/// <summary>
/// Exception thrown when an error occurs during Opper.ai API calls
/// </summary>
public class OpperException : Exception
{
    public int? StatusCode { get; }
    public string? ResponseContent { get; }

    public OpperException(string message) : base(message)
    {
    }

    public OpperException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public OpperException(string message, int statusCode, string? responseContent = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}

