using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalBackOffice;

public class ReservationStatus
{
    public int Id { get; set; }
    public string DescriptionText { get; set; }
    
    public static ReservationStatus[] GetSeedData()
    {
        return new[]
        {
            new ReservationStatus { Id = 1, DescriptionText = "Pending" },
            new ReservationStatus { Id = 2, DescriptionText = "Confirmed" },
            new ReservationStatus { Id = 3, DescriptionText = "Cancelled" },
            new ReservationStatus { Id = 4, DescriptionText = "Completed" }
        };
    }
}

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public string LastName { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; }

    [MaxLength(50)]
    public string Phone { get; set; }

    public bool IsHost { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Listing> HostListings { get; set; }
    public ICollection<Reservation> GuestReservations { get; set; }
    public ICollection<Review> Reviews { get; set; }
}

public class Listing
{
    public int Id { get; set; }
    public int HostId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; }

    public string Description { get; set; }

    [Required]
    [MaxLength(255)]
    public string AddressLine1 { get; set; }

    [MaxLength(255)]
    public string AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; }

    [Required]
    [MaxLength(100)]
    public string State { get; set; }

    [Required]
    [MaxLength(100)]
    public string Country { get; set; }

    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public int? NumBedrooms { get; set; }
    public int? NumBaths { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime UpdatedAt { get; set; }

    public User Host { get; set; }
    public ICollection<Reservation> Reservations { get; set; }
    public ICollection<Review> Reviews { get; set; }
    public ICollection<ListingAmenity> ListingAmenities { get; set; }
    public ICollection<Photo> Photos { get; set; }
}

public class Reservation
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public int GuestId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Listing Listing { get; set; }
    public User Guest { get; set; }
    public ReservationStatus Status { get; set; }
    public ICollection<Review> Reviews { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public int GuestId { get; set; }
    public int ReservationId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Listing Listing { get; set; }
    public User Guest { get; set; }
    public Reservation Reservation { get; set; }
}

public class Amenity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ListingAmenity> ListingAmenities { get; set; }
}

public class ListingAmenity
{
    public int ListingId { get; set; }
    public int AmenityId { get; set; }

    public Listing Listing { get; set; }
    public Amenity Amenity { get; set; }
}

public class Photo
{
    public int Id { get; set; }
    public int ListingId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; }

    [MaxLength(255)]
    public string Description { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Listing Listing { get; set; }
}

public class DataContext : DbContext
{
    public DataContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Listing> Listings { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<ListingAmenity> ListingAmenities { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<ReservationStatus> ReservationStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("listings");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Host)
                  .WithMany(u => u.HostListings)
                  .HasForeignKey(e => e.HostId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationStatus>(entity =>
        {
            entity.ToTable("reservation_statuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DescriptionText).IsRequired().HasMaxLength(255);
            entity.HasData(ReservationStatus.GetSeedData());
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Reservations)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Guest)
                  .WithMany(u => u.GuestReservations)
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Status)
                  .WithMany()
                  .HasForeignKey(e => e.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Reviews)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Guest)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(e => e.GuestId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Reservation)
                  .WithMany(r => r.Reviews)
                  .HasForeignKey(e => e.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.ToTable("amenities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ListingAmenity>(entity =>
        {
            entity.ToTable("listing_amenities");
            entity.HasKey(e => new { e.ListingId, e.AmenityId });
            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.ListingAmenities)
                  .HasForeignKey(e => e.ListingId);
            entity.HasOne(e => e.Amenity)
                  .WithMany(a => a.ListingAmenities)
                  .HasForeignKey(e => e.AmenityId);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("photos");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Photos)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}