using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("__EFMigrationsLock")]
public partial class EfmigrationsLock
{
    [Key]
    public int Id { get; set; }

    public string Timestamp { get; set; } = null!;
}
