using System.ComponentModel.DataAnnotations.Schema;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("users")]
public partial class User
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
