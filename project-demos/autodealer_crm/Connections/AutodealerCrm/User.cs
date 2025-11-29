using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("users")]
[Index("UserRoleId", Name = "IX_users_UserRoleId")]
public partial class User
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int UserRoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Manager")]
    public virtual ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();

    [InverseProperty("Manager")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();

    [InverseProperty("Manager")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("Manager")]
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    [ForeignKey("UserRoleId")]
    [InverseProperty("Users")]
    public virtual UserRole UserRole { get; set; } = null!;

    [InverseProperty("Manager")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
