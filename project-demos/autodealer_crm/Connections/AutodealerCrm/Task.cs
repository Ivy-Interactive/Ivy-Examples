using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("tasks")]
[Index("LeadId", Name = "IX_tasks_LeadId")]
[Index("ManagerId", Name = "IX_tasks_ManagerId")]
public partial class Task
{
    [Key]
    public int Id { get; set; }

    public int LeadId { get; set; }

    public int ManagerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public int Completed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("LeadId")]
    [InverseProperty("Tasks")]
    public virtual Lead Lead { get; set; } = null!;

    [ForeignKey("ManagerId")]
    [InverseProperty("Tasks")]
    public virtual User Manager { get; set; } = null!;
}
