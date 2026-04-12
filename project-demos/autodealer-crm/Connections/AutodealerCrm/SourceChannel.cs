using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class SourceChannel
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("SourceChannel")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
