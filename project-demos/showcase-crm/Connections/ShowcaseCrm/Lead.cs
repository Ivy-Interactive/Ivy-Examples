using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("leads")]
[Index("CompanyId", Name = "IX_leads_CompanyId")]
[Index("ContactId", Name = "IX_leads_ContactId")]
[Index("StatusId", Name = "IX_leads_StatusId")]
public partial class Lead
{
    [Key]
    public int Id { get; set; }

    public int? CompanyId { get; set; }

    public int? ContactId { get; set; }

    public int StatusId { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Leads")]
    public virtual Company? Company { get; set; }

    [ForeignKey("ContactId")]
    [InverseProperty("Leads")]
    public virtual Contact? Contact { get; set; }

    [InverseProperty("Lead")]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    [ForeignKey("StatusId")]
    [InverseProperty("Leads")]
    public virtual LeadStatus Status { get; set; } = null!;
}
