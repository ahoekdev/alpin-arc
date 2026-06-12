using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Lodge> Lodges => Set<Lodge>();
    public DbSet<Stage> Stages => Set<Stage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lodge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.DurationMinutes).IsRequired();
            entity.Property(e => e.DistanceMeters).IsRequired();

            entity.HasIndex(e => new { e.StartLodgeId, e.EndLodgeId }).IsUnique();

            entity.HasOne(e => e.StartLodge)
                .WithMany()
                .HasForeignKey(e => e.StartLodgeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EndLodge)
                .WithMany()
                .HasForeignKey(e => e.EndLodgeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
