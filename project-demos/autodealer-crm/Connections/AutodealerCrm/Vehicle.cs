using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

[Table("vehicles")]
[Index("ManagerId", Name = "IX_vehicles_ManagerId")]
[Index("VehicleStatusId", Name = "IX_vehicles_VehicleStatusId")]
[Index("Vin", Name = "IX_vehicles_Vin", IsUnique = true)]
public partial class Vehicle
{
    [Key]
    public int Id { get; set; }

    public string Make { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int Year { get; set; }

    public string Vin { get; set; } = null!;

    public decimal Price { get; set; }

    public int VehicleStatusId { get; set; }

    public int? ManagerId { get; set; }

    public string? ErpSyncId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ManagerId")]
    [InverseProperty("Vehicles")]
    public virtual User? Manager { get; set; }

    [InverseProperty("Vehicle")]
    public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();

    [ForeignKey("VehicleStatusId")]
    [InverseProperty("Vehicles")]
    public virtual VehicleStatus VehicleStatus { get; set; } = null!;

    [ForeignKey("VehicleId")]
    [InverseProperty("Vehicles")]
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
}
