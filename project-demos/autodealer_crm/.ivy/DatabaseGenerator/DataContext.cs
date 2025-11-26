using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutodealerCrm;

public class User
{
    public int Id { get; set; }
    [Required, MaxLength(255)]
    public string Name { get; set; } = null!;
    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = null!;
    [Required]
    public int UserRoleId { get; set; }
    [ForeignKey(nameof(UserRoleId))]
    public UserRole UserRole { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(255)]
    public string FirstName { get; set; } = null!;
    [Required, MaxLength(255)]
    public string LastName { get; set; } = null!;
    [MaxLength(255)]
    public string? Email { get; set; }
    [MaxLength(255)]
    public string? ViberId { get; set; }
    [MaxLength(255)]
    public string? WhatsappId { get; set; }
    [MaxLength(255)]
    public string? TelegramId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Vehicle
{
    public int Id { get; set; }
    [Required, MaxLength(255)]
    public string Make { get; set; } = null!;
    [Required, MaxLength(255)]
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    [Required, MaxLength(17)]
    public string Vin { get; set; } = null!;
    public decimal Price { get; set; }
    public int VehicleStatusId { get; set; }
    [ForeignKey(nameof(VehicleStatusId))]
    public VehicleStatus VehicleStatus { get; set; } = null!;
    public int? ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    public User? Manager { get; set; }
    [MaxLength(255)]
    public string? ErpSyncId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Lead
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;
    public int? ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    public User? Manager { get; set; }
    public int SourceChannelId { get; set; }
    [ForeignKey(nameof(SourceChannelId))]
    public SourceChannel SourceChannel { get; set; } = null!;
    public int LeadIntentId { get; set; }
    [ForeignKey(nameof(LeadIntentId))]
    public LeadIntent LeadIntent { get; set; } = null!;
    public int LeadStageId { get; set; }
    [ForeignKey(nameof(LeadStageId))]
    public LeadStage LeadStage { get; set; } = null!;
    public int? Priority { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LeadVehicle
{
    public int LeadId { get; set; }
    [ForeignKey(nameof(LeadId))]
    public Lead Lead { get; set; } = null!;
    public int VehicleId { get; set; }
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;
}

public class Media
{
    public int Id { get; set; }
    [Required, MaxLength(255)]
    public string FilePath { get; set; } = null!;
    [Required, MaxLength(50)]
    public string FileType { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public int? VehicleId { get; set; }
    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }
    public int? LeadId { get; set; }
    [ForeignKey(nameof(LeadId))]
    public Lead? Lead { get; set; }
    public int? CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Message
{
    public int Id { get; set; }
    public int? LeadId { get; set; }
    [ForeignKey(nameof(LeadId))]
    public Lead? Lead { get; set; }
    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;
    public int? ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    public User? Manager { get; set; }
    public int MessageChannelId { get; set; }
    [ForeignKey(nameof(MessageChannelId))]
    public MessageChannel MessageChannel { get; set; } = null!;
    public int MessageDirectionId { get; set; }
    [ForeignKey(nameof(MessageDirectionId))]
    public MessageDirection MessageDirection { get; set; } = null!;
    public int MessageTypeId { get; set; }
    [ForeignKey(nameof(MessageTypeId))]
    public MessageType MessageType { get; set; } = null!;
    public string? Content { get; set; }
    public int? MediaId { get; set; }
    [ForeignKey(nameof(MediaId))]
    public Media? Media { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CallRecord
{
    public int Id { get; set; }
    public int? LeadId { get; set; }
    [ForeignKey(nameof(LeadId))]
    public Lead? Lead { get; set; }
    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;
    public int? ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    public User? Manager { get; set; }
    public int CallDirectionId { get; set; }
    [ForeignKey(nameof(CallDirectionId))]
    public CallDirection CallDirection { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? Duration { get; set; }
    [MaxLength(255)]
    public string? RecordingUrl { get; set; }
    public decimal? ScriptScore { get; set; }
    public decimal? Sentiment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Task
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    [ForeignKey(nameof(LeadId))]
    public Lead Lead { get; set; } = null!;
    public int ManagerId { get; set; }
    [ForeignKey(nameof(ManagerId))]
    public User Manager { get; set; } = null!;
    [Required, MaxLength(255)]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public int Completed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserRole
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static UserRole[] GetSeedData() => new[]
    {
        new UserRole { Id = 1, DescriptionText = "Manager" },
        new UserRole { Id = 2, DescriptionText = "Supervisor" },
        new UserRole { Id = 3, DescriptionText = "Admin" },
        new UserRole { Id = 4, DescriptionText = "Analyst" }
    };
}

public class LeadStage
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static LeadStage[] GetSeedData() => new[]
    {
        new LeadStage { Id = 1, DescriptionText = "New" },
        new LeadStage { Id = 2, DescriptionText = "Contacted" },
        new LeadStage { Id = 3, DescriptionText = "Qualified" },
        new LeadStage { Id = 4, DescriptionText = "In Negotiation" },
        new LeadStage { Id = 5, DescriptionText = "Prepayment" },
        new LeadStage { Id = 6, DescriptionText = "Sold" },
        new LeadStage { Id = 7, DescriptionText = "Lost" }
    };
}

public class SourceChannel
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static SourceChannel[] GetSeedData() => new[]
    {
        new SourceChannel { Id = 1, DescriptionText = "Viber" },
        new SourceChannel { Id = 2, DescriptionText = "WhatsApp" },
        new SourceChannel { Id = 3, DescriptionText = "Telegram" },
        new SourceChannel { Id = 4, DescriptionText = "Call" },
        new SourceChannel { Id = 5, DescriptionText = "Email" }
    };
}

public class LeadIntent
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static LeadIntent[] GetSeedData() => new[]
    {
        new LeadIntent { Id = 1, DescriptionText = "Buy" },
        new LeadIntent { Id = 2, DescriptionText = "Sell" },
        new LeadIntent { Id = 3, DescriptionText = "Service Inquiry" },
        new LeadIntent { Id = 4, DescriptionText = "Other" }
    };
}

public class VehicleStatus
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static VehicleStatus[] GetSeedData() => new[]
    {
        new VehicleStatus { Id = 1, DescriptionText = "Available" },
        new VehicleStatus { Id = 2, DescriptionText = "Reserved" },
        new VehicleStatus { Id = 3, DescriptionText = "Sold" },
        new VehicleStatus { Id = 4, DescriptionText = "Archived" }
    };
}

public class MessageChannel
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static MessageChannel[] GetSeedData() => new[]
    {
        new MessageChannel { Id = 1, DescriptionText = "Viber" },
        new MessageChannel { Id = 2, DescriptionText = "WhatsApp" },
        new MessageChannel { Id = 3, DescriptionText = "Telegram" },
        new MessageChannel { Id = 4, DescriptionText = "Email" }
    };
}

public class MessageDirection
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static MessageDirection[] GetSeedData() => new[]
    {
        new MessageDirection { Id = 1, DescriptionText = "Incoming" },
        new MessageDirection { Id = 2, DescriptionText = "Outgoing" }
    };
}

public class MessageType
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static MessageType[] GetSeedData() => new[]
    {
        new MessageType { Id = 1, DescriptionText = "Text" },
        new MessageType { Id = 2, DescriptionText = "Photo" },
        new MessageType { Id = 3, DescriptionText = "File" },
        new MessageType { Id = 4, DescriptionText = "Voice Note" }
    };
}

public class CallDirection
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string DescriptionText { get; set; } = null!;
    public static CallDirection[] GetSeedData() => new[]
    {
        new CallDirection { Id = 1, DescriptionText = "Inbound" },
        new CallDirection { Id = 2, DescriptionText = "Outbound" }
    };
}

public class DataContext : DbContext
{
    public DataContext(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Lead> Leads { get; set; } = null!;
    public DbSet<LeadVehicle> LeadVehicles { get; set; } = null!;
    public DbSet<Media> Media { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<CallRecord> CallRecords { get; set; } = null!;
    public DbSet<Task> Tasks { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<LeadStage> LeadStages { get; set; } = null!;
    public DbSet<SourceChannel> SourceChannels { get; set; } = null!;
    public DbSet<LeadIntent> LeadIntents { get; set; } = null!;
    public DbSet<VehicleStatus> VehicleStatuses { get; set; } = null!;
    public DbSet<MessageChannel> MessageChannels { get; set; } = null!;
    public DbSet<MessageDirection> MessageDirections { get; set; } = null!;
    public DbSet<MessageType> MessageTypes { get; set; } = null!;
    public DbSet<CallDirection> CallDirections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>().HasData(UserRole.GetSeedData());
        modelBuilder.Entity<LeadStage>().HasData(LeadStage.GetSeedData());
        modelBuilder.Entity<SourceChannel>().HasData(SourceChannel.GetSeedData());
        modelBuilder.Entity<LeadIntent>().HasData(LeadIntent.GetSeedData());
        modelBuilder.Entity<VehicleStatus>().HasData(VehicleStatus.GetSeedData());
        modelBuilder.Entity<MessageChannel>().HasData(MessageChannel.GetSeedData());
        modelBuilder.Entity<MessageDirection>().HasData(MessageDirection.GetSeedData());
        modelBuilder.Entity<MessageType>().HasData(MessageType.GetSeedData());
        modelBuilder.Entity<CallDirection>().HasData(CallDirection.GetSeedData());

        modelBuilder.Entity<LeadVehicle>(entity =>
        {
            entity.HasKey(e => new { e.LeadId, e.VehicleId });
            entity.ToTable("lead_vehicles");
            entity.HasOne(e => e.Lead).WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasIndex(e => e.Vin).IsUnique();
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Media>(entity =>
        {
            entity.ToTable("media");
            entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Lead).WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasOne(e => e.Lead).WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Media).WithMany().HasForeignKey(e => e.MediaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CallRecord>(entity =>
        {
            entity.ToTable("call_records");
            entity.HasOne(e => e.Lead).WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasOne(e => e.Lead).WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Manager).WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}