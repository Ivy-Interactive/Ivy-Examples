using System.ComponentModel.DataAnnotations.Schema;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("contacts")]
[Index("CompanyId", Name = "IX_contacts_CompanyId")]
[Index(nameof(FirstName), Name = "IX_contacts_FirstName")]
[Index(nameof(LastName), Name = "IX_contacts_LastName")]
[Index(nameof(Email), Name = "IX_contacts_Email")]
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
