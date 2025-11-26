using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("media")]
[Index("CustomerId", Name = "IX_media_CustomerId")]
[Index("LeadId", Name = "IX_media_LeadId")]
[Index("VehicleId", Name = "IX_media_VehicleId")]
public partial class Medium
{
    [Key]
    public int Id { get; set; }

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public string UploadedAt { get; set; } = null!;

    public int? VehicleId { get; set; }

    public int? LeadId { get; set; }

    public int? CustomerId { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Media")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("LeadId")]
    [InverseProperty("Media")]
    public virtual Lead? Lead { get; set; }

    [InverseProperty("Media")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [ForeignKey("VehicleId")]
    [InverseProperty("Media")]
    public virtual Vehicle? Vehicle { get; set; }
}
