using System.ComponentModel.DataAnnotations.Schema;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

[Table("deal_stages")]
public partial class DealStage
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("Stage")]
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();
}
