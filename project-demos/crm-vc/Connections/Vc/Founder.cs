using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

[Table("founders")]
[Index("Email", Name = "IX_founders_email", IsUnique = true)]
[Index("GenderId", Name = "IX_founders_gender_id")]
public partial class Founder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    public string LastName { get; set; } = null!;

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("gender_id")]
    public int GenderId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GenderId")]
    [InverseProperty("Founders")]
    public virtual Gender Gender { get; set; } = null!;

    [ForeignKey("FounderId")]
    [InverseProperty("Founders")]
    public virtual ICollection<Startup> Startups { get; set; } = new List<Startup>();
}
