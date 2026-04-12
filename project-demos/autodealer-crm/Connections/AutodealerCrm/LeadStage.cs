using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class LeadStage
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("LeadStage")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
