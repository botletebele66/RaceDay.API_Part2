using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Models;

namespace RaceDay.API_Part2.Data
{

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

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.UserRole).HasMaxLength(50);
                entity.ToTable(t => t.HasCheckConstraint("CK_User_UserRole", "[UserRole] IN ('Organiser', 'Participant')"));
            });

            // Organiser
            modelBuilder.Entity<Organiser>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithOne(u => u.Organiser)
                      .HasForeignKey<Organiser>(o => o.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(o => o.UserID).IsUnique();
            });

            // Participant
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithOne(u => u.Participant)
                      .HasForeignKey<Participant>(p => p.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(p => p.UserID).IsUnique();
            });

            // Event
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.Organiser)
                      .WithMany(o => o.Events)
                      .HasForeignKey(e => e.OrganiserID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.EventType).HasMaxLength(50);
                entity.ToTable(t => t.HasCheckConstraint("CK_Event_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')"));
            });

            // EventCategory
            modelBuilder.Entity<EventCategory>(entity =>
            {
                entity.HasOne(ec => ec.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(ec => ec.EventID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(ec => ec.Distance).HasPrecision(10, 2);
                entity.Property(ec => ec.EntryFee).HasPrecision(10, 2);
            });

            // Enrolment
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.HasOne(en => en.Participant)
                      .WithMany(p => p.Enrolments)
                      .HasForeignKey(en => en.ParticipantID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(en => en.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(en => en.CategoryID)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(en => new { en.ParticipantID, en.CategoryID }).IsUnique();
                entity.Property(en => en.Status).HasMaxLength(50);
            });

            // Result
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasOne(r => r.Enrolment)
                      .WithOne(en => en.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentID)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(r => r.EnrolmentID).IsUnique();
            });
        }
    }

}
