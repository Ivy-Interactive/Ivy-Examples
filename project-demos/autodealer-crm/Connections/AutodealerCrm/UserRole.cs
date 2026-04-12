using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class UserRole
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("UserRole")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
