using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("__EFMigrationsLock")]
public partial class EfmigrationsLock
{
    [Key]
    public int Id { get; set; }

    public string Timestamp { get; set; } = null!;
}
