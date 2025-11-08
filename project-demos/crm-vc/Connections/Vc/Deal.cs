using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

[Table("deals")]
[Index("StartupId", Name = "IX_deals_startup_id")]
public partial class Deal
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("startup_id")]
    public int StartupId { get; set; }

    [Column("round")]
    public string Round { get; set; } = null!;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [Column("deal_date")]
    public DateTime? DealDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("StartupId")]
    [InverseProperty("Deals")]
    public virtual Startup Startup { get; set; } = null!;

    [ForeignKey("DealId")]
    [InverseProperty("Deals")]
    public virtual ICollection<Partner> Partners { get; set; } = new List<Partner>();
}
