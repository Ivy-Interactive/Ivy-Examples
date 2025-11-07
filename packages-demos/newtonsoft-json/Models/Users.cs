namespace NewtonsoftJsonExample;
public class UserData
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public List<string> Roles { get; set; } = [];
}