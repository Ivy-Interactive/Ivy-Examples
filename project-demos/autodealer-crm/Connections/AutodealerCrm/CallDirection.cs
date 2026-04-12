using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class CallDirection
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("CallDirection")]
    public virtual ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();
}
