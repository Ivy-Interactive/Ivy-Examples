using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("companies")]
public partial class Company
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    [InverseProperty("Company")]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    [InverseProperty("Company")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
