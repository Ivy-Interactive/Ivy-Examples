using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class VehicleStatus
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("VehicleStatus")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
