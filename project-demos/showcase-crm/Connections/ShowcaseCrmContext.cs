using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm.Connections.ShowcaseCrm;

public partial class ShowcaseCrmContext : DbContext
{
    public ShowcaseCrmContext(DbContextOptions<ShowcaseCrmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<DealStage> DealStages { get; set; }

    public virtual DbSet<EfmigrationsLock> EfmigrationsLocks { get; set; }

    public virtual DbSet<Lead> Leads { get; set; }

    public virtual DbSet<LeadStatus> LeadStatuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasOne(d => d.Company).WithMany(p => p.Contacts).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasOne(d => d.Company).WithMany(p => p.Deals).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Contact).WithMany(p => p.Deals).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Lead).WithMany(p => p.Deals).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Stage).WithMany(p => p.Deals).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DealStage>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<EfmigrationsLock>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasOne(d => d.Company).WithMany(p => p.Leads).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Contact).WithMany(p => p.Leads).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Status).WithMany(p => p.Leads).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeadStatus>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
