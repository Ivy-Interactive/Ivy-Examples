using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShowcaseCrm;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Deal> Deals { get; set; }
    public DbSet<LeadStatus> LeadStatuses { get; set; }
    public DbSet<DealStage> DealStages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Website).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.Contacts)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Source).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.Leads)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Contact)
                  .WithMany(c => c.Leads)
                  .HasForeignKey(e => e.ContactId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Status)
                  .WithMany()
                  .HasForeignKey(e => e.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.ToTable("deals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CloseDate).HasColumnType("date");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.Deals)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Contact)
                  .WithMany(c => c.Deals)
                  .HasForeignKey(e => e.ContactId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.Deals)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Stage)
                  .WithMany()
                  .HasForeignKey(e => e.StageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeadStatus>(entity =>
        {
            entity.ToTable("lead_statuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DescriptionText).IsRequired().HasMaxLength(200);
            entity.HasData(LeadStatus.GetSeedData());
        });

        modelBuilder.Entity<DealStage>(entity =>
        {
            entity.ToTable("deal_stages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DescriptionText).IsRequired().HasMaxLength(200);
            entity.HasData(DealStage.GetSeedData());
        });
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Contact> Contacts { get; set; } = null!;
    public ICollection<Lead> Leads { get; set; } = null!;
    public ICollection<Deal> Deals { get; set; } = null!;
}

public class Contact
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Lead> Leads { get; set; } = null!;
    public ICollection<Deal> Deals { get; set; } = null!;
}

public class Lead
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public int StatusId { get; set; }
    public LeadStatus Status { get; set; } = null!;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Deal> Deals { get; set; } = null!;
}

public class Deal
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public decimal? Amount { get; set; }
    public int StageId { get; set; }
    public DealStage Stage { get; set; } = null!;
    public DateTime? CloseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LeadStatus
{
    public int Id { get; set; }
    public string DescriptionText { get; set; } = null!;

    public static LeadStatus[] GetSeedData()
    {
        return new LeadStatus[]
        {
            new LeadStatus { Id = 1, DescriptionText = "New" },
            new LeadStatus { Id = 2, DescriptionText = "Contacted" },
            new LeadStatus { Id = 3, DescriptionText = "Qualified" },
            new LeadStatus { Id = 4, DescriptionText = "Lost" }
        };
    }
}

public class DealStage
{
    public int Id { get; set; }
    public string DescriptionText { get; set; } = null!;

    public static DealStage[] GetSeedData()
    {
        return new DealStage[]
        {
            new DealStage { Id = 1, DescriptionText = "Prospecting" },
            new DealStage { Id = 2, DescriptionText = "Qualification" },
            new DealStage { Id = 3, DescriptionText = "Proposal" },
            new DealStage { Id = 4, DescriptionText = "Closed Won" },
            new DealStage { Id = 5, DescriptionText = "Closed Lost" }
        };
    }
}