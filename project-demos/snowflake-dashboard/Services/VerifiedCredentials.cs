namespace SnowflakeDashboard.Services;

public static class VerifiedCredentials
{
    private static readonly SemaphoreSlim _verificationLock = new(1, 1);
    private static bool _hasAttemptedVerification;
    
    public static string? Account { get; set; }
    public static string? User { get; set; }
    public static string? Password { get; set; }
    public static bool IsVerified { get; set; }
    public static event Action? VerificationStatusChanged;
    
    public static bool HasCredentials => 
        !string.IsNullOrWhiteSpace(Account) && 
        !string.IsNullOrWhiteSpace(User) && 
        !string.IsNullOrWhiteSpace(Password);
    
    public static async Task<bool> TryLoadAndVerifyFromConfigurationAsync(IConfiguration configuration)
    {
        if (IsVerified || !await _verificationLock.WaitAsync(0)) return IsVerified;
        
        try
        {
            if (_hasAttemptedVerification) return IsVerified;
            _hasAttemptedVerification = true;
            
            var account = configuration["Snowflake:Account"];
            var user = configuration["Snowflake:User"];
            var password = configuration["Snowflake:Password"];
            
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                return false;
            
            var connectionString = $"account={account};user={user};password={password};";
            var isValid = await new SnowflakeService(connectionString).TestConnectionAsync();
            
            if (isValid)
            {
                Account = account;
                User = user;
                Password = password;
                IsVerified = true;
                VerificationStatusChanged?.Invoke();
            }
            
            return isValid;
        }
        finally
        {
            _verificationLock.Release();
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
        Account = User = Password = null;
        IsVerified = _hasAttemptedVerification = false;
        VerificationStatusChanged?.Invoke();
    }
}

