using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("deals")]
[Index("CompanyId", Name = "IX_deals_CompanyId")]
[Index("ContactId", Name = "IX_deals_ContactId")]
[Index("LeadId", Name = "IX_deals_LeadId")]
[Index("StageId", Name = "IX_deals_StageId")]
public partial class Deal
{
    [Key]
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int ContactId { get; set; }

    public int? LeadId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    public int StageId { get; set; }

    [Column(TypeName = "date")]
    public DateTime? CloseDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Deals")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ContactId")]
    [InverseProperty("Deals")]
    public virtual Contact Contact { get; set; } = null!;

    [ForeignKey("LeadId")]
    [InverseProperty("Deals")]
    public virtual Lead? Lead { get; set; }

    [ForeignKey("StageId")]
    [InverseProperty("Deals")]
    public virtual DealStage Stage { get; set; } = null!;
}
