using System.Net.Http.Json;
using System.Text.Json;

namespace OpperDotNet;

/// <summary>
/// Client for interacting with the Opper.ai API
/// </summary>
public class OpperClient : IDisposable
{
    private const string DefaultBaseUrl = "https://api.opper.ai";
    private const string CallEndpoint = "/v2/call";
    
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    /// <summary>
    /// Creates a new OpperClient with the specified API key
    /// </summary>
    /// <param name="apiKey">Your Opper.ai API key</param>
    /// <param name="baseUrl">Optional: Override the base API URL</param>
    public OpperClient(string apiKey, string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl ?? DefaultBaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _disposeHttpClient = true;
    }

    /// <summary>
    /// Creates a new OpperClient with a custom HttpClient
    /// </summary>
    /// <param name="httpClient">Pre-configured HttpClient with authentication</param>
    public OpperClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = false;
    }

    /// <summary>
    /// Execute a call to the Opper.ai API
    /// </summary>
    /// <param name="request">The call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API response</returns>
    public async Task<OpperCallResponse> CallAsync(
        OpperCallRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Request name cannot be null or empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Instructions))
            throw new ArgumentException("Request instructions cannot be null or empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Input))
            throw new ArgumentException("Request input cannot be null or empty", nameof(request));

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                CallEndpoint, 
                request, 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new OpperException(
                    $"Opper.ai API request failed with status code {response.StatusCode}",
                    (int)response.StatusCode,
                    errorContent);
            }

            var result = await response.Content.ReadFromJsonAsync<OpperCallResponse>(
                cancellationToken: cancellationToken);

            if (result == null)
                throw new OpperException("Failed to deserialize response from Opper.ai API");

            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new OpperException("HTTP request to Opper.ai API failed", ex);
        }
        catch (JsonException ex)
        {
            throw new OpperException("Failed to parse response from Opper.ai API", ex);
        }
        catch (OpperException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpperException("Unexpected error during Opper.ai API call", ex);
        }
    }

    /// <summary>
    /// Execute a simple call to the Opper.ai API with minimal parameters
    /// </summary>
    /// <param name="name">Name of the task</param>
    /// <param name="instructions">Instructions for the AI</param>
    /// <param name="input">Input text</param>
    /// <param name="model">Optional: Model to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API response</returns>
    public Task<OpperCallResponse> CallAsync(
        string name,
        string instructions,
        string input,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var request = new OpperCallRequest(name, instructions, input, model);
        return CallAsync(request, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}

