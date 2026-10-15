using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Data;

/// <summary>
/// EF Core context (Code-First). Table names, column sizes, check constraints, unique indexes and
/// delete behaviour mirror the schema in the Part 1 database-schema.sql script.
/// </summary>
/// 
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserID);
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Password).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.UserRole).HasMaxLength(50).IsRequired();
            entity.Property(u => u.CreatedDate).HasColumnType("datetime");
            entity.Property(u => u.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(u => u.Email).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Users_UserRole", "[UserRole] IN ('Organiser', 'Participant')"));
        });

        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.ToTable("Organiser");
            entity.HasKey(o => o.OrganiserID);
            entity.Property(o => o.CompanyName).HasMaxLength(255).IsRequired();
            entity.Property(o => o.PhoneNumber).HasMaxLength(20);
            entity.Property(o => o.RegistrationNumber).HasMaxLength(50);
            entity.Property(o => o.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(o => o.UserID).IsUnique();
            entity.HasIndex(o => o.RegistrationNumber).IsUnique();
            entity.HasOne(o => o.User)
                  .WithOne(u => u.Organiser)
                  .HasForeignKey<Organiser>(o => o.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participant");
            entity.HasKey(p => p.ParticipantID);
            entity.Property(p => p.DateOfBirth).HasColumnType("date");
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.EmergencyContact).HasMaxLength(255);
            entity.Property(p => p.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(p => p.UserID).IsUnique();
            entity.HasOne(p => p.User)
                  .WithOne(u => u.Participant)
                  .HasForeignKey<Participant>(p => p.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");
            entity.HasKey(e => e.EventID);
            entity.Property(e => e.EventName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.OrganiserID);
            entity.HasOne(e => e.Organiser)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganiserID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Event_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')");
                t.HasCheckConstraint("CK_Event_MaxParticipants", "[MaxParticipants] IS NULL OR [MaxParticipants] > 0");
            });
        });

        modelBuilder.Entity<EventCategory>(entity =>
        {
            entity.ToTable("EventCategory");
            entity.HasKey(c => c.CategoryID);
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Distance).HasPrecision(10, 2);
            entity.Property(c => c.EntryFee).HasPrecision(10, 2);
            entity.Property(c => c.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(c => c.EventID);
            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_EventCategory_Distance", "[Distance] > 0");
                t.HasCheckConstraint("CK_EventCategory_EntryFee", "[EntryFee] >= 0");
            });
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolment");
            entity.HasKey(en => en.EnrolmentID);
            entity.Property(en => en.Status).HasMaxLength(50).IsRequired();
            entity.Property(en => en.EnrolmentDate).HasColumnType("datetime");
            entity.Property(en => en.UpdatedDate).HasColumnType("datetime");
            entity.HasIndex(en => new { en.ParticipantID, en.CategoryID }).IsUnique();
            entity.HasIndex(en => en.CategoryID);
            entity.HasOne(en => en.Participant)
                  .WithMany(p => p.Enrolments)
                  .HasForeignKey(en => en.ParticipantID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryID)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_Enrolment_Status", "[Status] IN ('Registered', 'Started', 'Completed', 'Withdrawn')"));
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");
            entity.HasKey(r => r.ResultID);
            entity.Property(r => r.DateCompleted).HasColumnType("datetime");
            entity.HasIndex(r => r.EnrolmentID).IsUnique();
            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentID)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Result_TimeCompleted", "[TimeCompleted] IS NULL OR [TimeCompleted] >= 0");
                t.HasCheckConstraint("CK_Result_PlacementRank", "[PlacementRank] IS NULL OR [PlacementRank] > 0");
            });
        });
    }
}
