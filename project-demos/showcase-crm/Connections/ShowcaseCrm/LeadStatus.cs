using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("lead_statuses")]
public partial class LeadStatus
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("Status")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
