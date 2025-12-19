using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("call_records")]
[Index("CallDirectionId", Name = "IX_call_records_CallDirectionId")]
[Index("CustomerId", Name = "IX_call_records_CustomerId")]
[Index("LeadId", Name = "IX_call_records_LeadId")]
[Index("ManagerId", Name = "IX_call_records_ManagerId")]
public partial class CallRecord
{
    [Key]
    public int Id { get; set; }

    public int? LeadId { get; set; }

    public int CustomerId { get; set; }

    public int? ManagerId { get; set; }

    public int CallDirectionId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? Duration { get; set; }

    public string? RecordingUrl { get; set; }

    public string? ScriptScore { get; set; }

    public string? Sentiment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CallDirectionId")]
    [InverseProperty("CallRecords")]
    public virtual CallDirection CallDirection { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("CallRecords")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("LeadId")]
    [InverseProperty("CallRecords")]
    public virtual Lead? Lead { get; set; }

    [ForeignKey("ManagerId")]
    [InverseProperty("CallRecords")]
    public virtual User? Manager { get; set; }
}
