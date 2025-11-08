using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vc
{
    public class Startup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("founded_at")]
        public DateTime? FoundedAt { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<StartupFounder> StartupFounders { get; set; }
        public ICollection<StartupIndustry> StartupIndustries { get; set; }
        public ICollection<Deal> Deals { get; set; }
    }

    public class Founder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("first_name")]
        public string FirstName { get; set; }

        [Required]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("gender_id")]
        public int GenderId { get; set; }
        public Gender Gender { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<StartupFounder> StartupFounders { get; set; }
    }

    public class Partner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("first_name")]
        public string FirstName { get; set; }

        [Required]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("gender_id")]
        public int GenderId { get; set; }
        public Gender Gender { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<PartnerDeal> PartnerDeals { get; set; }
    }

    public class Deal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("startup_id")]
        public int StartupId { get; set; }
        public Startup Startup { get; set; }

        [Column("round")]
        public string Round { get; set; }

        [Column("amount", TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }
        
        [Column("deal_date")]
        public DateTime? DealDate { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<PartnerDeal> PartnerDeals { get; set; }
    }

    public class Industry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<StartupIndustry> StartupIndustries { get; set; }
    }

    public class StartupFounder
    {
        [Key]
        [Column("startup_id")]
        public int StartupId { get; set; }
        public Startup Startup { get; set; }

        [Key]
        [Column("founder_id")]
        public int FounderId { get; set; }
        public Founder Founder { get; set; }
    }

    public class PartnerDeal
    {
        [Key]
        [Column("partner_id")]
        public int PartnerId { get; set; }
        public Partner Partner { get; set; }

        [Key]
        [Column("deal_id")]
        public int DealId { get; set; }
        public Deal Deal { get; set; }
    }

    public class StartupIndustry
    {
        [Key]
        [Column("startup_id")]
        public int StartupId { get; set; }
        public Startup Startup { get; set; }

        [Key]
        [Column("industry_id")]
        public int IndustryId { get; set; }
        public Industry Industry { get; set; }
    }

    public class Gender
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("description_text")]
        public string DescriptionText { get; set; }

        public static Gender[] GetSeedData()
        {
            return new Gender[]
            {
                new Gender { Id = 1, DescriptionText = "Male" },
                new Gender { Id = 2, DescriptionText = "Female" },
                new Gender { Id = 3, DescriptionText = "NonBinary" },
                new Gender { Id = 4, DescriptionText = "Other" }
            };
        }
    }

    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options) { }

        public DbSet<Startup> Startups { get; set; }
        public DbSet<Founder> Founders { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<StartupFounder> StartupFounders { get; set; }
        public DbSet<PartnerDeal> PartnerDeals { get; set; }
        public DbSet<StartupIndustry> StartupIndustries { get; set; }
        public DbSet<Gender> Genders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Gender>(entity =>
            {
                entity.ToTable("genders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.DescriptionText).IsRequired().HasMaxLength(200);
                entity.HasData(Gender.GetSeedData());
            });

            modelBuilder.Entity<Startup>(entity =>
            {
                entity.ToTable("startups");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<Founder>(entity =>
            {
                entity.ToTable("founders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasOne(e => e.Gender)
                    .WithMany()
                    .HasForeignKey(e => e.GenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Partner>(entity =>
            {
                entity.ToTable("partners");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasOne(e => e.Gender)
                    .WithMany()
                    .HasForeignKey(e => e.GenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Deal>(entity =>
            {
                entity.ToTable("deals");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasOne(e => e.Startup)
                    .WithMany(s => s.Deals)
                    .HasForeignKey(e => e.StartupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Industry>(entity =>
            {
                entity.ToTable("industries");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<StartupFounder>(entity =>
            {
                entity.ToTable("startup_founders");
                entity.HasKey(e => new { e.StartupId, e.FounderId });
                entity.HasOne(e => e.Startup)
                    .WithMany(s => s.StartupFounders)
                    .HasForeignKey(e => e.StartupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Founder)
                    .WithMany(f => f.StartupFounders)
                    .HasForeignKey(e => e.FounderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PartnerDeal>(entity =>
            {
                entity.ToTable("partner_deals");
                entity.HasKey(e => new { e.PartnerId, e.DealId });
                entity.HasOne(e => e.Partner)
                    .WithMany(p => p.PartnerDeals)
                    .HasForeignKey(e => e.PartnerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Deal)
                    .WithMany(d => d.PartnerDeals)
                    .HasForeignKey(e => e.DealId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StartupIndustry>(entity =>
            {
                entity.ToTable("startup_industries");
                entity.HasKey(e => new { e.StartupId, e.IndustryId });
                entity.HasOne(e => e.Startup)
                    .WithMany(s => s.StartupIndustries)
                    .HasForeignKey(e => e.StartupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Industry)
                    .WithMany(i => i.StartupIndustries)
                    .HasForeignKey(e => e.IndustryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}