using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class LeadIntent
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("LeadIntent")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
