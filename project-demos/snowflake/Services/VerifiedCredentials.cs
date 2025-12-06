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
    
    public static bool HasCredentials => !string.IsNullOrWhiteSpace(Account) 
        && !string.IsNullOrWhiteSpace(User) 
        && !string.IsNullOrWhiteSpace(Password);
    
    public static void SetCredentials(string account, string user, string password)
    {
        Account = account;
        User = user;
        Password = password;
        IsVerified = true;
    }
    
    public static void Clear()
    {
        Account = null;
        User = null;
        Password = null;
        IsVerified = false;
    }
}
