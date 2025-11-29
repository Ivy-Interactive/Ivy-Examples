using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("leads")]
[Index("CustomerId", Name = "IX_leads_CustomerId")]
[Index("LeadIntentId", Name = "IX_leads_LeadIntentId")]
[Index("LeadStageId", Name = "IX_leads_LeadStageId")]
[Index("ManagerId", Name = "IX_leads_ManagerId")]
[Index("SourceChannelId", Name = "IX_leads_SourceChannelId")]
public partial class Lead
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? ManagerId { get; set; }

    public int SourceChannelId { get; set; }

    public int LeadIntentId { get; set; }

    public int LeadStageId { get; set; }

    public int? Priority { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Lead")]
    public virtual ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();

    [ForeignKey("CustomerId")]
    [InverseProperty("Leads")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("LeadIntentId")]
    [InverseProperty("Leads")]
    public virtual LeadIntent LeadIntent { get; set; } = null!;

    [ForeignKey("LeadStageId")]
    [InverseProperty("Leads")]
    public virtual LeadStage LeadStage { get; set; } = null!;

    [ForeignKey("ManagerId")]
    [InverseProperty("Leads")]
    public virtual User? Manager { get; set; }

    [InverseProperty("Lead")]
    public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();

    [InverseProperty("Lead")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [ForeignKey("SourceChannelId")]
    [InverseProperty("Leads")]
    public virtual SourceChannel SourceChannel { get; set; } = null!;

    [InverseProperty("Lead")]
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    [ForeignKey("LeadId")]
    [InverseProperty("Leads")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
