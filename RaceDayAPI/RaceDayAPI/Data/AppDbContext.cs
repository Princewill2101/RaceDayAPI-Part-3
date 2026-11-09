using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per entity = one table per entity
    public DbSet<User> Users { get; set; }
    public DbSet<EventType> EventTypes { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Users ───────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();           // No duplicate email addresses
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
            // CHECK constraint: Role must be "Organiser" or "Participant"
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role",
                "[Role] IN ('Organiser', 'Participant')"));
        });

        // ── EventTypes ──────────────────────────────────────────
        modelBuilder.Entity<EventType>(e =>
        {
            e.HasKey(et => et.EventTypeId);
            e.Property(et => et.TypeName).HasMaxLength(20).IsRequired();
            e.HasIndex(et => et.TypeName).IsUnique();
            // Seed the three fixed event types so they're always present
            e.HasData(
                new EventType { EventTypeId = 1, TypeName = "Run" },
                new EventType { EventTypeId = 2, TypeName = "Walk" },
                new EventType { EventTypeId = 3, TypeName = "Cycle" }
            );
        });

        // ── Events ──────────────────────────────────────────────
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Name).HasMaxLength(150).IsRequired();
            e.Property(ev => ev.Location).HasMaxLength(150).IsRequired();
            e.Property(ev => ev.DistanceKm).HasPrecision(5, 2);
            // An event belongs to one Organiser
            e.HasOne(ev => ev.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(ev => ev.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict);
            // An event has one EventType
            e.HasOne(ev => ev.EventType)
             .WithMany(et => et.Events)
             .HasForeignKey(ev => ev.EventTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Categories ──────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(c => c.DistanceKm).HasPrecision(5, 2);
            e.HasOne(c => c.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Enrolments ──────────────────────────────────────────
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.HasKey(en => en.EnrolmentId);
            e.Property(en => en.Status).HasMaxLength(20).HasDefaultValue("Confirmed");
            e.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status",
                "[Status] IN ('Confirmed', 'Cancelled')"));
            // One Participant can only enrol once per Event
            e.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();
            e.HasOne(en => en.Participant)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(en => en.ParticipantId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Event)
             .WithMany(ev => ev.Enrolments)
             .HasForeignKey(en => en.EventId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(en => en.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Results ─────────────────────────────────────────────
        modelBuilder.Entity<Result>(e =>
        {
            e.HasKey(r => r.ResultId);
            // One result per enrolment (one-to-one)
            e.HasIndex(r => r.EnrolmentId).IsUnique();
            e.HasOne(r => r.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(r => r.EnrolmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
