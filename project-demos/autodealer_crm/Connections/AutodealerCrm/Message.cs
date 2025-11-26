using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("messages")]
[Index("CustomerId", Name = "IX_messages_CustomerId")]
[Index("LeadId", Name = "IX_messages_LeadId")]
[Index("ManagerId", Name = "IX_messages_ManagerId")]
[Index("MediaId", Name = "IX_messages_MediaId")]
[Index("MessageChannelId", Name = "IX_messages_MessageChannelId")]
[Index("MessageDirectionId", Name = "IX_messages_MessageDirectionId")]
[Index("MessageTypeId", Name = "IX_messages_MessageTypeId")]
public partial class Message
{
    [Key]
    public int Id { get; set; }

    public int? LeadId { get; set; }

    public int CustomerId { get; set; }

    public int? ManagerId { get; set; }

    public int MessageChannelId { get; set; }

    public int MessageDirectionId { get; set; }

    public int MessageTypeId { get; set; }

    public string? Content { get; set; }

    public int? MediaId { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Messages")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("LeadId")]
    [InverseProperty("Messages")]
    public virtual Lead? Lead { get; set; }

    [ForeignKey("ManagerId")]
    [InverseProperty("Messages")]
    public virtual User? Manager { get; set; }

    [ForeignKey("MediaId")]
    [InverseProperty("Messages")]
    public virtual Medium? Media { get; set; }

    [ForeignKey("MessageChannelId")]
    [InverseProperty("Messages")]
    public virtual MessageChannel MessageChannel { get; set; } = null!;

    [ForeignKey("MessageDirectionId")]
    [InverseProperty("Messages")]
    public virtual MessageDirection MessageDirection { get; set; } = null!;

    [ForeignKey("MessageTypeId")]
    [InverseProperty("Messages")]
    public virtual MessageType MessageType { get; set; } = null!;
}
