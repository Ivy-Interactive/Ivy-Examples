using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

[Table("startups")]
public partial class Startup
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string Description { get; set; } = null!;

    [Column("founded_at")]
    public DateTime? FoundedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Startup")]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    [ForeignKey("StartupId")]
    [InverseProperty("Startups")]
    public virtual ICollection<Founder> Founders { get; set; } = new List<Founder>();

    [ForeignKey("StartupId")]
    [InverseProperty("Startups")]
    public virtual ICollection<Industry> Industries { get; set; } = new List<Industry>();
}
