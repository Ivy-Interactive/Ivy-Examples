// Note: This is a simplified JWT service for demonstration purposes
// In a real application, you would use the JWT package for proper token handling

namespace Jwt.Apps;

/// <summary>
/// Service class for JWT operations including token generation, validation, and claims extraction.
/// This demonstrates the core functionality of JWT token management.
/// Note: This is a simplified implementation for demonstration purposes.
/// In production, use a proper JWT library like the JWT package.
/// </summary>
public class JwtService
{
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService()
    {
        // In a real application, these should come from configuration
        _issuer = "JwtDemoApp";
        _audience = "JwtDemoUsers";
    }

    /// <summary>
    /// Generates a JWT token with the specified claims and expiration time.
    /// Note: This is a simplified implementation for demonstration purposes.
    /// </summary>
    /// <param name="claims">Dictionary of claims to include in the token</param>
    /// <param name="expirationMinutes">Token expiration time in minutes (default: 60)</param>
    /// <returns>Generated JWT token as a string</returns>
    public string GenerateToken(Dictionary<string, string> claims, int expirationMinutes = 60)
    {
        // This is a simplified implementation for demonstration
        // In a real application, use the JWT package for proper token generation
        var header = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var payload = CreatePayload(claims, expirationMinutes);
        
        // Simple base64 encoding (not proper JWT encoding)
        var headerEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(header));
        var payloadEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = "demo-signature"; // This would be a proper HMAC signature in real implementation
        
        return $"{headerEncoded}.{payloadEncoded}.{signature}";
    }

    /// <summary>
    /// Validates a JWT token and extracts its claims.
    /// Note: This is a simplified implementation for demonstration purposes.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>JWT validation result with claims if valid, or error information if invalid</returns>
    public JwtValidationResult ValidateToken(string token)
    {
        try
        {
            // This is a simplified implementation for demonstration
            // In a real application, use the JWT package for proper token validation
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return new JwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token format. Expected 3 parts separated by dots."
                };
            }

            var payload = parts[1];
            var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var payloadData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
            
            if (payloadData == null)
            {
                return new JwtValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to parse token payload."
                };
            }

            var claims = new Dictionary<string, string>();
            foreach (var kvp in payloadData)
            {
                claims[kvp.Key] = kvp.Value?.ToString() ?? "";
            }

            // Check expiration
            if (claims.ContainsKey("exp"))
            {
                var exp = long.Parse(claims["exp"]);
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                if (expDateTime < DateTime.UtcNow)
                {
                    return new JwtValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Token has expired."
                    };
                }
            }

            return new JwtValidationResult
            {
                IsValid = true,
                Claims = claims,
                Expires = claims.ContainsKey("exp") ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims["exp"])).DateTime : null,
                IssuedAt = claims.ContainsKey("iat") ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims["iat"])).DateTime : null,
                Issuer = claims.ContainsKey("iss") ? claims["iss"] : _issuer,
                Audience = claims.ContainsKey("aud") ? claims["aud"] : _audience
            };
        }
        catch (Exception ex)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string CreatePayload(Dictionary<string, string> claims, int expirationMinutes)
    {
        var payload = new Dictionary<string, object>
        {
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds()
        };

        foreach (var claim in claims)
        {
            payload[claim.Key] = claim.Value;
        }

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }
}

/// <summary>
/// Result of JWT token validation operation.
/// </summary>
public class JwtValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, string>? Claims { get; set; }
    public DateTime? Expires { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? ErrorMessage { get; set; }
}