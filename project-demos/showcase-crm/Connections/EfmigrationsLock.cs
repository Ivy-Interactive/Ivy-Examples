using System.ComponentModel.DataAnnotations.Schema;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("__EFMigrationsLock")]
public partial class EfmigrationsLock
{
    [Key]
    public int Id { get; set; }

    public string Timestamp { get; set; } = null!;
}
