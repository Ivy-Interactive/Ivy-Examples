using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Vc.Connections.Vc;

public partial class VcContext : DbContext
{
    public VcContext(DbContextOptions<VcContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Deal> Deals { get; set; }

    public virtual DbSet<Founder> Founders { get; set; }

    public virtual DbSet<Gender> Genders { get; set; }

    public virtual DbSet<Industry> Industries { get; set; }

    public virtual DbSet<Partner> Partners { get; set; }

    public virtual DbSet<Startup> Startups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasOne(d => d.Startup).WithMany(p => p.Deals).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Founder>(entity =>
        {
            entity.HasOne(d => d.Gender).WithMany(p => p.Founders).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Gender>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasOne(d => d.Gender).WithMany(p => p.Partners).OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(d => d.Deals).WithMany(p => p.Partners)
                .UsingEntity<Dictionary<string, object>>(
                    "PartnerDeal",
                    r => r.HasOne<Deal>().WithMany()
                        .HasForeignKey("DealId")
                        .OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne<Partner>().WithMany()
                        .HasForeignKey("PartnerId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("PartnerId", "DealId");
                        j.ToTable("partner_deals");
                        j.HasIndex(new[] { "DealId" }, "IX_partner_deals_deal_id");
                        j.IndexerProperty<int>("PartnerId").HasColumnName("partner_id");
                        j.IndexerProperty<int>("DealId").HasColumnName("deal_id");
                    });
        });

        modelBuilder.Entity<Startup>(entity =>
        {
            entity.HasMany(d => d.Founders).WithMany(p => p.Startups)
                .UsingEntity<Dictionary<string, object>>(
                    "StartupFounder",
                    r => r.HasOne<Founder>().WithMany()
                        .HasForeignKey("FounderId")
                        .OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne<Startup>().WithMany()
                        .HasForeignKey("StartupId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("StartupId", "FounderId");
                        j.ToTable("startup_founders");
                        j.HasIndex(new[] { "FounderId" }, "IX_startup_founders_founder_id");
                        j.IndexerProperty<int>("StartupId").HasColumnName("startup_id");
                        j.IndexerProperty<int>("FounderId").HasColumnName("founder_id");
                    });

            entity.HasMany(d => d.Industries).WithMany(p => p.Startups)
                .UsingEntity<Dictionary<string, object>>(
                    "StartupIndustry",
                    r => r.HasOne<Industry>().WithMany()
                        .HasForeignKey("IndustryId")
                        .OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne<Startup>().WithMany()
                        .HasForeignKey("StartupId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("StartupId", "IndustryId");
                        j.ToTable("startup_industries");
                        j.HasIndex(new[] { "IndustryId" }, "IX_startup_industries_industry_id");
                        j.IndexerProperty<int>("StartupId").HasColumnName("startup_id");
                        j.IndexerProperty<int>("IndustryId").HasColumnName("industry_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
