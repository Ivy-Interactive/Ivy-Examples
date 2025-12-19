using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm.Connections.AutodealerCrm;

public partial class AutodealerCrmContext : DbContext
{
    public AutodealerCrmContext(DbContextOptions<AutodealerCrmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CallDirection> CallDirections { get; set; }

    public virtual DbSet<CallRecord> CallRecords { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EfmigrationsLock> EfmigrationsLocks { get; set; }

    public virtual DbSet<Lead> Leads { get; set; }

    public virtual DbSet<LeadIntent> LeadIntents { get; set; }

    public virtual DbSet<LeadStage> LeadStages { get; set; }

    public virtual DbSet<Medium> Media { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageChannel> MessageChannels { get; set; }

    public virtual DbSet<MessageDirection> MessageDirections { get; set; }

    public virtual DbSet<MessageType> MessageTypes { get; set; }

    public virtual DbSet<SourceChannel> SourceChannels { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleStatus> VehicleStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CallRecord>(entity =>
        {
            entity.HasOne(d => d.Customer).WithMany(p => p.CallRecords).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Lead).WithMany(p => p.CallRecords).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Manager).WithMany(p => p.CallRecords).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EfmigrationsLock>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasOne(d => d.Customer).WithMany(p => p.Leads).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Manager).WithMany(p => p.Leads).OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(d => d.Vehicles).WithMany(p => p.Leads)
                .UsingEntity<Dictionary<string, object>>(
                    "LeadVehicle",
                    r => r.HasOne<Vehicle>().WithMany()
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne<Lead>().WithMany()
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("LeadId", "VehicleId");
                        j.ToTable("lead_vehicles");
                        j.HasIndex(new[] { "VehicleId" }, "IX_lead_vehicles_VehicleId");
                    });
        });

        modelBuilder.Entity<Medium>(entity =>
        {
            entity.HasOne(d => d.Customer).WithMany(p => p.Media).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Lead).WithMany(p => p.Media).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Media).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(d => d.Customer).WithMany(p => p.Messages).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Lead).WithMany(p => p.Messages).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Manager).WithMany(p => p.Messages).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Media).WithMany(p => p.Messages).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasOne(d => d.Lead).WithMany(p => p.Tasks).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Manager).WithMany(p => p.Tasks).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasOne(d => d.Manager).WithMany(p => p.Vehicles).OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
