using AarhusSpaceProgram.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    // DbSet properties for each entity: Which is a collection of entities of a specific type that can be queried from the database and used to perform CRUD operations.
    public DbSet<Astronaut> Astronauts => Set<Astronaut>();
    public DbSet<Scientist> Scientists => Set<Scientist>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Rocket> Rockets => Set<Rocket>();
    public DbSet<Launchpad> Launchpads => Set<Launchpad>();
    public DbSet<CelestialBody> CelestialBodies => Set<CelestialBody>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<Experiment> Experiments => Set<Experiment>();

    // OnModelCreating method is used to configure the model and its relationships using the Fluent API. It allows you to specify how entities relate to each other, set up constraints, and configure various aspects of the model.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.AstronautProfile)
            .WithOne(a => a.ApplicationUser)
            .HasForeignKey<Astronaut>(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.ScientistProfile)
            .WithOne(s => s.ApplicationUser)
            .HasForeignKey<Scientist>(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.ManagerProfile)
            .WithOne(m => m.ApplicationUser)
            .HasForeignKey<Manager>(m => m.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Manager)
            .WithMany(mgr => mgr.Missions)
            .HasForeignKey(m => m.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Experiment>()
            .HasOne(e => e.Mission)
            .WithMany(m => m.Experiments)
            .HasForeignKey(e => e.MissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Experiment>()
            .HasOne(e => e.Scientist)
            .WithMany(s => s.Experiments)
            .HasForeignKey(e => e.ScientistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Launchpad)
            .WithMany(lp => lp.Missions)
            .HasForeignKey(m => m.LaunchpadId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.TargetCelestialBody)
            .WithMany(cb => cb.Missions)
            .HasForeignKey(m => m.TargetCelestialBodyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Rocket)
            .WithOne(r => r.Mission)
            .HasForeignKey<Mission>(m => m.RocketId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Mission>()
            .HasMany(m => m.Astronauts)
            .WithMany(a => a.Missions)
            .UsingEntity(j => j.ToTable("MissionAstronauts"));

        modelBuilder.Entity<Mission>()
            .HasMany(m => m.Scientists)
            .WithMany(s => s.Missions)
            .UsingEntity(j => j.ToTable("MissionScientists"));

        modelBuilder.Entity<Mission>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Mission>()
            .HasIndex(m => new { m.LaunchpadId, m.LaunchDate })
            .IsUnique()
            .HasFilter("[LaunchpadId] IS NOT NULL AND [LaunchDate] IS NOT NULL");

        modelBuilder.Entity<Mission>()
            .HasIndex(m => m.RocketId)
            .IsUnique()
            .HasFilter("[RocketId] IS NOT NULL");
    }
}
