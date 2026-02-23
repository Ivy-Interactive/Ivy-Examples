using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("contacts")]
[Index("CompanyId", Name = "IX_contacts_CompanyId")]
public partial class Contact
{
    [Key]
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Contacts")]
    public virtual Company Company { get; set; } = null!;

    [InverseProperty("Contact")]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    [InverseProperty("Contact")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
