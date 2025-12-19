using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("customers")]
public partial class Customer
{
    [Key]
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    public string? ViberId { get; set; }

    public string? WhatsappId { get; set; }

    public string? TelegramId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();

    [InverseProperty("Customer")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();

    [InverseProperty("Customer")]
    public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();

    [InverseProperty("Customer")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
