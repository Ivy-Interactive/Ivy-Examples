using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

[Table("genders")]
public partial class Gender
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("description_text")]
    public string DescriptionText { get; set; } = null!;

    [InverseProperty("Gender")]
    public virtual ICollection<Founder> Founders { get; set; } = new List<Founder>();

    [InverseProperty("Gender")]
    public virtual ICollection<Partner> Partners { get; set; } = new List<Partner>();
}
