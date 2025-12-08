namespace SnowflakeExample.Services;

/// <summary>
/// Static storage for verified Snowflake credentials and verification status
/// </summary>
public static class VerifiedCredentials
{
    public static string? Account { get; set; }
    public static string? User { get; set; }
    public static string? Password { get; set; }
    public static bool IsVerified { get; set; }
    
    /// <summary>
    /// Event that fires when verification status changes
    /// </summary>
    public static event Action? VerificationStatusChanged;
    
    public static bool HasCredentials => !string.IsNullOrWhiteSpace(Account) 
        && !string.IsNullOrWhiteSpace(User) 
        && !string.IsNullOrWhiteSpace(Password);
    
    /// <summary>
    /// Load credentials from configuration (appsettings.json, environment variables, or dotnet secrets)
    /// and automatically verify them if they exist
    /// </summary>
    public static void LoadFromConfiguration(IConfiguration configuration)
    {
        // Only load if not already set (don't override manually entered credentials)
        if (IsVerified) return;
        
        var account = configuration["Snowflake:Account"];
        var user = configuration["Snowflake:User"];
        var password = configuration["Snowflake:Password"];
        
        // If all credentials are available from configuration, set them as verified
        if (!string.IsNullOrWhiteSpace(account) 
            && !string.IsNullOrWhiteSpace(user) 
            && !string.IsNullOrWhiteSpace(password))
        {
            Account = account;
            User = user;
            Password = password;
            IsVerified = true;
            VerificationStatusChanged?.Invoke();
        }
    }
    
    public static void SetCredentials(string account, string user, string password)
    {
        Account = account;
        User = user;
        Password = password;
        IsVerified = true;
        VerificationStatusChanged?.Invoke();
    }
    
    public static void Clear()
    {
        Account = null;
        User = null;
        Password = null;
        IsVerified = false;
        VerificationStatusChanged?.Invoke();
    }
}
