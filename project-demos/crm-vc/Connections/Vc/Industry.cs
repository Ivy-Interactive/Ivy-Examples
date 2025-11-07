using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

[Table("industries")]
[Index("Name", Name = "IX_industries_name", IsUnique = true)]
public partial class Industry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("IndustryId")]
    [InverseProperty("Industries")]
    public virtual ICollection<Startup> Startups { get; set; } = new List<Startup>();
}
