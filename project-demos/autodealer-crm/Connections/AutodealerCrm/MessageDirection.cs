using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class MessageDirection
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("MessageDirection")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
