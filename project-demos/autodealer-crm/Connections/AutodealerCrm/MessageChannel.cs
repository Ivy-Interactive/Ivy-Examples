using System.ComponentModel.DataAnnotations.Schema;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class MessageChannel
{
    [Key]
    public int Id { get; set; }

    public string DescriptionText { get; set; } = null!;

    [InverseProperty("MessageChannel")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
