namespace NewtonsoftJsonExample;
public class UserData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public List<string> Roles { get; set; } = [];
}